using DecompoXor.Application.DependencyInjection;
using DecompoXor.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection; // for AddOpenApi extension
using Microsoft.AspNetCore.Builder; // for MapOpenApi extension

var builder = WebApplication.CreateBuilder(args);

// Add framework services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // required for Swagger
builder.Services.AddSwaggerGen();

// Register application and infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
