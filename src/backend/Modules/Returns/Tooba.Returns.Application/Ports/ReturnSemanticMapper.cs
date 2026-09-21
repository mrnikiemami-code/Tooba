using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Contracts;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Ports;

/// <summary>نگاشت مقصد و خطاهای شناخته‌شده مرجوعی به SemanticError مرکزی.</summary>
public static class ReturnSemanticMapper
{
    /// <summary>Parse مقصد بازپرداخت؛ نامعتبر → SemanticError.</summary>
    public static Result<RefundDestination> ParseDestination(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success(RefundDestination.OriginalPayment);
        }

        return Enum.TryParse<RefundDestination>(value, ignoreCase: true, out var parsed)
            ? Result.Success(parsed)
            : Result.Failure<RefundDestination>(new SemanticError(ReturnsErrorCodes.RefundDestinationInvalid));
    }

    /// <summary>InvalidOperationException شناخته‌شده را به SemanticError نگاشت می‌کند (بدون ex.Message خام).</summary>
    public static SemanticError MapException(InvalidOperationException ex)
    {
        var msg = ex.Message?.Trim() ?? string.Empty;
        if (msg.StartsWith("return.", StringComparison.Ordinal) || msg.StartsWith("refund.", StringComparison.Ordinal)
            || msg.StartsWith("returns.", StringComparison.Ordinal))
        {
            return new SemanticError(msg.StartsWith("returns.", StringComparison.Ordinal)
                ? msg.Replace("returns.", "return.", StringComparison.Ordinal)
                : msg);
        }

        return msg switch
        {
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.WindowExpired)
                => new SemanticError(ReturnsErrorCodes.Expired),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NonReturnable)
                => new SemanticError(ReturnsErrorCodes.NonReturnable),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NothingReturnable)
                => new SemanticError(ReturnsErrorCodes.QuantityExceeded),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NotDelivered)
                => new SemanticError(ReturnsErrorCodes.NotDelivered),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.NotPaid)
                => new SemanticError(ReturnsErrorCodes.NotPaid),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.OrderMissing)
                => new SemanticError(ReturnsErrorCodes.Missing),
            var m when m == ReturnEligibilityReasonCodes.ToFaMessage(ReturnEligibilityReasonCodes.FulfillmentMissing)
                => new SemanticError(ReturnsErrorCodes.FulfillmentMissing),
            "تعداد مرجوعی از باقیماندهٔ تحویل‌شده بیشتر است." => new SemanticError(ReturnsErrorCodes.QuantityExceeded),
            "تعداد مرجوعی باید مثبت باشد." => new SemanticError(ReturnsErrorCodes.QuantityInvalid),
            "انتقال وضعیت از این حالت مجاز نیست." => new SemanticError(ReturnsErrorCodes.Stale),
            "retry فقط برای RefundFailed مجاز است." => new SemanticError(ReturnsErrorCodes.RefundRetryInvalidState),
            "refund فقط برای پرداخت Succeeded مجاز است." => new SemanticError(ReturnsErrorCodes.RefundAlreadyStarted),
            "پرداخت موفق برای refund پیدا نشد." => new SemanticError(ReturnsErrorCodes.RefundPaymentMissing),
            "پرداخت مرجع پیدا نشد." => new SemanticError(ReturnsErrorCodes.RefundPaymentMissing),
            "درخواست مرجوعی پیدا نشد." => new SemanticError(ReturnsErrorCodes.Missing),
            "خط سفارش پیدا نشد." => new SemanticError(ReturnsErrorCodes.LineMissing),
            "درخواست‌دهنده مالک سفارش نیست." => new SemanticError(ReturnsErrorCodes.NotOwner),
            "کلید idempotency الزامی است." => new SemanticError(ReturnsErrorCodes.IdempotencyRequired),
            _ => new SemanticError(ReturnsErrorCodes.Rejected),
        };
    }
}
