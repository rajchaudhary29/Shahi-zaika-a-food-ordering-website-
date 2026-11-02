namespace ShahiZayka.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class ProductNotFoundException : DomainException
{
    public ProductNotFoundException(int productId) 
        : base($"Product with ID '{productId}' was not found.") { }
}

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string productName, int requested, int available) 
        : base($"Insufficient stock for '{productName}'. Requested: {requested}, Available: {available}.") { }
}

public class InvalidOrderStateException : DomainException
{
    public InvalidOrderStateException(string message) : base(message) { }
}

public class DuplicateOrderException : DomainException
{
    public DuplicateOrderException(string idempotencyKey) 
        : base($"An order with Idempotency Key '{idempotencyKey}' has already been processed.") { }
}
