# MicroShop

MicroShop is a deliberately small .NET 10 microservices project for learning the complete path from an HTTP request to durable asynchronous processing:

`HTTP → CQRS → DDD → EF Core → PostgreSQL → Outbox → Kafka → Consumer Group → Inbox → PostgreSQL`

It contains exactly two independently deployable business services. Catalog owns products; Inventory owns stock quantities. They share only an integration-event contract and technical building blocks. Neither service reads the other service's database.

## Architecture

```text
Client
  |
  v
Catalog API
  |
  v
Catalog PostgreSQL
  | products
  | outbox_messages
  v
OutboxProcessor
  |
  v
Kafka Producer
  |
  v
catalog.product-created
  |
  v
Kafka Consumer
group: inventory-service
  |
  v
Inventory PostgreSQL
  | inbox_messages
  | inventory_items
```

Each service follows Clean Architecture:

- **Domain** contains entities, behavior, and business errors. It has no EF Core, Kafka, or HTTP concerns.
- **Application** contains CQRS commands, queries, handlers, validators, and repository abstractions.
- **Infrastructure** implements EF Core persistence and Kafka integration.
- **Api** is the Minimal API composition root and contains thin endpoint classes.

The shared `MicroShop.Contracts` project contains only `ProductCreatedIntegrationEvent`. Domain entities are never put on Kafka.

## Services and endpoints

Catalog owns `Product` (`Id`, `Name`, `Price`, `CreatedAtUtc`):

- `POST /api/products`
- `GET /api/products/{id}`

Inventory owns `InventoryItem` (`Id`, `ProductId`, `Quantity`, `CreatedAtUtc`, `UpdatedAtUtc`):

- `GET /api/inventory/{productId}`
- `PUT /api/inventory/{productId}`

Both services expose `/health` (including their PostgreSQL connectivity) and `/metrics`.

## CQRS, DDD, and validation

Minimal API endpoints create a command or query and send it through MediatR. FluentValidation runs in `ValidationBehavior`; endpoints do not invoke validators or repositories. Expected validation, not-found, and conflict failures use `Result`/`Result<T>` and map to Problem Details responses.

`Product.Create`, `InventoryItem.Create`, and `InventoryItem.ChangeQuantity` protect domain invariants. Setters remain private, mapping is explicit, and no generic repository or AutoMapper is used.

## Database per service

Catalog connects to the `catalog` PostgreSQL database and owns:

- `products`
- `outbox_messages`

Inventory connects to the separate `inventory` PostgreSQL database and owns:

- `inventory_items`
- `inbox_messages`

There are no cross-database queries or foreign keys. Each `DbContext` is its service's transaction boundary. Initial EF Core migrations are included in each Infrastructure project and are applied when its API starts.

## Transactional Outbox

`CreateProductCommandHandler` creates a `Product` and a `ProductCreatedIntegrationEvent`, then asks `ProductRepository` and `OutboxWriter` to track both records in the same `CatalogDbContext`. One `SaveChangesAsync` atomically inserts the product and its Outbox message. The HTTP request never publishes directly to Kafka.

`OutboxProcessor` reads a small batch of unprocessed messages. For each supported contract it:

1. deserializes the JSON contract;
2. calls the direct Confluent.Kafka producer;
3. waits for Kafka delivery acknowledgement;
4. sets `ProcessedAtUtc`, clears `Error`, and saves.

A publish failure is logged and stored in `Error`; `ProcessedAtUtc` stays null so a later loop can retry.

## Kafka concepts in this project

- **Producer:** Catalog's `KafkaProducer`, built with `ProducerBuilder<string, string>`.
- **Topic:** the single topic is `catalog.product-created`.
- **Message key:** `ProductId.ToString()`. Kafka hashes the key to select a partition; related events for one product therefore route consistently. The application does not manually choose a partition.
- **Partition:** an ordered log shard inside the topic. Produced and consumed partition numbers are written to structured logs.
- **Consumer:** Inventory's `ProductCreatedConsumer`, built with `ConsumerBuilder<string, string>`.
- **Consumer group:** `inventory-service`. Kafka tracks progress independently for this group and assigns partitions among its consumers.
- **Offset:** a message's position inside one partition. Topic, partition, offset, key, event ID, and product ID are logged on consumption.
- **Commit:** the consumer has `EnableAutoCommit = false` and calls `Commit` only after its database transaction succeeds.

