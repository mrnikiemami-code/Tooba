using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش احراز هویت Identity بدون Party و بدون مدل مجوز کسب‌وکار.
/// </summary>
public sealed class IdentityFoundationTests
{
    [Theory]
    [InlineData("  User.Name ", "user.name")]
    public void Username_normalization_is_trim_and_casefold_not_email_rules(string raw, string expected)
    {
        var (_, normalized) = LoginIdentifierNormalizer.Normalize(LoginIdentifierKind.Username, raw);
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void Email_normalization_lowercases_full_address()
    {
        var (_, normalized) = LoginIdentifierNormalizer.Normalize(LoginIdentifierKind.Email, "  Alex.O@Example.COM ");
        Assert.Equal("alex.o@example.com", normalized);
        var usernameOfLocalPart = LoginIdentifierNormalizer.NormalizeUsername("Alex.O");
        Assert.NotEqual(usernameOfLocalPart, normalized);
    }

    [Fact]
    public void Phone_normalization_keeps_plus_and_digits_without_iran_default()
    {
        var (_, normalized) = LoginIdentifierNormalizer.Normalize(LoginIdentifierKind.Phone, "+44 20 7946 0958");
        Assert.Equal("+442079460958", normalized);
        Assert.False(normalized.StartsWith("+98", StringComparison.Ordinal));
    }

    [Fact]
    public void Identity_projects_do_not_reference_party_or_masstransit()
    {
        var backend = Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Identity");
        foreach (var file in Directory.GetFiles(backend, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("MassTransit", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CustomerId", text, StringComparison.Ordinal);
            Assert.DoesNotContain("OrganizationId", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SellerId", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SpiceDB", text, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal("identity", IdentityDbContext.Schema);
    }

    [Fact]
    public void Domain_and_application_csproj_have_no_masstransit_package()
    {
        var domain = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Identity", "Tooba.Identity.Domain", "Tooba.Identity.Domain.csproj"));
        var application = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "backend", "Modules", "Identity", "Tooba.Identity.Application", "Tooba.Identity.Application.csproj"));
        Assert.DoesNotContain("MassTransit", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MassTransit", application, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Otp_abstraction_is_provider_neutral_and_purpose_agnostic()
    {
        var sender = new CapturingOtpSender();
        var otp = new InMemoryOtpChallengeService(sender);
        var login = await otp.IssueAsync(OtpPurpose.Login, "dest", CancellationToken.None);
        var reset = await otp.IssueAsync(OtpPurpose.PasswordReset, "dest", CancellationToken.None);
        Assert.NotEqual(login.ChallengeId, reset.ChallengeId);
        Assert.Equal(OtpPurpose.PasswordReset, sender.LastPurpose);
        Assert.False(await otp.VerifyAsync(login, sender.LastCode!, CancellationToken.None));
        Assert.True(await otp.VerifyAsync(reset, sender.LastCode!, CancellationToken.None));
        Assert.IsAssignableFrom<IOtpSender>(sender);
    }

    [Fact]
    public void Public_auth_failure_collapses_disabled_and_locked()
    {
        var disabled = AuthenticationResult.Fail(AuthenticationOutcome.Disabled);
        var locked = AuthenticationResult.Fail(AuthenticationOutcome.Locked);
        var invalid = AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        Assert.Equal(PublicAuthenticationError.InvalidCredentials, disabled.PublicError);
        Assert.Equal(PublicAuthenticationError.InvalidCredentials, locked.PublicError);
        Assert.Equal(PublicAuthenticationError.InvalidCredentials, invalid.PublicError);
        Assert.NotEqual(disabled.Outcome, locked.Outcome);
    }

    [Fact]
    public void Password_hasher_does_not_store_plaintext_and_is_rehash_ready()
    {
        var hasher = new AspNetPasswordHashingService();
        var hash = hasher.Hash("correct-horse-battery");
        Assert.DoesNotContain("correct-horse-battery", hash, StringComparison.Ordinal);
        Assert.Equal(PasswordVerificationOutcome.Success, hasher.Verify(hash, "correct-horse-battery"));
        Assert.Equal(PasswordVerificationOutcome.Failed, hasher.Verify(hash, "wrong-password"));
        Assert.Contains(Enum.GetValues<PasswordVerificationOutcome>(), v => v == PasswordVerificationOutcome.SuccessRehashNeeded);
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

        throw new InvalidOperationException("Repository root not found.");
    }
}

/// <summary>
/// تست PostgreSQL واقعی برای یکتایی شناسه و ورود رمز.
/// </summary>
public sealed class IdentityPostgresTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_identity")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Register_authenticate_duplicate_and_status_rules_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container!.GetConnectionString());
        await db.Database.EnsureCreatedAsync();
        var hasher = new AspNetPasswordHashingService();
        var sink = new InMemoryIdentitySecurityEventSink();
        var auth = IdentityTestFactory.CreateAuth(db, hasher, sink);

        var created = await auth.RegisterAsync(
            new RegisterUserCommand
            {
                IdentifierKind = LoginIdentifierKind.Email,
                Identifier = "  Alex@Example.COM ",
                Password = "correct-horse",
            },
            CancellationToken.None);

        var id = await auth.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, "alex@example.com", CancellationToken.None);
        Assert.Equal(created.UserId, id);

        await Assert.ThrowsAsync<IdentityDuplicateIdentifierException>(() => auth.RegisterAsync(
            new RegisterUserCommand
            {
                IdentifierKind = LoginIdentifierKind.Email,
                Identifier = "alex@example.com",
                Password = "correct-horse",
            },
            CancellationToken.None));

        var user = await db.Users.Include(x => x.Identifiers).Include(x => x.Password).FirstAsync(x => x.UserId == created.UserId);
        user.AddIdentifier(LoginIdentifierKind.Username, "Alex.User", DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        Assert.Equal(2, user.Identifiers.Count);
        Assert.Equal(created.UserId, await auth.FindUserIdByIdentifierAsync(LoginIdentifierKind.Username, "alex.user", CancellationToken.None));
        Assert.DoesNotContain("correct-horse", user.Password!.PasswordHash, StringComparison.Ordinal);

        var ok = await auth.AuthenticateWithPasswordAsync(LoginIdentifierKind.Email, "alex@example.com", "correct-horse", CancellationToken.None);
        Assert.True(ok.Succeeded);
        Assert.Equal(created.UserId, ok.Ticket!.UserId);

        var bad = await auth.AuthenticateWithPasswordAsync(LoginIdentifierKind.Email, "alex@example.com", "nope-nope-nope", CancellationToken.None);
        Assert.False(bad.Succeeded);
        Assert.Equal(PublicAuthenticationError.InvalidCredentials, bad.PublicError);

        await auth.DisableAsync(created.UserId, CancellationToken.None);
        var disabled = await auth.AuthenticateWithPasswordAsync(LoginIdentifierKind.Email, "alex@example.com", "correct-horse", CancellationToken.None);
        Assert.Equal(AuthenticationOutcome.Disabled, disabled.Outcome);
        Assert.Equal(PublicAuthenticationError.InvalidCredentials, disabled.PublicError);

        var second = await auth.RegisterAsync(
            new RegisterUserCommand
            {
                IdentifierKind = LoginIdentifierKind.Phone,
                Identifier = "+1 (202) 555-0100",
                Password = "another-pass",
            },
            CancellationToken.None);
        await auth.LockAsync(second.UserId, CancellationToken.None);
        var locked = await auth.AuthenticateWithPasswordAsync(LoginIdentifierKind.Phone, "+12025550100", "another-pass", CancellationToken.None);
        Assert.Equal(AuthenticationOutcome.Locked, locked.Outcome);
        Assert.Equal(PublicAuthenticationError.InvalidCredentials, locked.PublicError);

        var directory = new EfExternalIdentityDirectory(db);
        await directory.BindAsync(created.UserId, "https://idp.example/realms/tooba", "sub-1", CancellationToken.None);
        Assert.Equal(created.UserId, await directory.FindUserIdAsync("https://idp.example/realms/tooba", "sub-1", CancellationToken.None));

        var mfa = new EfMfaEnrollmentStore(db);
        await mfa.EnrollAsync(created.UserId, MfaFactorKind.Totp, CancellationToken.None);
        Assert.Contains(MfaFactorKind.Totp, await mfa.ListEnabledAsync(created.UserId, CancellationToken.None));

        Assert.Contains(sink.Events, e => e.EventName == "login_success");
        Assert.Contains(sink.Events, e => e.EventName == "login_failure");

        var rehashUser = await auth.RegisterAsync(
            new RegisterUserCommand
            {
                IdentifierKind = LoginIdentifierKind.Username,
                Identifier = "rehash-user",
                Password = "correct-horse",
            },
            CancellationToken.None);
        var stored = await db.Users.Include(x => x.Password).FirstAsync(x => x.UserId == rehashUser.UserId);
        stored.Password!.PasswordHash = "legacy-format";
        await db.SaveChangesAsync();
        var rehashAuth = IdentityTestFactory.CreateAuth(db, new ForcedRehashPasswordHashingService(hasher), sink);
        var rehashed = await rehashAuth.AuthenticateWithPasswordAsync(LoginIdentifierKind.Username, "rehash-user", "correct-horse", CancellationToken.None);
        Assert.True(rehashed.Succeeded);
        await db.Entry(stored).ReloadAsync();
        Assert.NotEqual("legacy-format", stored.Password!.PasswordHash);
    }

    private static IdentityDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, IdentityDbContext.Schema, typeof(IdentityDbContext));
        return new IdentityDbContext(options.Options);
    }
}

/// <summary>
/// hasher آزمایشی که یک‌بار SuccessRehashNeeded برمی‌گرداند تا مسیر ارتقای قالب هش پوشش داده شود.
/// </summary>
internal sealed class ForcedRehashPasswordHashingService : IPasswordHashingService
{
    private readonly IPasswordHashingService _inner;
    private bool _rehashed;

