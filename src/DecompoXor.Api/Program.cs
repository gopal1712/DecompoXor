using DecompoXor.Application.DependencyInjection;
using DecompoXor.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection; // for AddOpenApi extension
using Microsoft.AspNetCore.Builder; // for MapOpenApi extension

LoadLocalEnvironmentFile();

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
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();

static void LoadLocalEnvironmentFile()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DecompoXor.slnx")))
    {
        directory = directory.Parent;
    }

    if (directory is null)
    {
        return;
    }

    var envPath = Path.Combine(directory.FullName, ".env");
    if (!File.Exists(envPath))
    {
        return;
    }

    foreach (var line in File.ReadLines(envPath))
    {
        var entry = line.Trim();
        if (entry.Length == 0 || entry.StartsWith('#'))
        {
            continue;
        }

        if (entry.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
        {
            entry = entry[7..].TrimStart();
        }

        var separator = entry.IndexOf('=');
        if (separator <= 0)
        {
            continue;
        }

        var name = entry[..separator].Trim();
        var value = entry[(separator + 1)..].Trim();
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        var configurationName = name.ToUpperInvariant() switch
        {
            "GROQ_API_KEY" => "Groq__ApiKey",
            "OPENROUTER_API_KEY" => "Openrouter__ApiKey",
            _ => name
        };

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(configurationName)) &&
            !string.IsNullOrWhiteSpace(value) &&
            !value.StartsWith("replace-with-", StringComparison.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable(configurationName, value);
        }
    }
}
