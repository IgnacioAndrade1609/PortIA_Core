using Microsoft.EntityFrameworkCore;
using Minio;
using PortIA.Core.DependencyInjection;
using PortIA.Core.Infrastructure.Data;
using PortIA.Core.Services.Component.Storage.Services;
using PortIA.Core.Services.Component.Storage.Interfaces;
using PortIA.Core.Services.Component.Shipments.Services;
using PortIA.Core.Services.Component.Shipments.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAppConfiguration();
builder.Services.AddAll(builder.Configuration);

builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(builder.Configuration["Minio:Endpoint"])
    .WithCredentials(
        builder.Configuration["Minio:AccessKey"], 
        builder.Configuration["Minio:SecretKey"])
    .WithSSL(bool.Parse(builder.Configuration["Minio:WithSSL"] ?? "false"))
    .Build());

builder.Services.AddScoped<IMinioStorageService, MinioStorageService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortIACoreDbContext>();
    db.Database.Migrate();
}

app.UseHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
