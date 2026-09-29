# MicroShop Development Rules

## 1. Project purpose

MicroShop is a small educational microservices project.

The goal is to learn and demonstrate a clean implementation of:

- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core
- PostgreSQL
- Clean Architecture
- DDD
- CQRS
- MediatR
- FluentValidation
- Apache Kafka
- Confluent.Kafka
- Transactional Outbox Pattern
- Inbox Pattern
- Serilog
- OpenTelemetry
- Prometheus
- Grafana
- Docker
- Docker Compose

The project must remain small, understandable, and educational.

Do not turn it into a large enterprise system.

The main integration scenario is:

Catalog  
→ PostgreSQL  
→ Outbox  
→ Kafka Producer  
→ Kafka Topic  
→ Kafka Consumer  
→ Inbox  
→ Inventory  
→ PostgreSQL

Every file should have a clear reason to exist.

---

# 2. Reference project

Use the following project as a coding-style and architecture reference:

https://github.com/Kishotta/modular-monolith-course/tree/main/src/Evently

Take inspiration from:

- Domain / Application / Infrastructure separation
- CQRS
- MediatR
- Result pattern
- feature-oriented folders
- thin endpoints
- endpoint discovery
- explicit domain behavior
- dependency injection extension methods
- primary constructors
- clean and readable code

Important:

MicroShop is NOT a modular monolith.

It contains independent microservices.

Do not copy Evently architecture literally.

Adapt its style to microservices.

---

# 3. Microservices

There are exactly two business microservices:

1. Catalog
2. Inventory

Do not create additional services unless explicitly requested.

Do not add:

- Ordering
- Payments
- Users
- Notifications
- API Gateway

---

# 4. Main business flow

The main distributed flow is:

POST /api/products

↓

Catalog API

↓

CreateProductCommand

↓

CreateProductCommandHandler

↓

Product domain entity

↓

Catalog PostgreSQL

- products
- outbox_messages

↓

OutboxProcessor

↓

KafkaProducer

↓

Kafka topic:

catalog.product-created

↓

Kafka partition

↓

ProductCreatedConsumer

↓

Inventory PostgreSQL

- inbox_messages
- inventory_items

When a Product is created:

1. Catalog creates Product.
2. Catalog creates ProductCreatedIntegrationEvent.
3. Product and OutboxMessage are persisted in one database transaction.
4. OutboxProcessor reads the unprocessed message.
5. KafkaProducer publishes it.
6. Kafka stores it in the configured topic.
7. Inventory consumes the message.
8. Inventory checks Inbox by EventId.
9. Duplicate messages are ignored safely.
10. Inventory creates InventoryItem with Quantity = 0.
11. InboxMessage and InventoryItem are persisted transactionally.
12. Kafka offset is committed only after successful database processing.

---

# 5. Solution structure

Use approximately:

```text
MicroShop.sln

src/
    BuildingBlocks/
        MicroShop.BuildingBlocks.Domain/
        MicroShop.BuildingBlocks.Application/
        MicroShop.BuildingBlocks.Infrastructure/

    Contracts/
        MicroShop.Contracts/

    Services/
        Catalog/
            MicroShop.Catalog.Api/
            MicroShop.Catalog.Application/
            MicroShop.Catalog.Domain/
            MicroShop.Catalog.Infrastructure/

        Inventory/
            MicroShop.Inventory.Api/
            MicroShop.Inventory.Application/
            MicroShop.Inventory.Domain/
            MicroShop.Inventory.Infrastructure/

deploy/
    prometheus/
    grafana/

Directory.Build.props
Directory.Packages.props
.editorconfig
docker-compose.yml
README.md
```

---

# 6. Dependency rules

## Domain

Domain:

- may reference BuildingBlocks.Domain
- must not reference Application
- must not reference Infrastructure
- must not reference Api
- must not reference EF Core
- must not reference Kafka
- must not contain HTTP concerns

Domain contains:

- entities
- domain rules
- domain errors
- domain methods
- repository abstractions when appropriate
- domain events only when actually needed

---

## Application

Application:

- references Domain
- may reference BuildingBlocks.Application
- contains Commands
- contains Queries
- contains handlers
- contains validators
- contains application abstractions
- contains response models

Application must not contain:

- EF Core implementation
- Kafka implementation
- HTTP-specific logic
- Docker-specific logic

---

## Infrastructure

Infrastructure:

- references Application
- references Domain
- may reference BuildingBlocks.Infrastructure

Infrastructure contains:

- DbContext
- EF Core configurations
- repositories
- migrations
- Kafka producer
- Kafka consumer
- Outbox
- Inbox persistence
- background services
- infrastructure-specific implementations

