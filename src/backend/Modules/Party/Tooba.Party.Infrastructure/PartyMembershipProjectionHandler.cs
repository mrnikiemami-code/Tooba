using Tooba.BuildingBlocks;
using Tooba.Party.Infrastructure.Events;

namespace Tooba.Party.Infrastructure;

/// <summary>
/// تصویرسازی رابطهٔ مجوز پس از persist عضویت. داخل تراکنش DbContext Party اجرا نمی‌شود تا در دسترس نبودن SpiceDB commit کسب‌وکار را rollback نکند.
/// </summary>
public sealed class PartyMembershipProjectionHandler : IIntegrationEventHandler<PartyMembershipEstablishedIntegrationEvent>
{
    private readonly IAuthorizationTupleWriter _writer;

    /// <summary>
    /// handler را به abstraction Tooba وصل می‌کند نه به Authzed.Net.
    /// </summary>
    public PartyMembershipProjectionHandler(IAuthorizationTupleWriter writer) => _writer = writer;

    /// <inheritdoc />
    public Task HandleAsync(PartyMembershipEstablishedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return _writer.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(integrationEvent.UserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Party,
                    Id = integrationEvent.PartyId.ToString("D"),
                },
                Relation = AuthorizationRelations.Member,
            },
            cancellationToken);
    }
}
