using Microsoft.AspNetCore.Http;
using Tooba.AccessControl.Application.Models;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;

namespace Tooba.AccessControl.Endpoints.Errors;

/// <summary>Canonical AccessControl HTTP error presentation (no code.Contains heuristics).</summary>
internal static class AccessControlHttpErrors
{
    public static IResult From(Exception exception, ApiResponseFactory api) =>
        exception switch
        {
            AccessControlException ace => api.FromSemanticException(new SemanticException(new SemanticError(ace.Code))),
            SemanticException semantic => api.FromSemanticException(semantic),
            PlatformHttpException platform => api.FromPlatformException(platform),
            _ => throw exception,
        };
}
