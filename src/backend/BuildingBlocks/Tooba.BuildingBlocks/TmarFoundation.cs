using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentValidation;
using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.BuildingBlocks;

/// <summary>
/// ساعت متعارف UTC برای لایهٔ Application/Infrastructure. Domain اینترفیس را inject نمی‌کند.
/// </summary>
public interface IClock
{
    /// <summary>زمان فعلی به‌صورت <see cref="DateTimeOffset"/> با آفست صفر (UTC).</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>پیاده‌سازی سیستم بر پایهٔ <see cref="DateTimeOffset.UtcNow"/>.</summary>
public sealed class SystemUtcClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>ساعت ثابت برای تست.</summary>
public sealed class FixedClock : IClock
{
    /// <summary>ساعت را روی مقدار ثابت می‌سازد.</summary>
    public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    /// <inheritdoc />
    public DateTimeOffset UtcNow { get; }
}

/// <summary>تولید شناسه در مرز orchestration؛ Domain شناسه را صریحاً دریافت می‌کند.</summary>
public interface IIdGenerator
{
    /// <summary>یک شناسهٔ جدید (UUIDv7 در پیاده‌سازی پیش‌فرض) تولید می‌کند.</summary>
    Guid NewId();
}

/// <summary>پیاده‌سازی UUIDv7 از طریق <see cref="UuidV7"/>.</summary>
public sealed class UuidV7IdGenerator : IIdGenerator
{
    /// <inheritdoc />
    public Guid NewId() => UuidV7.New();
}

/// <summary>مولد قطعی برای تست.</summary>
public sealed class FixedIdGenerator : IIdGenerator
{
    private readonly Queue<Guid> _ids;

    /// <summary>مولد را با صف شناسه‌های ازپیش‌تعیین‌شده می‌سازد.</summary>
    public FixedIdGenerator(params Guid[] ids) => _ids = new Queue<Guid>(ids);

    /// <inheritdoc />
    public Guid NewId() => _ids.Count > 0 ? _ids.Dequeue() : throw new InvalidOperationException("fixed_id_exhausted");
}

/// <summary>خطای معنایی پایدار بدون متن محلی‌سازی‌شدهٔ کاربر.</summary>
public sealed class SemanticError
{
    /// <summary>کد پایدار مثل catalog.category.invalid_slug.</summary>
    public string Code { get; }

    /// <summary>آرگومان‌های ساخت‌یافتهٔ اختیاری.</summary>
    public IReadOnlyDictionary<string, string?> Arguments { get; }

    /// <summary>خطا را با کد و آرگومان می‌سازد.</summary>
    public SemanticError(string code, IReadOnlyDictionary<string, string?>? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("error_code_required", nameof(code));
        }

        Code = code.Trim();
        Arguments = arguments ?? new Dictionary<string, string?>();
    }
}

/// <summary>استثنای Application/Domain بر پایهٔ <see cref="SemanticError"/>.</summary>
public sealed class SemanticException : Exception
{
    /// <summary>خطای معنایی.</summary>
    public SemanticError Error { get; }

    /// <summary>استثنا را روی کد معنایی می‌سازد (پیام فنی = کد، نه متن محلی).</summary>
    public SemanticException(SemanticError error)
        : base(error.Code)
    {
        Error = error;
    }
}

/// <summary>رفتار اعتبارسنجی FluentValidation قبل از Handler.</summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>رفتار را با اعتبارسنج‌های ثبت‌شده می‌سازد.</summary>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}

/// <summary>
/// لاگ سبک lifecycle MediatR. خطاهای معنایی/اعتبارسنجی را Error تکراری نمی‌کند؛
/// شکست‌های غیرمنتظره یک‌بار در مرز exception pipeline لاگ می‌شوند.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>رفتار لاگ را می‌سازد.</summary>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        _logger.LogDebug("MediatR handling {Request}", name);
        try
        {
            var response = await next();
            if (response is IResultStatus { IsFailure: true })
            {
                // Business Result failure: Debug only — never Error (presentation owns client response).
                _logger.LogDebug("MediatR business Result failure {Request}", name);
            }
            else
            {
                _logger.LogDebug("MediatR handled {Request}", name);
            }

            return response;
        }
        catch (Exception ex) when (IsExpectedBusinessFailure(ex))
        {
            _logger.LogDebug(ex, "MediatR expected failure {Request} {ErrorType}", name, ex.GetType().Name);
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected: log once here at Debug; Host exception pipeline owns Warning/Error presentation.
            _logger.LogDebug(ex, "MediatR unexpected failure {Request} {ErrorType}", name, ex.GetType().Name);
            throw;
        }
    }

    private static bool IsExpectedBusinessFailure(Exception ex) =>
        ex is SemanticException
        || ex is FluentValidation.ValidationException
        || ex is PlatformHttpException;
}

/// <summary>فرمان اثبات foundation (فقط برای تست ثبت pipeline).</summary>
public sealed record FoundationPingCommand(string Name) : IRequest<string>;

/// <summary>اعتبارسنج فرمان اثبات.</summary>
public sealed class FoundationPingCommandValidator : AbstractValidator<FoundationPingCommand>
{
    /// <summary>قواعد حداقلی نام.</summary>
    public FoundationPingCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("foundation.ping.name_required");
    }
}

/// <summary>Handler اثبات foundation.</summary>
public sealed class FoundationPingCommandHandler : IRequestHandler<FoundationPingCommand, string>
{
    /// <inheritdoc />
    public Task<string> Handle(FoundationPingCommand request, CancellationToken cancellationToken)
        => Task.FromResult($"pong:{request.Name}");
}

/// <summary>ثبت CQRS foundation مشترک.</summary>
public static class ToobaCqrsRegistration
{
    /// <summary>
    /// MediatR 12.5.0، FluentValidation، IClock، IIdGenerator و pipelineهای validation/logging را ثبت می‌کند.
    /// </summary>
    /// <param name="services">DI.</param>
    /// <param name="additionalHandlerAssemblies">اسمبلی Handler ماژول‌ها.</param>
    public static IServiceCollection AddToobaCqrsFoundation(
        this IServiceCollection services,
        params System.Reflection.Assembly[] additionalHandlerAssemblies)
    {
        services.AddSingleton<IClock, SystemUtcClock>();
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();
        services.AddValidatorsFromAssembly(typeof(FoundationPingCommand).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(FoundationPingCommand).Assembly);
            foreach (var assembly in additionalHandlerAssemblies)
            {
                cfg.RegisterServicesFromAssembly(assembly);
            }
        });
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Observability.Tracing.TracingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