---

## Api

Api is the composition root.

Api contains:

- Minimal API endpoints
- dependency registration
- middleware
- Serilog setup
- health checks
- OpenTelemetry
- endpoint mapping
- application startup

Do not place business logic inside Api.

---

# 7. Service isolation

Catalog must not reference:

- Inventory.Domain
- Inventory.Application
- Inventory.Infrastructure
- Inventory.Api

Inventory must not reference:

- Catalog.Domain
- Catalog.Application
- Catalog.Infrastructure
- Catalog.Api

Both services may reference:

MicroShop.Contracts

BuildingBlocks projects where appropriate.

Each service owns its own database.

No shared DbContext.

No cross-database foreign keys.

---

# 8. BuildingBlocks

BuildingBlocks contain shared technical primitives only.

Allowed examples:

```text
Entity
IDomainEvent
Result
Result<T>
Error
ErrorType
ICommand
ICommand<TResponse>
IQuery<TResponse>
ICommandHandler
ICommandHandler<TCommand, TResponse>
IQueryHandler
ValidationBehavior
RequestLoggingBehavior
IEndpoint
EndpointExtensions
IDateTimeProvider
```

Do not put business entities into BuildingBlocks.

Do not put:

- Product
- InventoryItem
- ProductErrors
- InventoryErrors

inside BuildingBlocks.

---

# 9. Contracts

MicroShop.Contracts contains integration contracts only.

Example:

```text
ProductCreatedIntegrationEvent
```

Integration contracts:

- must not reference Domain
- must not reference Infrastructure
- must not reference EF Core
- must not reference Kafka implementation
- must not contain business behavior

Do not publish Domain entities directly to Kafka.

---

# 10. Catalog domain

Product contains:

```text
Guid Id
string Name
decimal Price
DateTime CreatedAtUtc
```

Rules:

- Name is required.
- Name has a reasonable maximum length.
- Price cannot be negative.
- State changes happen through domain methods.

Prefer:

```text
Product.Create(...)
Product.Update(...)
```

Do not expose unnecessary public setters.

Do not add:

- brands
- categories
- discounts
- reviews
- images
- warehouses

unless explicitly requested.

---

# 11. Inventory domain

InventoryItem contains:

```text
Guid Id
Guid ProductId
int Quantity
DateTime CreatedAtUtc
DateTime UpdatedAtUtc
```

Rules:

- ProductId is required.
- ProductId must be unique.
- Quantity cannot be negative.

Prefer:

```text
InventoryItem.Create(...)
InventoryItem.ChangeQuantity(...)
InventoryItem.Increase(...)
InventoryItem.Decrease(...)
```

Domain behavior belongs inside InventoryItem.

Do not mutate Quantity directly inside handlers.

Incorrect:

```csharp
inventoryItem.Quantity += command.Quantity;
```

Correct conceptually:

```csharp
var result = inventoryItem.Increase(command.Quantity);
```

---

# 12. API scope

Current Catalog endpoints:

```text
POST /api/products
GET  /api/products/{id}
```

Current Inventory endpoints:

```text
GET /api/inventory/{productId}
PUT /api/inventory/{productId}
```

Additional endpoints may be implemented later for learning.

Do not automatically generate complete CRUD for every entity.

Only implement use cases that are explicitly needed.

---

# 13. Minimal API

Do not use MVC Controllers.

Use ASP.NET Core Minimal API.

Each endpoint should implement:

```csharp
IEndpoint
```

with:

```csharp
void MapEndpoint(IEndpointRouteBuilder app);
```

Use automatic endpoint discovery.

Examples:

```text
CreateProduct.cs
GetProduct.cs
GetProducts.cs
UpdateProduct.cs
GetInventory.cs
UpdateInventory.cs
```

An endpoint should only:

1. read HTTP input
2. construct Command or Query
3. call ISender.Send(...)
4. map Result into HTTP response

Endpoints must not:

- use DbContext directly
- use repositories directly
- contain business rules
- produce Kafka messages directly

---

# 14. CQRS

Use MediatR.

Commands change state.

Queries read state.

Example Catalog structure:

```text
Products/
    CreateProduct/
        CreateProductCommand.cs
        CreateProductCommandHandler.cs
        CreateProductCommandValidator.cs

    GetProduct/
        GetProductQuery.cs
        GetProductQueryHandler.cs
        ProductResponse.cs
```

Example Inventory structure:

```text
Inventory/
    GetInventory/
        GetInventoryQuery.cs
        GetInventoryQueryHandler.cs
        InventoryResponse.cs

    UpdateInventory/
        UpdateInventoryCommand.cs
        UpdateInventoryCommandHandler.cs
        UpdateInventoryCommandValidator.cs
```

