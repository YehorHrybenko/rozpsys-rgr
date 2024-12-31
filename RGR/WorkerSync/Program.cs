
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Data;
using System.Text;
using static SharedLibrary.CalculationsModel;
using static SharedLibrary.DataModel;

string redisConnectionString = "redis:6379"; 
ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConnectionString);
var server = redis.GetServer(redisConnectionString);

IDatabase db = redis.GetDatabase();

Random random = new Random();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/save", ([FromBody] UpdateRequest request) =>
{
    var dataRaw = request.data;

    var batch = db.CreateBatch();

    batch.StringSetAsync("data", request.data);
    batch.StringSetAsync("target", request.target);
    batch.StringSetAsync("droneCount", request.droneCount);

    batch.Execute();
    Console.WriteLine($"Saved data to storage.");
})
.WithName("save")
.WithOpenApi();

app.MapGet("/getAllRecords", () =>
{
    var data = new Dictionary<string, SerializableVector>();

    foreach (var key in server.Keys())
    {
        string value = db.StringGet(key)!;
        data[key!] = JsonConvert.DeserializeObject<SerializableVector>(value)!;
    }
    return JsonConvert.SerializeObject(data);
})
.WithName("getAllRecords")
.WithOpenApi();

app.Run();

