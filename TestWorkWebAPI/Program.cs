using TestWorkWebAPI;
using Microsoft.EntityFrameworkCore;
using DataAccess;
using TestWorkWebAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AppConfig>();
builder.Services.AddDbContext<DataContext>((serviceProvider, options) =>
{
    var appConfig = serviceProvider.GetRequiredService<AppConfig>();
    var connectionString = appConfig.GetConnectionString("WebApiDataBase");
    options.UseNpgsql(connectionString); 
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("api/Values/upload", FileEndpoints.FirstMethod)
    .DisableAntiforgery()
    .WithOpenApi();



app.Run();

