# MicroShop

MicroShop is a compact backend pet project for managing a product catalog and inventory. Built with .NET 10, it consists of two independent services connected through Apache Kafka.

Catalog manages product data. Inventory tracks stock quantities and creates an inventory item when a product is added to the catalog. Transactional Outbox and Inbox processing keep this asynchronous integration reliable and idempotent.

The scope is intentionally focused: product creation, product lookup, inventory lookup, and quantity updates. There is no frontend, authentication, or checkout flow.

## Technology stack

- **Backend:** .NET 10, ASP.NET Core Minimal API.
- **Application:** Clean Architecture, DDD, CQRS, MediatR, FluentValidation.
- **Persistence:** Entity Framework Core, PostgreSQL, database per service.
- **Messaging:** Apache Kafka in KRaft mode, Confluent.Kafka, JSON integration contracts, Outbox and Inbox.
- **Observability:** Serilog, OpenTelemetry metrics, Prometheus, Grafana.
- **Tooling:** Docker Compose, multi-stage Docker builds, Swagger UI and OpenAPI via Swashbuckle.

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

Services share integration contracts through `MicroShop.Contracts` and technical primitives through BuildingBlocks. They do not reference each other's implementation projects or access each other's database. Kafka messages contain integration contracts, not domain entities.

## Services and endpoints

| Service | Method | Route | Operation |
| --- | --- | --- | --- |
| Catalog | POST | `/api/products` | Create a product |
| Catalog | GET | `/api/products/{id}` | Get a product by ID |
| Inventory | GET | `/api/inventory/{productId}` | Get inventory for a product |
| Inventory | PUT | `/api/inventory/{productId}` | Set the quantity of an existing inventory item |

Product names are required, and prices cannot be negative. Inventory quantities cannot be negative; each product has at most one inventory item. Newly created inventory items start with quantity `0`.

Both services expose `/health` (including their PostgreSQL connectivity) and `/metrics`.

## CQRS, DDD, and validation

Minimal API endpoints create a command or query and send it through MediatR. FluentValidation runs in `ValidationBehavior`; endpoints do not invoke validators or repositories. Expected validation, not-found, and conflict failures use `Result`/`Result<T>` and map to Problem Details responses.

`Product.Create`, `InventoryItem.Create`, and `InventoryItem.ChangeQuantity` protect domain invariants. Entities encapsulate state changes; handlers coordinate domain behavior and persistence through explicit repository interfaces.

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

A publish failure is logged and stored in `Error`; the message remains unprocessed and is retried by a subsequent processing cycle.

## Messaging

- **Producer:** Catalog's `KafkaProducer`, built with `ProducerBuilder<string, string>`.
- **Topic:** the single topic is `catalog.product-created`.
- **Message key:** `ProductId.ToString()`. Kafka hashes the key to select a partition; related events for one product therefore route consistently. The application does not manually choose a partition.
- **Partition:** an ordered log shard inside the topic. Produced and consumed partition numbers are written to structured logs.
- **Consumer:** Inventory's `ProductCreatedConsumer`, built with `ConsumerBuilder<string, string>`.
- **Consumer group:** `inventory-service`. Kafka tracks progress independently for this group and assigns partitions among its consumers.
- **Offset:** a message's position inside one partition. Topic, partition, offset, key, event ID, and product ID are logged on consumption.
- **Commit:** the consumer has `EnableAutoCommit = false` and calls `Commit` only after its database transaction succeeds.

Delivery follows **at-least-once** semantics. A failure between database persistence and offset commit can cause redelivery. Inbox deduplication makes repeated processing safe; Kafka alone does not provide exactly-once business processing.

## Inbox and idempotency

`ProductCreatedConsumer` uses `ProductCreatedIntegrationEvent.Id` as the `inbox_messages` primary key. On first delivery it starts an Inventory database transaction, adds the Inbox record and an `InventoryItem` with quantity `0`, calls `SaveChangesAsync`, and commits the database transaction. Only after that does it commit the Kafka offset.

On redelivery, the existing Inbox ID causes the business operation to be skipped; the consumer can safely commit that Kafka offset. A unique index on `inventory_items.product_id` supplies a second database-level duplicate guard.

Processing order:

```text
Consume → check Inbox → save Inbox + Inventory → commit DB → commit Kafka offset
```

Saving the database transaction before committing the offset prevents an acknowledged message from being lost before its business change is persisted.

## Getting started

### Prerequisites

- Docker with Docker Compose and Linux container support.
- .NET 10 SDK for building or running the APIs outside Docker.

Run all commands from the repository root.

### Start with Docker Compose

```powershell
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs -f catalog-api inventory-api kafka
```

Compose starts both APIs, two PostgreSQL instances, Kafka, Prometheus, and Grafana on a shared Docker network. EF Core migrations are applied automatically when the APIs start.

