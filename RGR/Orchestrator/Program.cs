using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Orchestrator.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using static SharedLibrary.DataModel;
using static SharedLibrary.CalculationsModel;
using static Orchestrator.Services.RangeSplitter;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var responseHandler = new TaskHandler();

var factory = new ConnectionFactory
{
    HostName = config["RabbitMQ:Host"]!,
    Port = config.GetValue<int>("RabbitMQ:Port"),
    UserName = config["RabbitMQ:UserName"]!,
    Password = config["RabbitMQ:Password"]!,
};

using var syncServiceClient = new HttpClient(new HttpClientHandler())
{
    BaseAddress = new Uri("http://workersync:8080")
};

IConnection connection;

while (true)
{
    try
    {
        connection = await factory.CreateConnectionAsync();
        break;
    }
    catch
    {
        Console.WriteLine("Could not connect to RabbitMQ. Reconnecting.");
        Thread.Sleep(1000);
    }
}

using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "requests", durable: false, exclusive: false, autoDelete: false);
var cbQueue = (await channel.QueueDeclareAsync("", durable: false, exclusive: false, autoDelete: false)).QueueName;

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    var bodyRaw = ea.Body.ToArray();
    var body = Encoding.UTF8.GetString(bodyRaw);

    var (id, result) = JsonConvert.DeserializeObject<CalculationsResponse>(body)!;
    
    responseHandler.AddResponse(id, result);

    return Task.CompletedTask;
};

await channel.BasicConsumeAsync(cbQueue, autoAck: true, consumer: consumer);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

int requestCouter = 0;
object requestLock = new();

app.MapPost("/process", async ([FromBody] UpdateRequest input) =>
{
    var content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");
    var response = syncServiceClient.PostAsync("/save", content);

    int totalDrones = input.droneCount;
    var ranges = SplitRange(0, totalDrones, 8);

    var dataRaw = input.data;
    var sw = new Stopwatch();
    sw.Start();

    var results = new List<(int, SerializableVector)>();
    
    var tasks = ranges.Select(r => Task.Run(async () => {
        int requestId;
        lock (requestLock)
        {
            requestId = requestCouter++;
        }
        
        var request = new CalculationsRequest() { requestID = requestId, slice = r, data = input.data, replyTo = cbQueue, altitude = input.altitude, target = input.target };

        var resRaw = responseHandler.PromiseRetrieve(requestId);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "requests",
            mandatory: true,
            basicProperties: new BasicProperties(),
            body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
        );

        var res = JsonConvert.DeserializeObject<List<(int, SerializableVector)>>(await resRaw);

        lock (results)
        {
            results.AddRange(res!);
        }
    }));

    Task.WaitAll(tasks.ToArray());
     
    sw.Stop();
    Console.WriteLine($"Calculations took: {sw.ElapsedMilliseconds} ms");

    var storageSaveResult = await response;
    storageSaveResult.EnsureSuccessStatusCode();
    Console.WriteLine($"{await storageSaveResult.Content.ReadAsStringAsync()}");

    var res = JsonConvert.SerializeObject(results.ToDictionary(p => p.Item1, p => p.Item2));
    return res;
})
.WithName("process")
.WithOpenApi();

app.Run();

