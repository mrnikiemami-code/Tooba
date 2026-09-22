using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Promotion.Application.Commands.ActivateSellerPromotion;
using Tooba.Promotion.Application.Commands.CreateSellerPromotion;
using Tooba.Promotion.Application.Commands.DeactivateSellerPromotion;
using Tooba.Promotion.Application.Commands.UpdateSellerPromotion;
using Tooba.Promotion.Application.Models;
using Tooba.Promotion.Application.Queries.GetSellerPromotion;
using Tooba.Promotion.Application.Queries.ListSellerPromotions;
namespace Tooba.Promotion.Endpoints.Seller;

public static class PromotionSellerEndpoints
{
    public static void Map(IEndpointRouteBuilder app) { 
        var g = app.MapGroup("/v1/seller/promotions"); 
        g.MapGet("", List); 
        g.MapPost("", Create); 
        g.MapGet("/{id:guid}", Get); 
        g.MapPut("/{id:guid}", Update); 
        g.MapPost("/{id:guid}/activate", Activate); 
        g.MapPost("/{id:guid}/deactivate", Deactivate); 
    }
    private static async Task<IResult> List(ISender s, IPromotionSellerAuthorizer a, ApiResponseFactory api, HttpContext c, CancellationToken ct) => 
        api.From(await s.Send(new ListSellerPromotionsQuery(await a.RequireSellerPartyIdAsync(c, ct)), ct));
    private static async Task<IResult> Get(Guid id, ISender s, IPromotionSellerAuthorizer a, ApiResponseFactory api, HttpContext c, CancellationToken ct) => 
        api.From(await s.Send(new GetSellerPromotionQuery(await a.RequireSellerPartyIdAsync(c, ct), id), ct));
    private static async Task<IResult> Create(UpsertSellerPromotionBody b, ISender s, IPromotionSellerAuthorizer a, ApiResponseFactory api, HttpContext c, CancellationToken ct) 
    { 
        var r = await s.Send(new CreateSellerPromotionCommand(await a.RequireSellerPartyIdAsync(c, ct), b.ToInput()), ct); 
        return r.IsSuccess ? Results.Json(r.Value, statusCode: 201) : api.From(r); 
    }
    private static async Task<IResult> Update(Guid id, UpsertSellerPromotionBody b, ISender s, IPromotionSellerAuthorizer a, ApiResponseFactory api, HttpContext c, CancellationToken ct) => 
        api.From(await s.Send(new UpdateSellerPromotionCommand(await a.RequireSellerPartyIdAsync(c, ct), id, b.ToInput()), ct));
    private static async Task<IResult> Activate(Guid id, ISender s, IPromotionSellerAuthorizer a, ApiResponseFactory api, HttpContext c, CancellationToken ct) => 
        api.From(await s.Send(new ActivateSellerPromotionCommand(await a.RequireSellerPartyIdAsync(c, ct), id), ct));
    private static async Task<IResult> Deactivate(Guid id, ISender s, IPromotionSellerAuthorizer a, ApiResponseFactory api, HttpContext c, CancellationToken ct) => 
        api.From(await s.Send(new DeactivateSellerPromotionCommand(await a.RequireSellerPartyIdAsync(c, ct), id), ct));
}
public sealed record UpsertSellerPromotionBody(string Name, string CouponCode, string DiscountKind, decimal DiscountValue, DateTimeOffset? EffectiveFrom, DateTimeOffset? EffectiveTo, string? Currency = null, decimal? MinimumSubtotal = null) { public PromotionMutationInput ToInput() => new(Name, CouponCode, DiscountKind, DiscountValue, EffectiveFrom, EffectiveTo, Currency, MinimumSubtotal); }