This is intentionally an **at-least-once** flow. A crash can happen after the database commit and before the Kafka offset commit. Kafka will then redeliver the event, which is why Inventory needs an Inbox rather than pretending business processing is exactly once.

## Inbox and idempotency

`ProductCreatedConsumer` uses `ProductCreatedIntegrationEvent.Id` as the `inbox_messages` primary key. On first delivery it starts an Inventory database transaction, adds the Inbox record and an `InventoryItem` with quantity `0`, calls `SaveChangesAsync`, and commits the database transaction. Only after that does it commit the Kafka offset.

On redelivery, the existing Inbox ID causes the business operation to be skipped; the consumer can safely commit that Kafka offset. A unique index on `inventory_items.product_id` supplies a second database-level duplicate guard.

The important order is:

```text
Consume → check Inbox → save Inbox + Inventory → commit DB → commit Kafka offset
```

Committing the offset first would allow a crash to lose the business change permanently.

## Run locally

Requirements: .NET 10 SDK, Docker, and Docker Compose.

PostgreSQL uses the pinned `postgres:18-alpine` image. Its named volumes mount at
`/var/lib/postgresql`; PostgreSQL 18 stores its cluster under `18/docker` inside
that mount. Mounting at the older `/var/lib/postgresql/data` path prevents this
image from starting. Correcting the mount does not require deleting empty volumes.
Existing database data from a different major version requires a planned upgrade,
not simply changing the image tag or deleting the volume.

The existing `appsettings.json` files configure Windows-hosted API development:
Catalog connects to `localhost:5433/catalog`, and Inventory to
`localhost:5434/inventory`. Compose overrides the same `ConnectionStrings:Database`
key through `ConnectionStrings__Database`, using `catalog-db:5432/catalog` and
`inventory-db:5432/inventory`. All database settings use the same development-only
`postgres` user and password. A local database connection error is expected while
the corresponding Docker database is stopped.

Kafka has two listeners because clients use the broker addresses returned in Kafka
metadata, not just the initial bootstrap address. Docker clients use the INTERNAL
listener advertised as `kafka:9092`. Windows/Rider clients use the EXTERNAL listener
advertised as `localhost:9094`, published on host port `9094`. Compose overrides
`Kafka__BootstrapServers`; the local `appsettings.json` files use `localhost:9094`.
The controller listener remains private on port `9093`.

```powershell
dotnet restore MicroShop.sln
dotnet build MicroShop.sln
docker compose up -d --build
docker compose ps
docker compose logs -f catalog-api inventory-api kafka
```

Service URLs:

- Catalog: `http://localhost:5001`
- Inventory: `http://localhost:5002`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000` (`admin` / `admin`)

To run the APIs from Rider or `dotnet run` against the Docker infrastructure,
stop the Docker APIs first to avoid two Outbox processors sharing the same database:

```powershell
docker compose stop catalog-api inventory-api
dotnet run --project src/Services/Catalog/MicroShop.Catalog.Api
# In another terminal:
dotnet run --project src/Services/Inventory/MicroShop.Inventory.Api
```

The launch profiles select `Development`, Catalog port `5101`, and Inventory port
`5102`. Local Swagger URLs are `http://localhost:5101/swagger` and
`http://localhost:5102/swagger`. Stop the local processes before returning to
containerized execution with `docker compose start catalog-api inventory-api`.

Stop containers while retaining named volumes:

```powershell
docker compose down
```

Stop containers and delete all MicroShop data:

```powershell
docker compose down -v
```

## API documentation

