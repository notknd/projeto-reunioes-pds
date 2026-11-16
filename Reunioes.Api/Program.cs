using Reunioes.Api.Endpoints;
using Reunioes.Dados.Conexao;
using Reunioes.Dados.Repositorios;
using Reunioes.Negocio.Servicos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=reunioes.db";

builder.Services.AddSingleton(new SqliteConnectionFactory(connectionString));
builder.Services.AddScoped<DatabaseInitializer>(services => new DatabaseInitializer(
    services.GetRequiredService<SqliteConnectionFactory>()));
builder.Services.AddScoped<RoomRepository>();
builder.Services.AddScoped<ReservationRepository>();

builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<RoomService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapRoomEndpoints();
app.MapReservationEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();

app.Run();