Every Command, Query, Handler, Validator and Response should normally have its own file.

Do not create:

```text
Commands.cs
Queries.cs
Handlers.cs
```

---

# 15. MediatR abstractions

Use abstractions such as:

```text
ICommand
ICommand<TResponse>

IQuery<TResponse>

ICommandHandler<TCommand>
ICommandHandler<TCommand, TResponse>

IQueryHandler<TQuery, TResponse>
```

Expected business results should use:

```text
Result
Result<T>
```

---

# 16. Result pattern

Use:

```text
Result
Result<T>
Error
ErrorType
```

Suggested ErrorType values:

```text
Failure
Validation
NotFound
Conflict
```

Expected failures must not use exceptions.

Examples:

```text
ProductErrors.NotFound(productId)

ProductErrors.InvalidPrice

InventoryErrors.NotFound(productId)

InventoryErrors.QuantityCannotBeNegative
```

Unexpected infrastructure failures may throw.

---

# 17. FluentValidation

Use FluentValidation for application input validation.

Examples:

```text
CreateProductCommandValidator
UpdateProductCommandValidator
UpdateInventoryCommandValidator
```

Register validators through assembly scanning.

Validation runs through:

```text
ValidationBehavior<TRequest, TResponse>
```

Do not call validators manually inside endpoints.

Do not duplicate application validation unnecessarily.

Domain invariants must still protect domain state.

---

# 18. PostgreSQL

Use separate PostgreSQL databases.

Catalog database:

```text
catalog
```

Tables:

```text
products
outbox_messages
```

Inventory database:

```text
inventory
```

Tables:

```text
inventory_items
inbox_messages
```

No service may directly access another service's database.

---

# 19. EF Core

Use EF Core.

Create:

```text
CatalogDbContext
InventoryDbContext
```

Use:

```text
UseNpgsql(...)
UseSnakeCaseNamingConvention()
```

Entity configuration must use:

```text
IEntityTypeConfiguration<T>
```

Examples:

```text
ProductConfiguration
InventoryItemConfiguration
OutboxMessageConfiguration
InboxMessageConfiguration
```

Use migrations.

Use:

```text
AsNoTracking()
```

for read-only queries.

Do not enable lazy loading.

Do not expose IQueryable outside Infrastructure.

Configure decimal precision explicitly.

Configure unique index for:

```text
inventory_items.product_id
```

---

# 20. Repository rules

Do NOT create a generic repository.

Use explicit repositories.

Examples:

```text
IProductRepository
ProductRepository

IInventoryRepository
InventoryRepository
```

Repositories expose only operations required by current use cases.

Do not automatically add:

```text
GetAll
Find
Search
Delete
Update
Exists
```

unless needed.

---

# 21. Automatic repository registration

Repositories must be registered automatically.

Do not manually register every repository.

Avoid:

```csharp
services.AddScoped<IProductRepository, ProductRepository>();
```

Repository convention:

Implementation:

```text
ProductRepository
```

Interface:

```text
IProductRepository
```

Both names must end with:

```text
Repository
```

Create reflection-based registration.

Register:

- concrete classes
- non-abstract classes
- names ending with Repository
- matching implemented interfaces ending with Repository

Lifetime:

```text
Scoped
```

Example conceptual usage:

```csharp
services.AddRepositories(
    typeof(InfrastructureAssemblyReference).Assembly);
```

A future repository such as:

```text
ICategoryRepository
CategoryRepository
```

must be discovered automatically without editing DI configuration.

Do not use assembly scanning for everything.

Automatic scanning applies specifically to repositories.

---

# 22. Explicit infrastructure registration

Keep explicit registration for special infrastructure components.

Examples:

```text
DbContext
KafkaProducer
Kafka consumer BackgroundService
OutboxProcessor
IDateTimeProvider
Options
OpenTelemetry
```

Do not blindly auto-register every interface.

---

# 23. Unit of Work

Use service DbContext as transaction boundary.

Application may depend on:

```text
IUnitOfWork
```

Catalog transaction:

```text
Product
+
OutboxMessage
```

must be committed together.

Inventory transaction:

```text
InboxMessage
+
InventoryItem
```

must be committed together.

Do not use distributed database transactions.

---

# 24. ID generation

When a domain factory creates a brand-new entity, that factory owns creation of the entity's identifier.

Examples:

```csharp
var product = new Product
{
    Id = Guid.NewGuid(),
    Name = name,
    Price = price,
    CreatedAtUtc = createdAtUtc
};
```

Use this for an entity's own new identity, such as:

