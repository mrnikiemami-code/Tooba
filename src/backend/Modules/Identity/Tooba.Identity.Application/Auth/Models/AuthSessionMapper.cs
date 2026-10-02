using Tooba.Identity.Contracts;

namespace Tooba.Identity.Application.Auth.Models;

/// <summary>Maps authentication tickets to HTTP session DTOs.</summary>
public static class AuthSessionMapper
{
    /// <summary>Builds the public session projection from an issued ticket.</summary>
    public static AuthSessionDto FromTicket(AuthenticationTicket ticket) =>
        new(ticket.UserId, ticket.SessionHandle, ticket.SessionHandle.ToString("D"), ticket.RefreshToken!);
}
