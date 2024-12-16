using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
builder.Services.AddRateLimiter(_ => _
               .AddFixedWindowLimiter(policyName: "web4fixed", options =>
               {
                   options.PermitLimit = 3;
                   options.Window = TimeSpan.FromSeconds(10);
                   options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                   options.QueueLimit = 0;
               }));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