    /// <summary>
    /// پوشش hasher واقعی پس از یک Verify بازسازی‌شونده.
    /// </summary>
    public ForcedRehashPasswordHashingService(IPasswordHashingService inner) => _inner = inner;

    /// <inheritdoc />
    public string Hash(string password) => _inner.Hash(password);

    /// <inheritdoc />
    public PasswordVerificationOutcome Verify(string hash, string password)
    {
        if (!_rehashed && hash == "legacy-format")
        {
            _rehashed = true;
            return PasswordVerificationOutcome.SuccessRehashNeeded;
        }

        return _inner.Verify(hash, password);
    }
}

/// <summary>
/// ساخت سرویس احراز برای تست بدون Host HTTP.
/// </summary>
internal static class IdentityTestFactory
{
    /// <summary>
    /// Authentication و lifecycle را روی یک DbContext و فرستندهٔ حافظه می‌سازد.
    /// </summary>
    public static IdentityAuthenticationService CreateAuth(
        IdentityDbContext db,
        IPasswordHashingService hasher,
        InMemoryIdentitySecurityEventSink sink,
        ICurrentCommerceContext? commerce = null,
        IdentityLifecycleOptions? lifecycle = null,
        CapturingOtpSender? sender = null)
    {
        sender ??= new CapturingOtpSender();
        var life = new IdentityLifecycleService(
            db,
            hasher,
            Options.Create(new IdentityPasswordPolicyOptions { MinimumLength = 10 }),
            Options.Create(lifecycle ?? new IdentityLifecycleOptions()),
            sink,
            commerce ?? new FixedCommerceContext(),
            sender);
        return new IdentityAuthenticationService(
            db,
            hasher,
            Options.Create(new IdentityPasswordPolicyOptions { MinimumLength = 10 }),
            sink,
            life);
    }

    /// <summary>
    /// فقط lifecycle را برای چالش/بازنشانی می‌سازد.
    /// </summary>
    public static IdentityLifecycleService CreateLifecycle(
        IdentityDbContext db,
        IPasswordHashingService hasher,
        InMemoryIdentitySecurityEventSink sink,
        CapturingOtpSender sender,
        ICurrentCommerceContext? commerce = null,
        IdentityLifecycleOptions? lifecycle = null) =>
        new(
            db,
            hasher,
            Options.Create(new IdentityPasswordPolicyOptions { MinimumLength = 10 }),
            Options.Create(lifecycle ?? new IdentityLifecycleOptions()),
            sink,
            commerce ?? new FixedCommerceContext(),
            sender);
}