```text
Product.Id
InventoryItem.Id
```

Identifiers that refer to an existing entity or message must still be supplied.

Examples:

```text
ProductId received by Inventory
Integration EventId received from Kafka
```

Direct Guid creation is also acceptable at the boundary where a genuinely new integration event is created:

```csharp
var integrationEvent = new ProductCreatedIntegrationEvent(
    Guid.NewGuid(),
    dateTimeProvider.UtcNow,
    product.Id);
```

Do not introduce an identity-generation service merely to wrap `Guid.NewGuid()`.

---

# 25. Date and time

Use UTC.

Prefer abstraction:

```text
IDateTimeProvider
```

Application code should prefer:

```csharp
dateTimeProvider.UtcNow
```

instead of repeated direct:

```csharp
DateTime.UtcNow
```

Use naming:

```text
CreatedAtUtc
UpdatedAtUtc
OccurredAtUtc
ReceivedAtUtc
ProcessedAtUtc
```

---

# 26. Kafka

Use Apache Kafka.

Use:

```text
Confluent.Kafka
```

Do NOT use:

- MassTransit
- RabbitMQ
- Azure Service Bus
- in-memory message buses

Kafka concepts must remain visible in the project.

The developer should be able to identify:

- Producer
- Consumer
- Topic
- Partition
- Key
- Offset
- Consumer Group
- Commit

---

# 27. Kafka configuration

Use strongly typed options.

Example:

```csharp
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public string ConsumerGroup { get; init; } = string.Empty;

    public string ProductCreatedTopic { get; init; } = string.Empty;
}
```

Configuration values:

```text
BootstrapServers = kafka:9092

ConsumerGroup = inventory-service

ProductCreatedTopic = catalog.product-created
```

Do not hardcode these values inside Kafka components.

---

# 28. Kafka topic

Use:

```text
catalog.product-created
```

Do not create unnecessary topics.

Do not add:

- Schema Registry
- Avro
- Protobuf
- retry topics
- DLQ

unless explicitly requested later.

Use JSON serialization.

---

# 29. Kafka key

Use:

```text
ProductId.ToString()
```

as the Kafka message key.

Do not manually select a partition.

Let Kafka select partition based on the key.

This keeps messages related to the same ProductId consistently partitioned.

---

# 30. Integration event

Create:

```text
ProductCreatedIntegrationEvent
```

Example:

```csharp
public sealed record ProductCreatedIntegrationEvent(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid ProductId);
```

Because it contains two or more parameters, it must always be vertically formatted.

EventId and ProductId have different meanings.

EventId:

identifies the Kafka message/event.

ProductId:

identifies the Product.

Use EventId for Inbox idempotency.

Use ProductId as Kafka key.

---

# 31. Outbox Pattern

Catalog must use transactional Outbox.

Required concepts:

```text
OutboxMessage
OutboxMessageConfiguration
IOutboxWriter
OutboxWriter
OutboxProcessor
IKafkaProducer
KafkaProducer
```

OutboxMessage contains approximately:

```text
Guid Id
string Type
string Content
DateTime OccurredAtUtc
DateTime? ProcessedAtUtc
string? Error
```

---

# 32. Outbox transaction

CreateProductCommandHandler must not publish Kafka directly.

Correct flow:

```text
Product.Create

↓

Create ProductCreatedIntegrationEvent

↓

Add Product

↓

Add OutboxMessage

↓

SaveChangesAsync
```

Product and OutboxMessage are persisted in one database transaction.

Incorrect:

```text
Save Product

↓

Publish Kafka directly from handler
```

---

# 33. OutboxProcessor

OutboxProcessor must inherit:

```text
BackgroundService
```

It should:

1. create DI scope
2. load a small batch of unprocessed messages
3. deserialize supported integration events
4. publish through IKafkaProducer
5. wait for delivery confirmation
6. mark ProcessedAtUtc
7. clear Error
8. SaveChanges

If Kafka publishing fails:

- log error
- optionally update Error
- keep ProcessedAtUtc null
- allow future retry

Do not crash the application because one Outbox message fails.

Do not use:

- Quartz
- Hangfire

---

# 34. Kafka producer

Create:

```text
IKafkaProducer
KafkaProducer
```

Use:

```text
ProducerBuilder<string, string>
```

Use System.Text.Json.

Example method:

```csharp
Task ProduceAsync<T>(
    string topic,
    string key,
    T message,
    CancellationToken cancellationToken);
```

Await Kafka delivery confirmation.

Only mark Outbox message processed after Kafka confirms successful delivery.

---

# 35. Kafka consumer

Inventory uses:

```text
ProductCreatedConsumer
```

Implement as:

```text
BackgroundService
```

Use:

```text
ConsumerBuilder<string, string>
```

Subscribe to:

```text
catalog.product-created
```

Use consumer group:

```text
inventory-service
```

Configure:

```text
EnableAutoCommit = false
```

---

# 36. Kafka processing order

Mandatory order:

```text
Consume Kafka message

↓

Deserialize integration event

↓

Check Inbox

↓

Execute business operation

↓

Save Inbox + Inventory transaction

↓

Commit Kafka offset
```

Never:

```text
Consume

↓

Commit

↓

Save DB
```

Kafka offset must only be committed after successful database persistence.

---

# 37. Inbox Pattern

Inventory must implement Inbox.

InboxMessage contains approximately:

```text
Guid Id
string Type
DateTime ReceivedAtUtc
DateTime? ProcessedAtUtc
```

Use:

```text
ProductCreatedIntegrationEvent.Id
```

as InboxMessage.Id.

Processing:

If Inbox message already exists:

- do not repeat business operation
- log duplicate
- commit Kafka offset
- continue

If message is new:

- create InboxMessage
- create InventoryItem
- save both transactionally
- commit Kafka offset after successful DB commit

---

# 38. Duplicate protection

Use both:

InboxMessage.Id primary key

and:

unique index on InventoryItem.ProductId

Inbox is the main idempotency mechanism.

Database uniqueness is additional protection.

---

# 39. Kafka delivery semantics

Treat the consumer flow as:

```text
at-least-once
```

Do not claim exactly-once business processing.

Do not implement Kafka transactions initially.

Do not implement distributed transactions.

Inbox exists because duplicate delivery is possible.

---

# 40. Serialization

Use:

```text
System.Text.Json
```

Do not add Newtonsoft.Json unless specifically required.

Do not serialize Domain objects.

Serialize integration contracts only.

---

# 41. Logging architecture

Logging must be centralized where possible.

Do not inject:

```text
ILogger<THandler>
```

into every MediatR handler only to log routine handler execution.

Application handlers should focus on orchestration and business use cases.

---

# 42. Automatic MediatR logging

Create:

```text
RequestLoggingBehavior<TRequest, TResponse>
```

Register it globally.

It should automatically log:

- request type
- request started
- request completed
- elapsed time
- unhandled error

Example intent:

```text
Handling CreateProductCommand

Handled CreateProductCommand in 18 ms
```

Do not serialize complete request objects automatically.

Do not automatically log every request property.

Future requests may contain sensitive information.

All Commands and Queries must receive this behavior automatically.

---

# 43. Handler logging

Do not manually write routine logs like:

```csharp
logger.LogInformation(
    "Created product {ProductId}",
    product.Id);
```

inside Application handlers if the information is only confirming successful handler execution.

Remove ILogger dependencies that exist only for this purpose.

Handlers should usually not require ILogger.

---

# 44. HTTP logging

Use global Serilog request logging.

Use:

```csharp
app.UseSerilogRequestLogging();
```

Do not manually log every HTTP request inside individual endpoints.

---

# 45. Infrastructure logging

Explicit logging is allowed and expected in Infrastructure where useful.

Examples:

- Kafka consumer startup
- Kafka consumer shutdown
- Kafka consume error
- Kafka topic
- Kafka partition
- Kafka offset
- Kafka key
- Kafka producer failure
- Outbox publishing failure
- duplicate Inbox event
- unexpected BackgroundService exception

Do not remove useful infrastructure diagnostics.

The rule is:

```text
HTTP
→ centralized Serilog request logging

MediatR
→ centralized RequestLoggingBehavior

Kafka / Outbox / Inbox / BackgroundServices
→ explicit technical structured logging
```

---

# 46. Structured logging

Always use structured logging.

Correct:

```csharp
logger.LogInformation(
    "Consumed message {EventId} from partition {Partition}",
    integrationEvent.Id,
    consumeResult.Partition.Value);
```

Incorrect:

```csharp
logger.LogInformation(
    $"Consumed message {integrationEvent.Id}");
```

Do not use string interpolation for structured log values.

---

# 47. OpenTelemetry

Use OpenTelemetry metrics.

Instrument:

- ASP.NET Core
- runtime metrics

Expose Prometheus-compatible metrics.

Prometheus should scrape:

```text
catalog-api
inventory-api
```

---

# 48. Grafana

Grafana must use Prometheus as datasource.

Provision datasource automatically.

Provide one small dashboard.

Useful metrics:

- HTTP request rate
- HTTP request duration
- runtime/process metrics

Optional custom counters:

```text
catalog_products_created_total
catalog_outbox_published_total
inventory_events_consumed_total
inventory_duplicate_events_total
```

Do not over-engineer observability.

---

# 49. Observability scope

Use:

```text
Serilog
OpenTelemetry Metrics
Prometheus
Grafana
```

Do not add initially:

- Loki
- Tempo
- Jaeger
- Elasticsearch

---

# 50. Health checks

Both APIs expose:

```text
/health
```

Health checks should verify PostgreSQL.

Kafka health checks are optional if they can be implemented simply.

Do not create a complicated custom Kafka health subsystem.

---

# 51. Docker Compose

docker-compose.yml contains:

```text
catalog-api
inventory-api
catalog-db
inventory-db
kafka
prometheus
grafana
```

Use Kafka in KRaft mode where practical.

Do not add ZooKeeper unless the selected image requires it.

Use one Docker network.

Use named volumes where appropriate.

---

# 52. Docker networking

Inside Docker use service DNS names.

Catalog database:

```text
catalog-db
```

Inventory database:

```text
inventory-db
```

Kafka:

```text
kafka:9092
```

Do not use:

```text
localhost
```

for communication between containers.

---

# 53. Dockerfiles

Create one multi-stage Dockerfile for:

```text
Catalog.Api
Inventory.Api
```

Use .NET 10 images.

Do not copy unnecessary development files into runtime image.

---

# 54. Dependency injection

Keep Program.cs small.

Prefer:

```csharp
builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);
```

Use extension methods for:

- Application registration
- Infrastructure registration
- Endpoints
- OpenTelemetry
- Serilog where appropriate

Do not put large registration blocks directly into Program.cs.

---

# 55. Automatic registration

Prefer automatic registration where a clear convention exists.

Use assembly scanning for:

- repositories
- validators
- MediatR handlers
- endpoints

Do not manually register every new handler, validator, repository, or endpoint.

---

# 56. Explicit registration

Use explicit registration when convention scanning would make behavior unclear.

Examples:

```text
DbContext
Kafka Producer
Hosted Services
OutboxProcessor
IDateTimeProvider
Options
```

Do not create a magic assembly scanner that registers everything.

---

# 57. Program.cs

Program.cs should mainly:

1. create builder
2. configure Serilog
3. add Application
4. add Infrastructure
5. add endpoints
6. add ProblemDetails
7. add health checks
8. add OpenTelemetry
9. build app
10. configure global exception handling
11. configure Serilog request logging
12. map endpoints
13. map health
14. map metrics
15. run

Business logic must not exist in Program.cs.

---

# 58. Global error handling

Use:

```text
ProblemDetails
```

and centralized exception handling.

Typical mappings:

```text
Validation → 400

NotFound → 404

Conflict → 409

Unexpected failure → 500
```

Do not add try/catch to every endpoint.

---

# 59. Local variable style

Prefer:

```csharp
var
```

for local variables.

Correct:

```csharp
var productResult = Product.Create(
    command.Name,
    command.Price,
    dateTimeProvider.UtcNow);
```

Correct:

```csharp
var product = productResult.Value;
```

Incorrect:

```csharp
Result<Product> productResult = Product.Create(...);
```

Incorrect:

```csharp
Product product = productResult.Value;
```

Use an explicit type only when it materially improves readability.

Do not repeat obvious type information.

---

# 60. Mandatory vertical formatting

This rule is mandatory.

If a declaration or invocation contains TWO OR MORE parameters or arguments, format them vertically.

One parameter or argument per line.

This applies even if everything fits on one line.

---

## Correct record

```csharp
public sealed record CreateProductCommand(
    string Name,
    decimal Price)
    : ICommand<Guid>;
```

---

## Incorrect record

```csharp
public sealed record CreateProductCommand(string Name, decimal Price)
    : ICommand<Guid>;
```

---

## Correct method

```csharp
public async Task<Result<Guid>> Handle(
    CreateProductCommand command,
    CancellationToken cancellationToken)
```

---

## Incorrect method

```csharp
public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
```

---

## Correct invocation

```csharp
var productResult = Product.Create(
    productId,
    command.Name,
    command.Price,
    dateTimeProvider.UtcNow);
```

---

## Exactly two arguments must also be vertical

Correct:

```csharp
var product = Product.Create(
    name,
    price);
```

Incorrect:

```csharp
var product = Product.Create(name, price);
```

---

## One parameter may remain inline

Correct:

```csharp
productRepository.Add(product);
```

Correct:

```csharp
await unitOfWork.SaveChangesAsync(cancellationToken);
```

---

# 61. Vertical formatting applies to

Apply the 2+ rule consistently to:

- constructors
- primary constructors
- record constructors
- methods
- local functions
- delegates
- lambdas where practical
- method calls
- constructor calls
- logger calls
- Kafka calls
- EF configuration calls where applicable

This project-specific rule overrides normal formatter preferences.

---

# 62. Logical whitespace

Use blank lines between logical steps.

Do not write entire method bodies as one dense block.

Correct:

```csharp
var productResult = Product.Create(
    command.Name,
    command.Price,
    dateTimeProvider.UtcNow);

if (productResult.IsFailure)
{
    return productResult.Error;
}

var product = productResult.Value;

var integrationEvent = new ProductCreatedIntegrationEvent(
    Guid.NewGuid(),
    dateTimeProvider.UtcNow,
    product.Id);

productRepository.Add(product);
outboxWriter.Add(integrationEvent);

await unitOfWork.SaveChangesAsync(cancellationToken);

return product.Id;
```

Use one empty line between logical blocks.

Do not add multiple unnecessary blank lines.

---

# 63. Group related operations

Related operations may remain next to each other.

Correct:

```csharp
productRepository.Add(product);
outboxWriter.Add(integrationEvent);

await unitOfWork.SaveChangesAsync(cancellationToken);
```

Do not blindly insert an empty line after every statement.

Whitespace must show logical grouping.

---

# 64. Preferred handler style

Prefer handlers like:

```csharp
internal sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IOutboxWriter outboxWriter,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var productResult = Product.Create(
            command.Name,
            command.Price,
            dateTimeProvider.UtcNow);

        if (productResult.IsFailure)
        {
            return productResult.Error;
        }

        var product = productResult.Value;

        var integrationEvent = new ProductCreatedIntegrationEvent(
            Guid.NewGuid(),
            dateTimeProvider.UtcNow,
            product.Id);

        productRepository.Add(product);
        outboxWriter.Add(integrationEvent);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
```

Notice:

- `var` is used
- domain entities create their own identifiers
- direct Guid creation is limited to the new integration event boundary
- no unnecessary ILogger
- 2+ parameters are vertical
- logical blocks are separated
- handler only orchestrates use case
- logging is centralized

---

# 65. Naming rules

Use meaningful full names.

Avoid:

```text
repo
ctx
msg
evt
ct
cfg
svc
mgr
db
```

Prefer:

```text
productRepository
inventoryRepository
dbContext
message
integrationEvent
cancellationToken
configuration
service
manager
database
```

Do not shorten names just to save characters.

---

# 66. C# style

Use modern C# supported by .NET 10.

Use:

- file-scoped namespaces
- nullable reference types
- implicit usings
- primary constructors where readable
- records where appropriate
- sealed classes when inheritance is not intended
- internal visibility by default where possible

Do not use:

```text
#region
```

Do not add unnecessary XML comments.

Comments should explain WHY.

Do not write comments that only restate code.

---

# 67. Async rules

Use asynchronous APIs.

Examples:

```text
FirstOrDefaultAsync
SingleOrDefaultAsync
ToListAsync
SaveChangesAsync
ProduceAsync
```

Pass CancellationToken whenever available.

Use full name:

```text
cancellationToken
```

Do not use:

```text
ct
```

Do not use:

```text
.Result
.Wait()
```

Do not wrap naturally asynchronous I/O in Task.Run.

---

# 68. Mapping

Use manual mapping.

Do not add AutoMapper.

Do not return EF Core entities directly from API endpoints.

Return response records.

---

# 69. Packages

Use:

```text
Directory.Packages.props
```

for central package management.

Use current stable package versions compatible with .NET 10.

Main packages may include:

```text
MediatR
FluentValidation.DependencyInjectionExtensions
Microsoft.EntityFrameworkCore
Npgsql.EntityFrameworkCore.PostgreSQL
EFCore.NamingConventions
Confluent.Kafka
Serilog.AspNetCore
Serilog.Sinks.Console
OpenTelemetry.Extensions.Hosting
OpenTelemetry.Instrumentation.AspNetCore
OpenTelemetry.Instrumentation.Runtime
OpenTelemetry.Exporter.Prometheus.AspNetCore
```

Do not blindly copy old Evently package versions.

Do not use preview packages unless required.

---

# 70. Do not add

Do not add unless explicitly requested:

- RabbitMQ
- MassTransit
- Redis
- authentication
- authorization
- users
- orders
- payments
- notifications
- API Gateway
- Saga
- event sourcing
- Kubernetes
- service mesh
- gRPC
- GraphQL
- frontend
- AutoMapper
- generic repository
- Quartz
- Hangfire
- Schema Registry
- Avro
- Protobuf
- retry topics
- DLQ
- distributed transactions

