namespace BengalTex.ERP.Domain.Exceptions;

public class DomainException : Exception
{
    public string? Code { get; }
    public DomainException(string message, string? code = null) : base(message) => Code = code;
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.", "NOT_FOUND") { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message, "CONFLICT") { }
}

public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message, string? code = null) : base(message, code ?? "BUSINESS_RULE") { }
}