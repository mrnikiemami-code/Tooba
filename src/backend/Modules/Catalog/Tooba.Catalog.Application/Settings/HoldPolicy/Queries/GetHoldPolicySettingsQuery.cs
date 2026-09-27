using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.HoldPolicy.Models;

namespace Tooba.Catalog.Application.Settings.HoldPolicy.Queries;

/// <summary>Admin GET hold-policy settings aggregate.</summary>
public sealed record GetHoldPolicySettingsQuery : IRequest<Result<HoldPolicySettingsView>>;
