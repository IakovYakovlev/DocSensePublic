using DocSenseV1.Authorization;
using DocSenseV1.Data;
using DocSenseV1.ServiceExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Настройка логирования (используется встроенное логирование ASP.NET)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var services = builder.Services;

services.AddControllers();
services.AddEndpointsApiExplorer();

services.AddDbContext<EFDatabaseContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-RapidAPI-Proxy-Secret",
        Type = SecuritySchemeType.ApiKey,
        Description = "API Key for authorization"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- Регистрация сервисов приложения ---
services.AddApplicationServices();

var app = builder.Build();

// Получаем логгер после сборки приложения
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Применяем миграции автоматически при старте
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EFDatabaseContext>();
    
    try
    {
        logger.LogInformation("Применение миграций к базе данных...");
        dbContext.Database.Migrate();
        logger.LogInformation("Миграции успешно применены.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка при применении миграций.");
        throw;
    }
}

logger.LogInformation("Swagger available at http://localhost:8080/swagger");

app.UseSwagger();
app.UseSwaggerUI();


app.UseRouting();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapControllers();

app.Run();
