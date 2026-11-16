# Sistema de Gerenciamento de Salas de Reunião

API em .NET 8 para gerenciar salas e reservas. A aplicação usa SQL parametrizado com ADO.NET e SQLite local.

## Organização

- `Reunioes.Api`: apresentação HTTP (Minimal APIs) e configuração da aplicação.
- `Reunioes.Negocio`: serviços, validações e regras de horários/conflitos.
- `Reunioes.Dados`: entidades, conexão e repositórios com comandos SQL parametrizados.

Os repositórios abrem conexões e usam parâmetros para enviar valores ao banco. A criação e o reagendamento verificam conflitos e gravam a alteração dentro de uma transação.

## Executar localmente

Requisito: .NET 8 SDK. Não é necessário instalar ou iniciar um servidor de banco de dados.

```bash
dotnet restore .\Reunioes.slnx
dotnet run --project .\Reunioes.Api\Reunioes.Api.csproj --launch-profile http
```

Abra `http://localhost:5100/swagger` para consultar e executar os endpoints. O SQLite cria o arquivo `reunioes.db` automaticamente ao iniciar a API.

A documentação Swagger fica disponível em `/swagger` no ambiente de desenvolvimento.

## Endpoints

### Salas

| Método e rota | Ação |
| --- | --- |
| `POST /api/rooms` | Cadastra uma sala (`name`, `floor`, `capacity`). |
| `GET /api/rooms/{roomId}` | Consulta uma sala. |
| `PUT /api/rooms/{roomId}` | Atualiza nome, andar e capacidade. |
| `DELETE /api/rooms/{roomId}` | Exclui a sala se não houver reservas vinculadas. |
| `GET /api/rooms?page=1&name=Sala` | Lista 10 salas por página, filtra pelo nome e ordena por andar crescente. |
| `GET /api/rooms/availability?startDate=2026-11-14&endDate=2026-11-15` | Retorna horas livres de cada sala no período, contando as datas inicial e final e o expediente das 08:00 às 19:00. |

### Reservas

| Método e rota | Ação |
| --- | --- |
| `POST /api/reservations` | Cria uma reserva. |
| `PUT /api/reservations/{reservationId}` | Reagenda uma reserva futura. |
| `DELETE /api/reservations/{reservationId}` | Cancela uma reserva. |
| `GET /api/metrics/reservations/last-7-days` | Conta reservas criadas nos últimos sete dias; o intervalo retornado usa UTC. |

Exemplo de criação (substitua `roomId` pelo identificador retornado ao criar a sala):

```json
{
  "roomId": "00000000-0000-0000-0000-000000000000",
  "startTime": "2026-11-16T10:00:00",
  "endTime": "2026-11-16T11:00:00"
}
```

As reservas precisam começar depois das 08:00 e terminar antes das 19:00, no mesmo dia. Horários adjacentes são aceitos; sobreposição na mesma sala retorna `409 Conflict`. Erros de validação retornam `400 Bad Request` e recursos inexistentes retornam `404 Not Found`.

O banco é criado automaticamente ao iniciar a API. `schema.sql` contém o script de criação das tabelas SQLite.
