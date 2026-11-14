using Microsoft.EntityFrameworkCore;
using Reunioes.Dados.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseProvider = builder.Configuration["Database:Provider"] ?? "SQLite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=reunioes.db";

builder.Services.AddDbContext<ReunioesDbContext>(options =>
{
    if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        options.UseNpgsql(connectionString);
    else if (databaseProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connectionString);
    else
        throw new InvalidOperationException(
            $"Provedor de banco de dados '{databaseProvider}' não suportado. Use SQLite ou PostgreSQL.");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
