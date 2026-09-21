using System.Collections.ObjectModel;

namespace Tooba.BuildingBlocks.Results;

/// <summary>
/// Shared status surface for MediatR tracing/logging without unsafe reflection.
/// </summary>
public interface IResultStatus
{
    /// <summary>True when the operation succeeded.</summary>
    bool IsSuccess { get; }

    /// <summary>True when the operation failed with one or more <see cref="SemanticError"/> values.</summary>
    bool IsFailure { get; }

    /// <summary>Business errors; empty on success.</summary>
    IReadOnlyList<SemanticError> Errors { get; }
}

/// <summary>
/// Canonical non-generic Result for expected business outcomes (no value payload).
/// Invariant: success has zero errors; failure has at least one <see cref="SemanticError"/>.
/// </summary>
public class Result : IResultStatus
{
    private static readonly IReadOnlyList<SemanticError> EmptyErrors =
        new ReadOnlyCollection<SemanticError>(Array.Empty<SemanticError>());

    private readonly IReadOnlyList<SemanticError> _errors;

    private Result(bool isSuccess, IReadOnlyList<SemanticError> errors)
    {
        IsSuccess = isSuccess;
        _errors = errors;
    }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    /// <inheritdoc />
    public IReadOnlyList<SemanticError> Errors => _errors;

    /// <summary>Primary (first) business error. Throws when success.</summary>
    public SemanticError FirstError =>
        IsFailure
            ? _errors[0]
            : throw new InvalidOperationException("result_first_error_on_success");

    /// <summary>Successful void outcome.</summary>
    public static Result Success() => new(true, EmptyErrors);

    /// <summary>Failure from a single semantic error.</summary>
    public static Result Failure(SemanticError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, new ReadOnlyCollection<SemanticError>([error]));
    }

    /// <summary>Failure from one or more semantic errors (primary = first).</summary>
    public static Result Failure(IEnumerable<SemanticError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var list = errors as IList<SemanticError> ?? errors.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("result_failure_requires_errors", nameof(errors));
        }

        foreach (var error in list)
        {
            ArgumentNullException.ThrowIfNull(error);
        }

        return new Result(false, new ReadOnlyCollection<SemanticError>(list.ToArray()));
    }

    /// <summary>Failure from one or more semantic errors.</summary>
    public static Result Failure(params SemanticError[] errors) => Failure((IEnumerable<SemanticError>)errors);

    /// <summary>Typed success factory.</summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>Typed failure factory.</summary>
    public static Result<T> Failure<T>(SemanticError error) => Result<T>.Failure(error);

    /// <summary>Typed failure factory.</summary>
    public static Result<T> Failure<T>(IEnumerable<SemanticError> errors) => Result<T>.Failure(errors);

    /// <summary>Match success / failure branches.</summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<IReadOnlyList<SemanticError>, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(_errors);
    }
}

/// <summary>
/// Canonical typed Result. Invariant: success has a value and zero errors; failure has errors and no accessible value.
/// </summary>
public sealed class Result<T> : IResultStatus
{
    private static readonly IReadOnlyList<SemanticError> EmptyErrors =
        new ReadOnlyCollection<SemanticError>(Array.Empty<SemanticError>());

    private readonly T? _value;
    private readonly IReadOnlyList<SemanticError> _errors;

    private Result(bool isSuccess, T? value, IReadOnlyList<SemanticError> errors)
    {
        IsSuccess = isSuccess;
        _value = value;
        _errors = errors;
    }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    /// <inheritdoc />
    public IReadOnlyList<SemanticError> Errors => _errors;

    /// <summary>Primary (first) business error. Throws when success.</summary>
    public SemanticError FirstError =>
        IsFailure
            ? _errors[0]
            : throw new InvalidOperationException("result_first_error_on_success");

    /// <summary>Success value. Throws when failure.</summary>
    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("result_value_on_failure");

    /// <summary>Successful outcome with value.</summary>
    public static Result<T> Success(T value) => new(true, value, EmptyErrors);

    /// <summary>Failure from a single semantic error.</summary>
    public static Result<T> Failure(SemanticError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(false, default, new ReadOnlyCollection<SemanticError>([error]));
    }

    /// <summary>Failure from one or more semantic errors (primary = first).</summary>
    public static Result<T> Failure(IEnumerable<SemanticError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var list = errors as IList<SemanticError> ?? errors.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("result_failure_requires_errors", nameof(errors));
        }

        foreach (var error in list)
        {
            ArgumentNullException.ThrowIfNull(error);
        }

        return new Result<T>(false, default, new ReadOnlyCollection<SemanticError>(list.ToArray()));
    }

    /// <summary>Failure from one or more semantic errors.</summary>
    public static Result<T> Failure(params SemanticError[] errors) => Failure((IEnumerable<SemanticError>)errors);

    /// <summary>Match success / failure branches.</summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<SemanticError>, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(_errors);
    }

    /// <summary>Implicit conversion from non-generic failure Result.</summary>
    public static implicit operator Result<T>(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("result_implicit_typed_requires_failure_or_value");
        }

        return Failure(result.Errors);
    }
}
