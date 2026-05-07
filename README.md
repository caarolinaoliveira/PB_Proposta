# PB.Proposta — MS Análise de Crédito

Microsserviço responsável pela análise de crédito. Consome o evento `ClienteCadastrado` do RabbitMQ, calcula o score, aplica as regras de negócio e publica o evento `CreditoAprovado` caso a proposta seja aprovada.

## Tecnologias

- .NET 10
- ASP.NET Core Web API + BackgroundService
- Entity Framework Core + SQL Server
- RabbitMQ.Client 6.8.1
- Clean Architecture + DDD

## Estrutura

```
PB.Proposta/
  PB.Proposta.Domain/           # Entidades, interfaces, enums
  PB.Proposta.Application/      # Services, interfaces, eventos
  PB.Proposta.Infrastructure/   # EF Core, repositórios, RabbitMQ, e-mail
  PB.Proposta.Presentation/     # Controllers, middlewares, Program.cs
  Testes/                       # Testes unitários
```

## Regras de score

| Score | Resultado | Limite | Cartões |
|-------|-----------|--------|---------|
| 0 – 100 | Negado | — | 0 |
| 101 – 500 | Aprovado | R$ 1.000,00 | 1 |
| 501 – 1000 | Aprovado | R$ 5.000,00 | 2 |

> O score é gerado de forma aleatória simulando um bureau de crédito. Em produção, viria de uma integração com Serasa/SPC.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- MS Cliente rodando e publicando eventos

## Como rodar localmente

### 1. Subir a infraestrutura

Na raiz da solution (onde está o `docker-compose.yml`), sobe o RabbitMQ e o SQL Server:

```bash
docker-compose up -d rabbitmq sqlserver
```

Confirma que os containers estão rodando:

```bash
docker ps
```

Você deve ver:

```
pb_rabbitmq    → portas 5672 e 15672
pb_sqlserver   → porta 1433
```

> Painel do RabbitMQ disponível em http://localhost:15672 com usuário `guest` e senha `guest`

### 2. Configurar o appsettings

No arquivo `PB.Proposta/appsettings.json`, configure as credenciais de acordo com o ambiente:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=PB_Propostas;User Id=sa;Password=Pb@123456;TrustServerCertificate=True"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "User": "guest",
    "Password": "guest"
  }
}
```

> Para usar o Azure SQL Server, substitua a connection string mantendo `Database=PB_Propostas`.

### 3. Criar e aplicar migrations

```bash
cd PB.Proposta
dotnet ef migrations add InitialCreate --project PB.Proposta.Infrastructure --startup-project .
dotnet ef database update --project PB.Proposta.Infrastructure --startup-project .
```

> A migration também é aplicada automaticamente na inicialização via `MigrateAsync()` 

### 4. Rodar o serviço

```bash
cd PB.Proposta
dotnet run
```

O consumer ficará aguardando mensagens na fila `cliente.cadastrado`:


A API estará disponível para consulta de propostas em:

```
http://localhost:5263/swagger/index.html
```

## Infraestrutura Docker

O `docker-compose.yml` sobe dois serviços compartilhados entre todos os microsserviços:

| Container | Imagem | Porta | Uso |
|---|---|---|---|
| pb_rabbitmq | rabbitmq:3-management | 5672 / 15672 | Broker de mensagens + painel web |
| pb_sqlserver | mssql/server:2022 | 1433 | Banco de dados SQL Server |

Os dados são persistidos em volumes Docker (`rabbitmq_data` e `sqlserver_data`) — reiniciar os containers não apaga filas nem bancos.

Comandos úteis:

```bash
# Parar os containers
docker-compose down

# Parar e apagar todos os dados (reset total)
docker-compose down -v

# Ver logs do RabbitMQ
docker logs pb_rabbitmq

# Ver logs do SQL Server
docker logs pb_sqlserver
```

## Fluxo do evento

```
RabbitMQ: cliente.cadastrado
        ↓
Verifica idempotência (ClienteId já processado?)
        ↓
Calcula score (0–1000)
        ↓
Cria PropostaEntity (regras aplicadas no Domain)
        ↓
Persiste no banco (PB_Propostas)
        ↓
Negada → [EMAIL] Proposta negada → fim
        ↓
Aprovada → [EMAIL] Proposta aprovada
        ↓
Publica → RabbitMQ: credito.aprovado
```

## Resiliência

O consumer implementa retry automático via `BasicNack(requeue: true)` — em caso de falha no processamento, a mensagem é devolvida à fila e reprocessada automaticamente.

## Decisões arquiteturais

- **API + BackgroundService**: a API expõe o endpoint de consulta de propostas enquanto o consumer roda em background no mesmo host, sem necessidade de dois processos separados.
- **Idempotência**: antes de processar, verifica se já existe uma proposta para o `ClienteId`. Protege contra reprocessamento de mensagens duplicadas em cenários de retry.
- **Regras de negócio no Domain**: a `PropostaEntity` aplica as regras de score internamente no construtor — o `PropostaService` não conhece os valores de score.
- **Migration automática**: o banco é criado e atualizado automaticamente na inicialização via `MigrateAsync()`.
- **Evento rico**: o `CreditoAprovadoEvent` carrega todos os dados necessários para o MS Cartão, evitando consultas adicionais entre serviços.