| Service | Swagger UI interactive API reference | OpenAPI JSON machine-readable API contract |
| --- | --- | --- |
| Catalog | http://localhost:5001/swagger | http://localhost:5001/swagger/v1/swagger.json |
| Inventory | http://localhost:5002/swagger | http://localhost:5002/swagger/v1/swagger.json |

Both pages work in the normal Docker Compose environment, including `Production`;
`/` redirects to `/swagger`. Swashbuckle generates one `v1` OpenAPI document from
each service's Minimal API endpoints. Swagger UI includes **Try it out** for
executing requests and serves its browser assets directly from the API.

## Exercise the API

Create a product:

```powershell
$created = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5001/api/products `
  -ContentType application/json `
  -Body '{"name":"Keyboard","price":79.90}'

$productId = $created.value
```

Read the product and the asynchronously created inventory row:

```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/products/$productId"
Invoke-RestMethod -Uri "http://localhost:5002/api/inventory/$productId"
```

Change and re-read the quantity:

```powershell
Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:5002/api/inventory/$productId" `
  -ContentType application/json `
  -Body '{"quantity":12}'

Invoke-RestMethod -Uri "http://localhost:5002/api/inventory/$productId"
```

## Inspect PostgreSQL

```powershell
docker compose exec catalog-db psql -U postgres -d catalog -c "select * from products;"
docker compose exec catalog-db psql -U postgres -d catalog -c "select id, type, occurred_at_utc, processed_at_utc, error from outbox_messages;"
docker compose exec inventory-db psql -U postgres -d inventory -c "select * from inbox_messages;"
docker compose exec inventory-db psql -U postgres -d inventory -c "select * from inventory_items;"
```

## Learn with the Kafka CLI

Compose uses the official `apache/kafka:4.1.1` image. Its Kafka scripts are under `/opt/kafka/bin`, so these commands execute inside that exact container image.

List topics:

```powershell
docker compose exec kafka /opt/kafka/bin/kafka-topics.sh --bootstrap-server kafka:9092 --list
```

Describe the topic, including partitions and leaders:

```powershell
docker compose exec kafka /opt/kafka/bin/kafka-topics.sh --bootstrap-server kafka:9092 --describe --topic catalog.product-created
```

Read existing and new messages while printing keys, partitions, and offsets:

```powershell
docker compose exec kafka /opt/kafka/bin/kafka-console-consumer.sh --bootstrap-server kafka:9092 --topic catalog.product-created --from-beginning --property print.key=true --property print.partition=true --property print.offset=true
```

Inspect the Inventory consumer group and its committed offsets:

```powershell
docker compose exec kafka /opt/kafka/bin/kafka-consumer-groups.sh --bootstrap-server kafka:9092 --describe --group inventory-service
```

The `CURRENT-OFFSET`, `LOG-END-OFFSET`, and `LAG` columns show what the group has committed and how far it is behind each partition.

## Prometheus and Grafana

Both APIs emit ASP.NET Core and .NET runtime metrics through OpenTelemetry at `/metrics`. Prometheus scrapes `catalog-api:8080` and `inventory-api:8080` every five seconds. Grafana is provisioned with Prometheus as its default data source and a small MicroShop dashboard showing HTTP request rate, average duration, and runtime heap size.

Useful checks:

```powershell
Invoke-WebRequest http://localhost:5001/health
Invoke-WebRequest http://localhost:5002/health
Invoke-WebRequest http://localhost:9090/api/v1/targets
```

## Solution map

```text
src/
  BuildingBlocks/
    MicroShop.BuildingBlocks.Domain
    MicroShop.BuildingBlocks.Application
    MicroShop.BuildingBlocks.Infrastructure
  Contracts/
    MicroShop.Contracts
  Services/
    Catalog/
      MicroShop.Catalog.Domain
      MicroShop.Catalog.Application
      MicroShop.Catalog.Infrastructure
      MicroShop.Catalog.Api
    Inventory/
      MicroShop.Inventory.Domain
      MicroShop.Inventory.Application
      MicroShop.Inventory.Infrastructure
      MicroShop.Inventory.Api
deploy/
  prometheus/
  grafana/
```
