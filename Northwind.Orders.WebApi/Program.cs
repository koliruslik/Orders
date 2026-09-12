using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Northwind.Orders.WebApi.Services;
using Northwind.Services.EntityFramework.Entities;
using Northwind.Services.EntityFramework.Repositories;
using Northwind.Services.Repositories;

var builder = WebApplication.CreateBuilder(args);

var configuredConnectionString = builder.Configuration.GetConnectionString("NorthwindDatabase")
    ?? throw new InvalidOperationException("The `NorthwindDatabase` connection string is not set.");
var connectionStringBuilder = new SqliteConnectionStringBuilder(configuredConnectionString);
bool isInMemory = connectionStringBuilder.Mode == SqliteOpenMode.Memory
    || connectionStringBuilder.DataSource == ":memory:";

if (isInMemory)
{
    connectionStringBuilder.Mode = SqliteOpenMode.Memory;
    connectionStringBuilder.Cache = SqliteCacheMode.Shared;
    if (connectionStringBuilder.DataSource == ":memory:")
    {
        connectionStringBuilder.DataSource = "Northwind";
    }
}
else
{
    connectionStringBuilder.DataSource = Path.GetFullPath(
        connectionStringBuilder.DataSource,
        builder.Environment.ContentRootPath);
}

var connectionString = connectionStringBuilder.ToString();
using var databaseService = new DatabaseService(connectionString);
if (isInMemory || !File.Exists(connectionStringBuilder.DataSource))
{
    databaseService.InitializeDatabase();
}

// Add services to the container.
builder.Services.AddDbContext<NorthwindContext>(options => options.UseSqlite(connectionString));
builder.Services.AddControllers();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();
