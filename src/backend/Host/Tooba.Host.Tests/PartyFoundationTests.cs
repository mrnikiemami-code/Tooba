using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Identity.Domain;
using Tooba.Party.Application;
using Tooba.Party.Domain;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Events;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation Party بدون UI تجاری و بدون نقش ثابت Seller/Agency روی User.
/// </summary>
[Collection("PostgresSerial")]
public sealed class PartyFoundationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <summary>
    /// Postgres واقعی را برای isolation Tenant بالا می‌آورد.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_party_a")
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

    /// <summary>
    /// کانتینر را آزاد می‌کند.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public void UserAccount_has_no_party_or_organization_fields()
    {
        var names = typeof(UserAccount).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("PartyId", names);
        Assert.DoesNotContain("OrganizationId", names);
        Assert.DoesNotContain("SellerId", names);
        Assert.DoesNotContain("MembershipId", names);
    }

    [Fact]
    public void Party_projects_do_not_reference_authzed_or_identity_persistence()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     Path.Combine(root, "src", "backend", "Modules", "Party", "Tooba.Party.Domain"),
                     Path.Combine(root, "src", "backend", "Modules", "Party", "Tooba.Party.Application"),
                     Path.Combine(root, "src", "backend", "Modules", "Party", "Tooba.Party.Infrastructure"),
                 })
        {
            var csproj = File.ReadAllText(Directory.GetFiles(project, "*.csproj").Single());
            Assert.DoesNotContain("Authzed", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tooba.Identity", csproj, StringComparison.Ordinal);
        }

        var domain = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Party", "Tooba.Party.Domain", "PartyDomain.cs"));
        var application = File.ReadAllText(Path.Combine(root, "src", "backend", "Modules", "Party", "Tooba.Party.Application", "PartyContracts.cs"));
        Assert.DoesNotContain("Authzed", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Authzed", application, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Permission", typeof(PartyMembership).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("Role", typeof(PartyMembership).GetProperties().Select(p => p.Name));
        Assert.Equal("party", PartyDbContext.Schema);
    }

    [Fact]
    public void Organization_relationship_codes_are_extensible_not_seller_only_enum()
    {
        Assert.Equal("parent_of", OrganizationRelationCodes.ParentOf);
        Assert.Equal("operated_by", OrganizationRelationCodes.OperatedBy);
        Assert.Equal("represents", OrganizationRelationCodes.Represents);
        Assert.DoesNotContain("SellerOnly", Enum.GetNames<PartyKind>());
        Assert.Equal("seller", PartyCapabilityCodes.Seller);
    }

    [Fact]
    public async Task Projection_handler_writes_authorization_outside_party_db_transaction()
    {
        var writer = new RecordingAuthorizationTupleWriter();
        var handler = new PartyMembershipProjectionHandler(writer);
        var evt = new PartyMembershipEstablishedIntegrationEvent
        {
            MembershipId = Guid.NewGuid(),
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            PartyId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            RelationCode = MembershipRelationCodes.Member,
        };

        Assert.Empty(writer.Writes);
        await handler.HandleAsync(evt, CancellationToken.None);
        Assert.Single(writer.Writes);
        Assert.Equal(AuthorizationObjectTypes.Party, writer.Writes[0].Resource.Type);
        Assert.Equal(AuthorizationRelations.Member, writer.Writes[0].Relation);
    }

    [SkippableFact]
    public async Task Person_org_memberships_outbox_and_tenant_isolation_on_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var csA = _container.GetConnectionString();
        await using (var admin = new Npgsql.NpgsqlConnection(csA))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'tooba_party_b'";
            var exists = await cmd.ExecuteScalarAsync();
            if (exists is null)
            {
                await using var create = admin.CreateCommand();
                create.CommandText = "CREATE DATABASE tooba_party_b";
                await create.ExecuteNonQueryAsync();
            }
        }

        var csB = new Npgsql.NpgsqlConnectionStringBuilder(csA) { Database = "tooba_party_b" }.ConnectionString;
        var commerceA = new FixedCommerceContext();
        commerceA.Assign(OutboxTestContextFactory.SingleStore("tenant-a", "tenant-a"));
        var commerceB = new FixedCommerceContext();
        commerceB.Assign(OutboxTestContextFactory.SingleStore("tenant-b", "tenant-b"));

        await using var dbA = CreatePartyDb(csA, commerceA);
        await using var dbB = CreatePartyDb(csB, commerceB);
        await dbA.Database.EnsureCreatedAsync();
        await dbB.Database.EnsureCreatedAsync();

        var dirA = new PartyDirectory(dbA);
        var dirB = new PartyDirectory(dbB);
        var userOne = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var userTwo = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var person = await dirA.CreatePersonAsync("Alex Person", CancellationToken.None);
        var link = await dirA.LinkUserAsync(userOne, person.PartyId, CancellationToken.None);
        Assert.Equal(userOne, link.UserId);
        Assert.Equal(person.PartyId, link.PartyId);

        var orgOne = await dirA.CreateOrganizationAsync("Org One", "Org One Ltd", CancellationToken.None);
        var orgTwo = await dirA.CreateOrganizationAsync("Org Two", null, CancellationToken.None);
        await dirA.GrantOrganizationCapabilityAsync(orgOne.PartyId, PartyCapabilityCodes.Seller, CancellationToken.None);
        await dirA.GrantOrganizationCapabilityAsync(orgOne.PartyId, PartyCapabilityCodes.Agency, CancellationToken.None);

        var m1 = await dirA.EstablishMembershipAsync(userOne, orgOne.PartyId, MembershipRelationCodes.Member, CancellationToken.None);
        var m2 = await dirA.EstablishMembershipAsync(userTwo, orgOne.PartyId, MembershipRelationCodes.Member, CancellationToken.None);
        var m3 = await dirA.EstablishMembershipAsync(userOne, orgTwo.PartyId, MembershipRelationCodes.Member, CancellationToken.None);
        Assert.Equal(orgOne.PartyId, m1.PartyId);
        Assert.Equal(orgOne.PartyId, m2.PartyId);
        Assert.Equal(orgTwo.PartyId, m3.PartyId);

        var rel = await dirA.RelateOrganizationsAsync(orgOne.PartyId, orgTwo.PartyId, OrganizationRelationCodes.ParentOf, CancellationToken.None);
        Assert.Equal(OrganizationRelationCodes.ParentOf, rel.RelationCode);

        var outbox = await dbA.OutboxMessages.AsNoTracking().ToListAsync();
        Assert.Contains(outbox, row => row.EventType == PartyMembershipEstablishedIntegrationEvent.EventTypeName);
        Assert.DoesNotContain(outbox, row => row.Payload.Contains("Authzed", StringComparison.OrdinalIgnoreCase));

        var writer = new RecordingAuthorizationTupleWriter();
        Assert.Empty(writer.Writes);
        var handler = new PartyMembershipProjectionHandler(writer);
        var serializer = new JsonIntegrationEventSerializer([new PartyOutboxRegistration()]);
        var integration = (PartyMembershipEstablishedIntegrationEvent)serializer.Deserialize(outbox.First(r => r.EventType == PartyMembershipEstablishedIntegrationEvent.EventTypeName));
        await handler.HandleAsync(integration, CancellationToken.None);
        Assert.NotEmpty(writer.Writes);

        var orgB = await dirB.CreateOrganizationAsync("Tenant B Org", null, CancellationToken.None);
        Assert.Null(await dirB.FindByIdAsync(orgOne.PartyId, CancellationToken.None));
        Assert.Null(await dirA.FindByIdAsync(orgB.PartyId, CancellationToken.None));
        Assert.Empty(await dbB.Memberships.AsNoTracking().Where(x => x.PartyId == orgOne.PartyId).ToListAsync());
    }

    private static PartyDbContext CreatePartyDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PartyOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PartyDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PartyDbContext.Schema, typeof(PartyDbContext));
        options.AddInterceptors(interceptor);
        return new PartyDbContext(options.Options);
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
/// نویسندهٔ جعلی مجوز برای اثبات اینکه تصویرسازی خارج از تراکنش Party است.
/// </summary>
internal sealed class RecordingAuthorizationTupleWriter : IAuthorizationTupleWriter
{
    /// <summary>
    /// نوشتن‌های دیده‌شده پس از handler نه در SaveChanges.
    /// </summary>
    public List<AuthorizationRelationshipWrite> Writes { get; } = [];

    /// <inheritdoc />
    public Task WriteAsync(AuthorizationRelationshipWrite write, CancellationToken cancellationToken)
    {
        Writes.Add(write);
        return Task.CompletedTask;
    }
}
