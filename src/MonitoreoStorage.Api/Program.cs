using Microsoft.OpenApi.Models;
using MonitoreoStorage.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddEnvironmentVariables();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MonitoreoStorage API", Version = "v1" });
});

// Register our services
builder.Services.AddSingleton<ITableReadService, TableReadService>();

// Register new services for Parte 2 (Analysis)
builder.Services.AddSingleton<ILlmAnalysisService, AzureOpenAiService>();
builder.Services.AddSingleton<IAnalysisConfigService, AnalysisConfigService>();
builder.Services.AddSingleton<IAuditService, AuditService>();

// Add HTTP client for Azure OpenAI
builder.Services.AddHttpClient();

// Add memory caching for configuration
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
