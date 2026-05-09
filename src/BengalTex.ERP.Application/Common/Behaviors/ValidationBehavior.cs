using BengalTex.ERP.Domain.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        // If TResponse is Result or Result<T>, return validation failure; otherwise throw
        var errors = failures
            .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage, f.ErrorCode))
            .ToList();

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.ValidationFailure(errors);

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(Result<>).MakeGenericType(typeof(TResponse).GetGenericArguments()[0]);
            var method = resultType.GetMethod(nameof(Result.ValidationFailure));
            return (TResponse)method!.Invoke(null, new object[] { errors })!;
        }

        throw new ValidationException(failures);
    }
}