using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش نشست، چرخش Refresh، چالش پایدار و isolation بدون UI ورود.
/// </summary>
[Collection("PostgresSerial")]
public sealed class IdentityLifecycleTests : IAsyncLifetime
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
                .WithDatabase("tooba_identity_life_a")
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

    [Fact]
    public void Opaque_secret_hash_is_not_the_raw_secret()
    {
        var raw = OpaqueSecretHasher.Generate();
        var hash = OpaqueSecretHasher.Hash(raw);
        Assert.NotEqual(raw, hash);
        Assert.Equal(hash, OpaqueSecretHasher.Hash(raw));
        Assert.DoesNotContain(raw, hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Access_boundary_strips_refresh_secret_without_minting_jwt()
    {
        var boundary = new SessionAccessCredentialBoundary();
        var ticket = new AuthenticationTicket
        {
            UserId = Guid.NewGuid(),
            SessionHandle = Guid.NewGuid(),
            RefreshToken = "raw-refresh",
            AuthenticatedAt = DateTimeOffset.UtcNow,
        };
        var access = boundary.ToAccessTicket(ticket);
        Assert.Null(access.RefreshToken);
        Assert.Equal(ticket.SessionHandle, access.SessionHandle);
    }

    [SkippableFact]
    public async Task Session_rotation_challenges_and_tenant_isolation_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_identity_life_b'";
            if (await cmd.ExecuteScalarAsync() is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_identity_life_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_identity_life_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-a", "tenant-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-b", "tenant-b"));

        await using var dbA = CreateDb(csA);
        await using var dbB = CreateDb(csB);
        await dbA.Database.EnsureCreatedAsync();
        await dbB.Database.EnsureCreatedAsync();

        var hasher = new AspNetPasswordHashingService();
        var sink = new InMemoryIdentitySecurityEventSink();
        var sender = new CapturingOtpSender();
        var auth = IdentityTestFactory.CreateAuth(dbA, hasher, sink, commerceA, sender: sender);
        var life = IdentityTestFactory.CreateLifecycle(dbA, hasher, sink, sender, commerceA);

        var created = await auth.RegisterAsync(
            new RegisterUserCommand
            {
                IdentifierKind = LoginIdentifierKind.Email,
                Identifier = "life@example.com",
                Password = "correct-horse",
            },
            CancellationToken.None);

        var login = await auth.AuthenticateWithPasswordAsync(
            LoginIdentifierKind.Email,
            "life@example.com",
            "correct-horse",
            CancellationToken.None);
        Assert.True(login.Succeeded);
        var rawRefresh = login.Ticket!.RefreshToken;
        Assert.False(string.IsNullOrWhiteSpace(rawRefresh));
        var sessionRow = await dbA.Sessions.AsNoTracking().SingleAsync(x => x.SessionId == login.Ticket.SessionHandle);
        Assert.NotEqual(rawRefresh, sessionRow.RefreshSecretHash);
        Assert.DoesNotContain(rawRefresh!, sessionRow.RefreshSecretHash, StringComparison.Ordinal);
        Assert.Equal("tenant-a", sessionRow.TenantId);

        var rotated = await auth.RefreshSessionAsync(login.Ticket.SessionHandle, rawRefresh!, CancellationToken.None);
        Assert.True(rotated.Succeeded);
        Assert.NotEqual(rawRefresh, rotated.Ticket!.RefreshToken);
        var replayOld = await auth.RefreshSessionAsync(login.Ticket.SessionHandle, rawRefresh!, CancellationToken.None);
        Assert.Equal(AuthenticationOutcome.RefreshReuse, replayOld.Outcome);
        Assert.Equal(PublicAuthenticationError.InvalidCredentials, replayOld.PublicError);

        var secondLogin = await auth.AuthenticateWithPasswordAsync(
            LoginIdentifierKind.Email,
            "life@example.com",
            "correct-horse",
            CancellationToken.None);
        await auth.RevokeSessionAsync(secondLogin.Ticket!.SessionHandle, "test_revoke", CancellationToken.None);
        var revokedRefresh = await auth.RefreshSessionAsync(
            secondLogin.Ticket.SessionHandle,
            secondLogin.Ticket.RefreshToken!,
            CancellationToken.None);
        Assert.Equal(AuthenticationOutcome.RevokedSession, revokedRefresh.Outcome);

        var third = await auth.AuthenticateWithPasswordAsync(
            LoginIdentifierKind.Email,
            "life@example.com",
            "correct-horse",
            CancellationToken.None);
        await auth.RevokeAllSessionsAsync(created.UserId, "test_revoke_all", CancellationToken.None);
        var allRevoked = await auth.RefreshSessionAsync(third.Ticket!.SessionHandle, third.Ticket.RefreshToken!, CancellationToken.None);
        Assert.Equal(AuthenticationOutcome.RevokedSession, allRevoked.Outcome);

        var fourth = await auth.AuthenticateWithPasswordAsync(
            LoginIdentifierKind.Email,
            "life@example.com",
            "correct-horse",
            CancellationToken.None);
        await auth.ChangePasswordAsync(created.UserId, "correct-horse", "new-horse-battery", CancellationToken.None);
        var afterChange = await auth.RefreshSessionAsync(fourth.Ticket!.SessionHandle, fourth.Ticket.RefreshToken!, CancellationToken.None);
        Assert.Equal(AuthenticationOutcome.RevokedSession, afterChange.Outcome);
        var relogin = await auth.AuthenticateWithPasswordAsync(
            LoginIdentifierKind.Email,
            "life@example.com",
            "new-horse-battery",
            CancellationToken.None);
        Assert.True(relogin.Succeeded);

        var unknown = await life.RequestPasswordResetAsync(LoginIdentifierKind.Email, "nobody@example.com", CancellationToken.None);
        var known = await life.RequestPasswordResetAsync(LoginIdentifierKind.Email, "life@example.com", CancellationToken.None);
        Assert.True(unknown.Accepted);
        Assert.True(known.Accepted);
        Assert.Null(unknown.ChallengeId);
        Assert.NotNull(known.ChallengeId);
        var resetRow = await dbA.Challenges.AsNoTracking().SingleAsync(x => x.ChallengeId == known.ChallengeId);
        Assert.NotEqual(sender.LastCode, resetRow.SecretHash);
        Assert.Equal(OtpPurpose.PasswordReset, resetRow.Purpose);

        var expiredId = known.ChallengeId!.Value;
        var trackedReset = await dbA.Challenges.FirstAsync(x => x.ChallengeId == expiredId);
        trackedReset.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await dbA.SaveChangesAsync();
        Assert.Equal(
            ChallengeConsumeOutcome.InvalidOrExpired,
            await life.CompletePasswordResetAsync(expiredId, sender.LastCode!, "fresh-password", CancellationToken.None));

        var reset2 = await life.RequestPasswordResetAsync(LoginIdentifierKind.Email, "life@example.com", CancellationToken.None);
        var resetSecret = sender.LastCode!;
        Assert.Equal(
            ChallengeConsumeOutcome.Succeeded,
            await life.CompletePasswordResetAsync(reset2.ChallengeId!.Value, resetSecret, "reset-horse-1", CancellationToken.None));
        Assert.Equal(
            ChallengeConsumeOutcome.Consumed,
            await life.CompletePasswordResetAsync(reset2.ChallengeId!.Value, resetSecret, "reset-horse-2", CancellationToken.None));

        var verifyHandle = await life.IssueIdentifierVerificationAsync(
            created.UserId,
            LoginIdentifierKind.Email,
            "life@example.com",
            CancellationToken.None);
        var identifierBefore = await dbA.Identifiers.AsNoTracking()
            .SingleAsync(x => x.UserId == created.UserId && x.Kind == LoginIdentifierKind.Email);
        Assert.Equal(IdentifierVerificationState.Unverified, identifierBefore.VerificationState);
        var verifyCode = sender.LastCode!;
        Assert.Equal(
            ChallengeConsumeOutcome.InvalidOrExpired,
            await life.CompleteIdentifierVerificationAsync(verifyHandle.ChallengeId, "00000000", CancellationToken.None));
        var afterWrong = await dbA.Challenges.AsNoTracking().SingleAsync(x => x.ChallengeId == verifyHandle.ChallengeId);
        Assert.Equal(1, afterWrong.AttemptCount);
        Assert.Equal(
            ChallengeConsumeOutcome.Succeeded,
            await life.CompleteIdentifierVerificationAsync(verifyHandle.ChallengeId, verifyCode, CancellationToken.None));
        var idEntity = await dbA.Identifiers.FirstAsync(x => x.Id == identifierBefore.Id);
        await dbA.Entry(idEntity).ReloadAsync();
        Assert.Equal(IdentifierVerificationState.Verified, idEntity.VerificationState);

        var tight = IdentityTestFactory.CreateLifecycle(
            dbA,
            hasher,
            sink,
            sender,
            commerceA,
            new IdentityLifecycleOptions { MaxChallengeAttempts = 2, ChallengeLifetimeMinutes = 15 });
        var limited = await tight.IssueAsync(OtpPurpose.Login, "life@example.com", CancellationToken.None);
        Assert.False(await tight.VerifyAsync(limited, "11111111", CancellationToken.None));
        Assert.False(await tight.VerifyAsync(limited, "22222222", CancellationToken.None));
        var persistedOtp = await dbA.Challenges.AsNoTracking().SingleAsync(x => x.ChallengeId == limited.ChallengeId);
        Assert.NotNull(persistedOtp.LockedAt);
        Assert.Equal(2, persistedOtp.AttemptCount);
        Assert.NotEqual(sender.LastCode, persistedOtp.SecretHash);

        var disabledLogin = await auth.AuthenticateWithPasswordAsync(
            LoginIdentifierKind.Email,
            "life@example.com",
            "reset-horse-1",
            CancellationToken.None);
        await auth.DisableAsync(created.UserId, CancellationToken.None);
        var disabledRefresh = await auth.RefreshSessionAsync(
            disabledLogin.Ticket!.SessionHandle,
            disabledLogin.Ticket.RefreshToken!,
            CancellationToken.None);
        Assert.Equal(AuthenticationOutcome.Disabled, disabledRefresh.Outcome);

        Assert.DoesNotContain(sink.Events, e => (e.EventName + (e.UserId?.ToString() ?? "")).Contains("refresh", StringComparison.OrdinalIgnoreCase)
            && sink.Events.Any(x => x.EventName.Contains("eyJ", StringComparison.Ordinal)));
        Assert.All(sink.Events, e =>
        {
            Assert.DoesNotContain("correct-horse", e.EventName, StringComparison.Ordinal);
            Assert.DoesNotContain("reset-horse", e.EventName, StringComparison.Ordinal);
            Assert.False(e.EventName.Contains(rawRefresh ?? "---", StringComparison.Ordinal));
        });
        Assert.Contains(sink.Events, e => e.EventName == "session_created");
        Assert.Contains(sink.Events, e => e.EventName == "session_revoked");
        Assert.Contains(sink.Events, e => e.EventName == "password_changed");
        Assert.Contains(sink.Events, e => e.EventName == "password_reset_completed");
        Assert.Contains(sink.Events, e => e.EventName == "identifier_verified");
        Assert.Contains(sink.Events, e => e.EventName == "refresh_reuse_detected");

        var authB = IdentityTestFactory.CreateAuth(dbB, hasher, new InMemoryIdentitySecurityEventSink(), commerceB);
        await authB.RegisterAsync(
            new RegisterUserCommand
            {
                IdentifierKind = LoginIdentifierKind.Email,
                Identifier = "other@example.com",
                Password = "correct-horse",
            },
            CancellationToken.None);
        Assert.Null(await authB.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, "life@example.com", CancellationToken.None));
        Assert.Empty(await dbB.Sessions.AsNoTracking().Where(x => x.UserId == created.UserId).ToListAsync());
        Assert.Empty(await dbB.Challenges.AsNoTracking().Where(x => x.UserId == created.UserId).ToListAsync());
    }

    private static IdentityDbContext CreateDb(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, IdentityDbContext.Schema, typeof(IdentityDbContext));
        return new IdentityDbContext(options.Options);
    }
}