| Service | URL |
| --- | --- |
| Catalog API | [http://localhost:5001](http://localhost:5001) |
| Inventory API | [http://localhost:5002](http://localhost:5002) |
| Prometheus | [http://localhost:9090](http://localhost:9090) |
| Grafana | [http://localhost:3000](http://localhost:3000) |

The default local credentials are `postgres` / `postgres` for PostgreSQL and `admin` / `admin` for Grafana. This Compose configuration is intended for local use and is not a production deployment configuration.

### Build the solution

```powershell
dotnet restore MicroShop.sln
dotnet build MicroShop.sln --no-restore
```

### Run APIs from Rider or the CLI

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

### Stop or reset the environment

Stop containers while retaining named volumes:

```powershell
docker compose down
```

Reset the environment, permanently deleting all project data stored in named volumes:

```powershell
docker compose down -v
```

PostgreSQL uses `postgres:18-alpine` with volumes mounted at `/var/lib/postgresql`. Existing clusters from another PostgreSQL major version require a planned upgrade; changing the image tag does not migrate their data.

## Configuration

Local API settings are defined in each service's `appsettings.json`. Docker Compose overrides the same settings through `ConnectionStrings__Database` and `Kafka__BootstrapServers`.

| Dependency | Docker API address | Host API address |
| --- | --- | --- |
| Catalog PostgreSQL | `catalog-db:5432`, database `catalog` | `localhost:5433`, database `catalog` |
| Inventory PostgreSQL | `inventory-db:5432`, database `inventory` | `localhost:5434`, database `inventory` |
| Kafka | `kafka:9092` | `localhost:9094` |

Kafka exposes separate INTERNAL and EXTERNAL listeners because clients use broker addresses returned in metadata. Docker clients receive `kafka:9092`; host clients receive `localhost:9094`. The controller listener remains private on port `9093`. Both APIs use strongly typed `KafkaOptions`; address selection is configuration-driven.

## API documentation

| Service | Swagger UI | OpenAPI JSON |
| --- | --- | --- |
| Catalog | [API reference](http://localhost:5001/swagger) | [OpenAPI document](http://localhost:5001/swagger/v1/swagger.json) |
| Inventory | [API reference](http://localhost:5002/swagger) | [OpenAPI document](http://localhost:5002/swagger/v1/swagger.json) |

Both pages work in the normal Docker Compose environment, including `Production`;
`/` redirects to `/swagger`. Swashbuckle generates one `v1` OpenAPI document from
each service's Minimal API endpoints. Swagger UI includes **Try it out** for
executing requests and serves its browser assets directly from the API.

## API usage

Create a product:

```powershell
$created = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:5001/api/products `
  -ContentType application/json `
  -Body '{"name":"Keyboard","price":79.90}'

$productId = $created.value
```

The response contains the new ID in `value`. Read the product and its inventory:

```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/products/$productId"
Invoke-RestMethod -Uri "http://localhost:5002/api/inventory/$productId"
```

Inventory creation is asynchronous. An immediate inventory lookup may return `404` until the integration event has been processed; retry the lookup shortly afterward.

Change and re-read the quantity:

```powershell
Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:5002/api/inventory/$productId" `
  -ContentType application/json `
  -Body '{"quantity":12}'

Invoke-RestMethod -Uri "http://localhost:5002/api/inventory/$productId"
```

## Operations

### PostgreSQL

```powershell
docker compose exec catalog-db psql -U postgres -d catalog -c "select * from products;"
docker compose exec catalog-db psql -U postgres -d catalog -c "select id, type, occurred_at_utc, processed_at_utc, error from outbox_messages;"
docker compose exec inventory-db psql -U postgres -d inventory -c "select * from inbox_messages;"
docker compose exec inventory-db psql -U postgres -d inventory -c "select * from inventory_items;"
```

### Kafka CLI

Compose uses `apache/kafka:4.1.1`. The following commands run the bundled Kafka utilities inside the broker container.

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

## Observability

Serilog provides centralized HTTP request logs, MediatR request execution logs, and structured Kafka/Outbox/Inbox diagnostics. Consumer logs include topic, partition, offset, key, and event ID.

Both APIs emit ASP.NET Core and .NET runtime metrics through OpenTelemetry at `/metrics`. Prometheus scrapes `catalog-api:8080` and `inventory-api:8080` every five seconds. Grafana is provisioned with Prometheus as its default data source and a small MicroShop dashboard showing HTTP request rate, average duration, and runtime heap size.

Health and scrape checks:

```powershell
Invoke-WebRequest http://localhost:5001/health
Invoke-WebRequest http://localhost:5002/health
Invoke-WebRequest http://localhost:9090/api/v1/targets
```

## Repository structure

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
