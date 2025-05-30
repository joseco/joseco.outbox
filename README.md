# Joseco.Outbox

Joseco.Outbox is a library for implementing the Outbox pattern in .NET applications.

The Outbox pattern is a design pattern that helps ensure that messages are sent reliably and consistently in distributed systems. It involves storing messages in a local database (the outbox) before sending them to a message broker or other external system. This way, if the application crashes or fails to send the message, the message can be retried later.

This project is supported using Mediatr as a dependency. The main idea of this implementation, is to use the outbox table to store messages that need to be processed later, and then use a background service to periodically check the outbox table for new messages and send them to a INotificationHandler to process them. The outbox table is also used to store the status of the messages, so that we can track which messages have been sent and which ones are still pending.

Initially, this project was created to be used with Entity Framework Core.

### Installing Joseco.Outbox.EFCore

You should install [Joseco.Outbox.EFCore with NuGet](https://www.nuget.org/packages/Joseco.Outbox.EFCore):

    Install-Package Joseco.Outbox.EFCore
    
Or via the .NET Core command line interface:

    dotnet add package Joseco.Outbox.EFCore

Either commands, from Package Manager Console or .NET Core CLI, will download and install Joseco.Outbox.EFCore and all required dependencies.

### Using Contracts-Only Package

To reference only the contracts for Joseco.Outbox, which includes:

- `OutboxMessage` 
- `IOutboxService` 

This package is useful in scenarios where you need to store OutboxMessage objects in separate assembly/project from the persistence implementation. This allows you to share the contracts across multiple projects without needing to reference the entire Joseco.Outbox.EFCore package.

The `OutboxMessage<T>` object is the main object that will be used to store the messages that need to be sent. The `OutboxMessage` object has the following properties:

- Id: The unique identifier of the message.
- Type: The type of the message.
- Content: The content of the message. This is a Generic Type, so you can store any type of object in the message. This field will be stored as a JSON string in the database.
- Created: The date and time when the message was created. This field is used to track the creation date of the message.
- Processed: A boolean value that indicates whether the message has been processed or not. This field is used to track the status of the message.
- ProcessedAt: The date and time when the message was processed. This field is used to track the processing date of the message.
- CorrelationId: The correlation id of the message. This field is used to track the correlation id of the message. This field is optional and can be null.
- TraceId: The trace id of the message. This field is used to track the trace id of the message. This field is optional and can be null.
- SpanId: The span id of the message. This field is used to track the span id of the message. This field is optional and can be null.

The `IOutboxService<T>` interface is used to send messages to the outbox. The `IOutboxService` interface has the following methods:

- `AddAsync(OutboxMessage<T> message)`: This method is used to add a message to the outbox.

### How to configure Joseco.Outbox.EFCore in your project

To use Joseco.Outbox.EFCore in your project, you need to add a DbSet for the `OutboxMessage<T>` object in your DbContext class and add the configuration on the `OnModelCreating` method calling `modelBuilder.AddOutboxModel<DomainEvent>()`.
This will allow Entity Framework Core to create the outbox table in the database. 
In this example, we are using `DomainEvent` class as the generic type for the `OutboxMessage<T>` object. You can create your own Type and used in the Generic Type of `OutboxMessage<T>`.
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<OutboxMessage<DomainEvent>> OutboxMessages { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ...
        modelBuilder.AddOutboxModel<DomainEvent>(); // This will add the OutboxMessage table to the database
        base.OnModelCreating(modelBuilder);
        ....
    }
    
}
```
Then, you need to create a class that implements the `IOutboxDatabase<E>` interface. We recomend the following implementation:
```csharp
public class OutboxDatabase : IOutboxDatabase<DomainEvent>
{
    private readonly ApplicationDbContext _dbContext;

    public OutboxDatabase(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public DbSet<OutboxMessage<DomainEvent>> GetOutboxMessages()
    {
        return _dbContext.OutboxMessages;
    }
}
```
Or, if you are using the Unit Of Work Pattern:
```csharp
public class OutboxDatabase : IOutboxDatabase<DomainEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public OutboxDatabase(ApplicationDbContext dbContext, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    public DbSet<OutboxMessage<DomainEvent>> GetOutboxMessages()
    {
        return _dbContext.OutboxMessages;
    }
}
```
After that, you need to register the `IOutboxDatabase<E>` and `IOutboxService<E>` in the DI container. 
```csharp
  
    ...
    builder.Services
            .AddScoped<IOutboxDatabase<DomainEvent>, OutboxDatabase>() // or UnitOfWork
            .AddOutbox<DomainEvent>();
    ...

```

### Using the Outbox Service

To use the Outbox Service, you need to inject the `IOutboxService<DomainEvent>` in your class and use the `AddAsync` method to add a message to the outbox. 
```csharp
public class MyService
{
    private readonly IOutboxService<DomainEvent> _outboxService;
    public MyService(IOutboxService<DomainEvent> outboxService)
    {
        _outboxService = outboxService;
    }
    public async Task DoSomethingAsync(...)
    {
        ...
        DomainEvent domainEvent = // Get the domain event from somewhere
        ...
        var message = new OutboxMessage<DomainEvent>(domainEvent);
        await _outboxService.AddAsync(message);
    }
}
```

### Using the Outbox Background Service

To use the Outbox Background Service, you need to create a class that implements the `INotificationHandler<OutboxMessage<DomainEvent>>` interface. This class will be used to process the messages in the outbox. 
```csharp
public class OutboxMessageHandler : INotificationHandler<OutboxMessage<DomainEvent>>
{
    public Task Handle(OutboxMessage<DomainEvent> notification, CancellationToken cancellationToken)
    {
        // Process the message here
        return Task.CompletedTask;
    }
}
```
Ensure to register the `OutboxMessageHandler` in the DI container using the `AddMediatR` method:
```csharp
builder.Services
    .AddMediatR(typeof(OutboxMessageHandler).Assembly)
```
Then, you need to register the `OutboxBackgroundService` in the DI container. 
```csharp
builder.Services
    .AddOutboxBackgroundService<DomainEvent>();
```
Also you can define the interval of the background service passing the value in milliseconds on the `AddOutboxBackgroundService<T>` method. The default interval is 5 seconds (5000 milliseconds).
```csharp
builder.Services
    .AddOutboxBackgroundService<DomainEvent>(5000);
```