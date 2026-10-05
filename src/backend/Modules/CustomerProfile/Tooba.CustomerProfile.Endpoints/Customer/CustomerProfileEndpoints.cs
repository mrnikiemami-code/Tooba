using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.CustomerProfile.Application.Commands.UpsertCustomerProfile;
using Tooba.CustomerProfile.Application.Queries.GetCustomerProfilePage;
using Tooba.CustomerProfile.Contracts;
using Tooba.CustomerProfile.Contracts.Errors;

namespace Tooba.CustomerProfile.Endpoints.Customer;

/// <summary>HTTP boundary for GET/PUT /v1/customer/profile — ISender + ApiResponseFactory.From(Result).</summary>
public static class CustomerProfileEndpoints
{
    /// <summary>Maps profile read/write under the customer group.</summary>
    public static void MapProfile(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/profile", GetProfileAsync);
        group.MapPut("/profile", UpdateProfileAsync);
    }

    private static async Task<IResult> GetProfileAsync(
        HttpContext httpContext,
        ICustomerAccountActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return Unauthorized(api);
        }

        var result = await sender.Send(new GetCustomerProfilePageQuery(actor.Value), cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> UpdateProfileAsync(
        CustomerProfileWriteRequest body,
        HttpContext httpContext,
        ICustomerAccountActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return Unauthorized(api);
        }

        var result = await sender.Send(
            new UpsertCustomerProfileCommand(actor.Value, body.ToWrite()),
            cancellationToken);
        return api.From(result);
    }

    private static IResult Unauthorized(ApiResponseFactory api) =>
        api.FromFailure(new SemanticError(CustomerProfileErrorCodes.SessionRequired));
}

/// <summary>HTTP body for profile update; Identity credentials are never accepted.</summary>
public sealed record CustomerProfileWriteRequest(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? BirthDate,
    string? Bio);

/// <summary>Maps HTTP body to module Contracts write without owner projection.</summary>
public static class CustomerProfileWriteRequestExtensions
{
    /// <summary>Converts HTTP input to <see cref="CustomerProfileWrite"/>.</summary>
    public static CustomerProfileWrite ToWrite(this CustomerProfileWriteRequest body) =>
        new(body.DisplayName, body.FirstName, body.LastName, body.BirthDate, body.Bio);
}
