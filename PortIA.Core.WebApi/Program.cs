using PortIA.Core.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAppConfiguration();
builder.Services.AddAll(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

app.UseHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
