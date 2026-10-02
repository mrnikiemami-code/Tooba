namespace Tooba.AccessControl.Contracts.Enums;

/// <summary>محدودهٔ مالک نقش: پلتفرم یا فروشنده.</summary>
public enum AccessOwnerScopeKind
{
    /// <summary>نقش‌های پلتفرم / Admin.</summary>
    Platform = 1,

    /// <summary>نقش‌های یک فروشنده.</summary>
    Seller = 2,
}