---

# 71. Scope control

Before adding any technology or abstraction, ask:

```text
Does this help the current use case or help demonstrate:

Catalog
→ Outbox
→ Kafka
→ Inbox
→ Inventory?
```

If not, do not add it.

Prefer simple and understandable implementation.

Do not build abstractions for hypothetical future requirements.

---

# 72. New endpoint development

When adding a new Command endpoint, follow an existing Command feature as template.

Typical flow:

```text
Endpoint
↓
Command
↓
Validator
↓
Handler
↓
Domain
↓
Repository
↓
UnitOfWork
```

When adding a new Query endpoint:

```text
Endpoint
↓
Query
↓
QueryHandler
↓
Repository / read abstraction
↓
Response
```

Do not involve Kafka in every CRUD operation.

Kafka should only be used when there is a real cross-service integration use case.

---

# 73. README

README should explain:

- project goal
- Clean Architecture
- DDD
- CQRS
- MediatR
- Catalog
- Inventory
- database-per-service
- Outbox
- Kafka Producer
- Topic
- Kafka Key
- Partition
- Consumer
- Consumer Group
- Offset
- Offset Commit
- Inbox
- idempotency
- Docker
- Prometheus
- Grafana

Include architecture diagram.

Include HTTP examples.

Include Docker commands.

Include Kafka CLI commands supported by the selected Docker image.

---

# 74. Kafka README commands

README should explain how to:

- list topics
- describe topic
- consume topic manually
- inspect consumer groups
- inspect committed offsets

Commands should match the actual Kafka Docker image used.

---

# 75. Formatting verification

`dotnet format` is not enough to validate this project's custom style.

Roslyn may keep or restore horizontal argument lists.

After running dotnet format, inspect custom rules manually.

Especially verify:

```text
2+ method parameters
2+ constructor parameters
2+ record parameters
2+ invocation arguments
```

They must remain vertical.

---

# 76. Build verification

After meaningful code changes run:

```text
dotnet restore MicroShop.sln
```

Then:

```text
dotnet build MicroShop.sln --no-restore
```

Fix all build errors.

Run:

```text
dotnet format MicroShop.sln
```

Then inspect custom formatting rules.

Then:

```text
dotnet format MicroShop.sln --verify-no-changes
```

---

# 77. Docker verification

Run:

```text
docker compose config
```

If Docker is available:

```text
docker compose up -d --build
```

Then inspect:

```text
docker compose ps
```

and relevant logs.

---

# 78. End-to-end verification

When Docker is available verify:

POST Product

↓

Product saved in Catalog

↓

OutboxMessage saved

↓

OutboxProcessor publishes

↓

Kafka message created

↓

Inventory consumer receives message

↓

InboxMessage saved

↓

InventoryItem created with Quantity = 0

↓

Kafka offset committed

Then verify:

```text
GET /api/products/{id}

GET /api/inventory/{productId}

PUT /api/inventory/{productId}
```

---

# 79. Duplicate verification

When possible, redeliver the same integration event using the same EventId.

Expected:

- no duplicate InventoryItem
- no duplicate business operation
- Inbox detects duplicate
- Kafka offset is safely committed

---

# 80. Definition of done

A change is complete only when:

1. architecture rules are preserved
2. project builds
3. new code follows formatting rules
4. 2+ parameters are vertical
5. local variables prefer var
6. logical blocks contain readable spacing
7. handlers do not contain unnecessary ILogger
8. new domain entities create their own IDs in domain factories
9. repositories follow automatic DI convention
10. commands/queries use MediatR
11. validation uses FluentValidation pipeline
12. no forbidden dependencies were introduced
13. Kafka topic and consumer semantics are preserved
14. Outbox semantics are preserved
15. Inbox semantics are preserved
16. DB commit occurs before Kafka offset commit
17. docker compose configuration remains valid
18. no core functionality contains TODO placeholders

Do not report a task as complete while the solution does not compile.

---

# 81. Final development principle

Prefer:

```text
simple
explicit
consistent
readable
testable
educational
```

over:

```text
clever
generic
highly abstract
future-proof
enterprise-heavy
```

The project is intentionally small.

Every new abstraction must justify its existence.

Every new endpoint should be understandable by following an existing endpoint template.

Every new developer should be able to trace:

```text
HTTP request
→ Endpoint
→ MediatR
→ Handler
→ Domain
→ Persistence
```

and for asynchronous communication:

```text
Domain/Application change
→ Outbox
→ Kafka
→ Consumer
→ Inbox
→ Domain/Persistence
```

without unnecessary indirection.
