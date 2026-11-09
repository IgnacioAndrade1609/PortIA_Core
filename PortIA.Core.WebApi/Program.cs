using Microsoft.OpenApi;
using PortIA.Core.DependencyInjection;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Services.Component.Alerts.Interfaces;
using PortIA.Core.WebApi.ErrorHandling;
using PortIA.Core.WebApi.Hubs;
using PortIA.Core.WebApi.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAppConfiguration();
builder.Services.AddAll(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddScoped<IAlertNotifier, SignalRAlertNotifier>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Pega el token del login (sin 'Bearer', Swagger lo antepone solo).",
        });

        options.AddSecurityRequirement(document => new()
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5173", "http://localhost:3000") // Añade los puertos comunes de Angular/Vite
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.ApplyPendingMigrations();

app.UseExceptionHandler();

app.UseHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AlertsHub>("/hubs/alerts");

app.Run();
