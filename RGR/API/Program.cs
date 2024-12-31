
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using static SharedLibrary.CalculationsModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using var client = new HttpClient(new HttpClientHandler())
{
    BaseAddress = new Uri("http://orchestrator:8080")
};

app.MapPost("/update", async ([FromBody] UpdateRequest input) =>
{
    //Console.WriteLine("Processing request!");
    var content = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/process", content);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync(); 
})
.WithName("Update")
.WithOpenApi();

app.Run();


