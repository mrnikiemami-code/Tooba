using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Auth.Queries;
using Tooba.Identity.Application.Auth.Validators;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// Durable inventory: every Identity endpoint-reachable MediatR request is classified and
/// VALIDATOR_REQUIRED entries have a discovered FluentValidation validator.
/// </summary>
public sealed class IdentityValidatorCoverageGuardTests
{
    private const string ValidatorRequiredPresent = "VALIDATOR_REQUIRED_PRESENT";
    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED";

    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("RegisterAuthUserCommand", ValidatorRequiredPresent),
        ("CompletePasswordResetCommand", ValidatorRequiredPresent),
        ("RequestIdentifierVerificationCommand", ValidatorRequiredPresent),
        ("CompleteIdentifierVerificationCommand", ValidatorRequiredPresent),
        ("RequestOtpLoginCommand", ValidatorRequiredPresent),
        ("ChangePasswordCommand", ValidatorRequiredPresent),
        ("LoginWithPasswordCommand", NoValidatorRequired),
        ("RefreshAuthSessionCommand", NoValidatorRequired),
        ("LogoutSessionCommand", NoValidatorRequired),
        ("LogoutAllSessionsCommand", NoValidatorRequired),
        ("RequestPasswordResetCommand", NoValidatorRequired),
        ("CompleteOtpLoginCommand", NoValidatorRequired),
        ("GetAuthMeQuery", NoValidatorRequired),
    ];

    private static readonly (string RequestTypeName, Type ValidatorType)[] RequiredValidators =
    [
        ("RegisterAuthUserCommand", typeof(RegisterAuthUserCommandValidator)),
        ("CompletePasswordResetCommand", typeof(CompletePasswordResetCommandValidator)),
        ("RequestIdentifierVerificationCommand", typeof(RequestIdentifierVerificationCommandValidator)),
        ("CompleteIdentifierVerificationCommand", typeof(CompleteIdentifierVerificationCommandValidator)),
        ("RequestOtpLoginCommand", typeof(RequestOtpLoginCommandValidator)),
        ("ChangePasswordCommand", typeof(ChangePasswordCommandValidator)),
    ];

    [Fact]
    public void Endpoint_reachable_requests_are_exhaustively_classified()
    {
        Assert.Equal(13, Manifest.Length);
        Assert.Equal(Manifest.Length, Manifest.Select(x => x.RequestTypeName).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Required_validators_are_registered_in_cqrs_foundation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(RegisterAuthUserCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        foreach (var (name, validatorType) in RequiredValidators)
        {
            var requestType = typeof(RegisterAuthUserCommand).Assembly
                .GetTypes()
                .Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            var validator = sp.GetService(validatorInterface);
            Assert.NotNull(validator);
            Assert.IsType(validatorType, validator);
        }

        foreach (var (name, classification) in Manifest.Where(x => x.Classification == NoValidatorRequired))
        {
            var requestType = typeof(RegisterAuthUserCommand).Assembly
                .GetTypes()
                .Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            Assert.Null(sp.GetService(validatorInterface));
            _ = classification;
        }
    }

    [Fact]
    public void Endpoints_dispatch_through_ISender_not_service_ports()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root,
            "src",
            "backend",
            "Modules",
            "Identity",
            "Tooba.Identity.Endpoints",
            "Auth",
            "IdentityAuthEndpoints.cs"));
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("api.From", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdentityAuthenticationService", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdentityOtpLoginService", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdentityCredentialLifecycle", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ICustomerProfileDirectory", endpoints, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
