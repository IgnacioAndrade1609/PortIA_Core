using Microsoft.EntityFrameworkCore;
using PortIA.Core.DependencyInjection;
using PortIA.Core.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAppConfiguration();
builder.Services.AddAll(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5173", "http://localhost:3000") // Añade los puertos comunes de Angular/Vite
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
