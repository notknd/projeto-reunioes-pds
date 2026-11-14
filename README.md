# Sistema de Gerenciamento de Salas de Reunião

API em .NET 8 para gerenciar salas e reservas. A aplicação usa SQLite por padrão no desenvolvimento local e também pode usar PostgreSQL via Docker Compose.

## Requisitos

- .NET 8 SDK
- Docker Compose (para usar PostgreSQL)

## Executar com SQLite

```bash
dotnet run --project Reunioes.Api
```

O banco SQLite é criado no arquivo `reunioes.db`. O provedor padrão pode ser configurado por `Database:Provider` e `ConnectionStrings:DefaultConnection`.

## Executar com PostgreSQL

Copie `.env.example` para `.env`, altere a senha local e inicie o banco:

```bash
docker compose up -d postgres
```

No terminal que executa a API, configure as variáveis de ambiente correspondentes e inicie:

```bash
$env:Database__Provider = "PostgreSQL"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=reunioes;Username=reunioes;Password=sua-senha-local"
dotnet run --project Reunioes.Api
```

No Linux/macOS, use `export` no lugar de `$env:`. Não armazene credenciais reais no repositório. Para voltar ao SQLite, use `Database__Provider=SQLite` e `ConnectionStrings__DefaultConnection="Data Source=reunioes.db"`.

A documentação Swagger fica disponível em `/swagger` no ambiente de desenvolvimento.
