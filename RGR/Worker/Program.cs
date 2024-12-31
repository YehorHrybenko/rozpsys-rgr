
using Newtonsoft.Json;
using Orchestrator.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using static SharedLibrary.CalculationsModel;
using static SharedLibrary.DataModel;

var random = new Random();

Func<string, string?> env = Environment.GetEnvironmentVariable;

var factory = new ConnectionFactory
{
    HostName = env("RMQ_HOST")!,
    Port = int.Parse(env("RMQ_PORT")!),
    UserName = env("RMQ_USERNAME")!,
    Password = env("RMQ_PASSWORD")!,
};

IConnection connection;

while (true)
{
    try
    {
        connection = await factory.CreateConnectionAsync();
        break;
    }
    catch {
        Console.WriteLine("Could not connect to RabbitMQ. Reconnecting.");
        Thread.Sleep(1000);
    }
}

using var channel = await connection.CreateChannelAsync();
await channel.QueueDeclareAsync(queue: "requests", durable: false, exclusive: false, autoDelete: false);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();

        var request = JsonConvert.DeserializeObject<CalculationsRequest>(Encoding.UTF8.GetString(body));
        var (id, dataRaw, slice, replyTo, altitude, target) = request!;

        var data = JsonConvert.DeserializeObject<Dictionary<int, DroneData>>(dataRaw)!;
        var tgt = JsonConvert.DeserializeObject<SerializableVector>(target)!.ToVector3();
        var res = DroneManager.UpdateDrones(data, altitude, tgt,  slice);
        var resSerialized = JsonConvert.SerializeObject(res)!;

        var response = new CalculationsResponse()
        {
            requestID = id,
            result = resSerialized,
        };

        //Console.WriteLine($"Processed data chunk {id}.");

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: replyTo, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
};

await channel.BasicConsumeAsync("requests", autoAck: true, consumer: consumer);

Console.WriteLine("RabbitMQ connected. Waiting for messages!");

while (true)
{
    Thread.Sleep(1000);
}
