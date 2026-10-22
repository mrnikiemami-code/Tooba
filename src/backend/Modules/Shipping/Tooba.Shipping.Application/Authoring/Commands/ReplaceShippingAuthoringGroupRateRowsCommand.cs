using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Commands;

/// <summary>
/// جایگزینی کامل مجموعهٔ ردیف‌های نرخ یک گروه با بازبینی ریشهٔ مورد انتظار. مالک و ارز هرگز از
/// body نمی‌آیند: مالک از مجوزدهی سرور و ارز از زمینهٔ trusted است.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="GroupId">شناسهٔ گروه از مسیر.</param>
/// <param name="Request">بدنهٔ جایگزینی.</param>
public sealed record ReplaceShippingAuthoringGroupRateRowsCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    Guid GroupId,
    ShippingAuthoringRateRowsReplaceRequest Request)
    : IRequest<Result<ShippingAuthoringGroupRateRowsItem>>;

/// <summary>
/// هندلر جایگزینی ردیف‌های نرخ. ارز معتبر <b>پیش از هر نوشتن</b> از درز canonical زمینهٔ نویسندگی
/// خوانده می‌شود؛ اگر زمینهٔ trusted resolve نشده یا ارزی وجود ندارد، نوشتن fail-closed رد می‌شود و
/// هیچ ردیفی تغییر نمی‌کند.
/// </summary>
/// <remarks>
/// کل مجموعه در دامنه پیش‌سنجی می‌شود (شکل، بازهٔ نیم‌باز، دقت مبلغ، مدت تحویل، هم‌پوشانی کامل
/// سه‌محوری، هویت ردیف و ارز) و تنها پس از آن یک SaveChanges ضمنی اتمی ردیف‌ها و بازبینی ریشه را
/// با هم می‌نویسد. هیچ تراکنش دست‌سندی در این مسیر نیست.
/// </remarks>
public sealed class ReplaceShippingAuthoringGroupRateRowsHandler(
    IShippingAuthoringDraftDirectory directory,
    IShippingAuthoringContextSource contextSource)
    : IRequestHandler<ReplaceShippingAuthoringGroupRateRowsCommand, Result<ShippingAuthoringGroupRateRowsItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringGroupRateRowsItem>> Handle(
        ReplaceShippingAuthoringGroupRateRowsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);

        // Defense in depth for direct (non-HTTP) invocation: an explicit null collection is never
        // "replace with nothing"; [] stays a valid incomplete configuration.
        if (request.Request.Rows is null)
        {
            return Result.Failure<ShippingAuthoringGroupRateRowsItem>(
                new SemanticError(ShippingErrorCodes.AuthoringRateRowPayloadInvalid));
        }

        var snapshot = await contextSource.ReadAsync(cancellationToken);
        if (snapshot.Edition is not (ToobaEdition.SingleStore or ToobaEdition.Marketplace)
            || string.IsNullOrWhiteSpace(snapshot.DefaultCurrency))
        {
            return Result.Failure<ShippingAuthoringGroupRateRowsItem>(
                new SemanticError(ShippingErrorCodes.AuthoringConfigurationUnavailable));
        }

        IReadOnlyList<ShippingAuthoringDraft.ShippingAuthoringRateRowInput> rows;
        try
        {
            rows = ShippingAuthoringDraftWorkflow.ToDomainRateRows(request.Request.Rows);
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringGroupRateRowsItem>(exception.Error);
        }

        try
        {
            var replaced = await directory.ReplaceGroupRateRowsAsync(
                request.Owner,
                request.DraftId,
                request.GroupId,
                request.Request.ExpectedRevision,
                rows,
                snapshot.DefaultCurrency,
                cancellationToken);
            if (replaced.IsFailure)
            {
                return Result.Failure<ShippingAuthoringGroupRateRowsItem>(replaced.Errors);
            }

            var group = replaced.Value.FindRegionGroup(request.GroupId);
            return group is null
                ? Result.Failure<ShippingAuthoringGroupRateRowsItem>(
                    new SemanticError(ShippingErrorCodes.AuthoringRegionGroupNotFound))
                : Result.Success(ShippingAuthoringDraftWorkflow.MapRateRows(replaced.Value, group));
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringGroupRateRowsItem>(exception.Error);
        }
    }
}
