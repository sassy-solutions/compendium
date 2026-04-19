# Compendium.Core

The core domain primitives and patterns for the Compendium Framework. This library provides the fundamental building blocks for implementing Domain-Driven Design (DDD) patterns in .NET applications.

## Features

### Domain Primitives

- **Entity<TId>**: Base class for all domain entities with identity, timestamps, and business rule validation
- **AggregateRoot<TId>**: Base class for aggregate roots with domain event management and optimistic concurrency
- **ValueObject**: Base class for value objects with structural equality

### Domain Events

- **IDomainEvent**: Interface for domain events within bounded contexts
- **DomainEventBase**: Base implementation for domain events
- **IIntegrationEvent**: Interface for integration events across bounded contexts

### Business Rules

- **IBusinessRule**: Interface for encapsulating business rules and invariants
- **BusinessRuleValidationException**: Exception thrown when business rules are violated

### Specifications

- **ISpecification<T>**: Interface for the Specification pattern with query composition
- **Specification<T>**: Base implementation with logical operators (And, Or, Not)

### Result Pattern

- **Result**: Represents operation results without exceptions
- **Result<T>**: Generic result with value
- **Error**: Structured error information with types and metadata
- **ResultExtensions**: Functional programming extensions (Map, Bind, Match)

## Usage Examples

### Entity with Business Rules

```csharp
public class User : Entity<UserId>
{
    public User(UserId id, string email, string name) : base(id)
    {
        CheckRule(new ValidEmailRule(email));
        CheckRule(new NonEmptyNameRule(name));
        
        Email = email;
        Name = name;
    }
    
    public string Email { get; private set; }
    public string Name { get; private set; }
}
```

### Aggregate Root with Domain Events

```csharp
public class Order : AggregateRoot<OrderId>
{
    public void PlaceOrder()
    {
        CheckRule(new OrderMustHaveItemsRule(Items));
        
        Status = OrderStatus.Placed;
        AddDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount));
        IncrementVersion();
    }
}
```

### Value Object

```csharp
public class Money : ValueObject
{
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
    
    public decimal Amount { get; }
    public string Currency { get; }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### Result Pattern

```csharp
public Result<User> CreateUser(string email, string name)
{
    if (string.IsNullOrWhiteSpace(email))
        return Error.Validation("User.Email.Empty", "Email cannot be empty");
        
    if (string.IsNullOrWhiteSpace(name))
        return Error.Validation("User.Name.Empty", "Name cannot be empty");
        
    var user = new User(UserId.New(), email, name);
    return Result.Success(user);
}

// Usage with functional composition
var result = CreateUser(email, name)
    .Map(user => new UserDto(user.Id, user.Email, user.Name))
    .Tap(dto => logger.LogInformation("User created: {UserId}", dto.Id))
    .Match(
        onSuccess: dto => Ok(dto),
        onFailure: error => BadRequest(error.Message)
    );
```

### Specifications

```csharp
public class ActiveUsersSpecification : Specification<User>
{
    public ActiveUsersSpecification() : base(user => user.IsActive)
    {
        ApplyOrderBy(user => user.Name);
    }
}

// Composition
var spec = new ActiveUsersSpecification()
    .And(new UsersByRoleSpecification("Admin"))
    .Or(new UsersByRoleSpecification("Manager"));
```

## Design Principles

1. **No External Dependencies**: Core library only uses .NET BCL
2. **Immutability by Default**: Value objects and events are immutable
3. **Thread Safety**: All implementations are thread-safe
4. **Functional Patterns**: Result pattern eliminates exceptions for control flow
5. **Rich Domain Models**: Entities encapsulate behavior and enforce invariants
6. **Event-Driven Architecture**: Support for domain and integration events

## License

Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.