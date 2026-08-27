using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application;

/// <summary>
/// تعریف پایدار یک Permission کاتالوگ (نه نام endpoint).
/// </summary>
public sealed record PermissionDefinition(
    string PermissionId,
    string Module,
    string DisplayNameKey,
    string DescriptionKey,
    bool Delegable,
    IReadOnlyList<AccessScopeKind> ScopeKinds);

/// <summary>
/// کاتالوگ معنایی ثابت مجوزهای Access Control.
/// </summary>
public static class PermissionCatalog
{
    private static readonly AccessScopeKind[] GlobalOnly = [AccessScopeKind.GlobalWithinOwner];

    private static readonly AccessScopeKind[] GlobalAndCategory =
    [
        AccessScopeKind.GlobalWithinOwner,
        AccessScopeKind.Category,
    ];

    private static readonly AccessScopeKind[] GlobalProductBrand =
    [
        AccessScopeKind.GlobalWithinOwner,
        AccessScopeKind.Product,
        AccessScopeKind.Brand,
        AccessScopeKind.Category,
    ];

    /// <summary>فهرست کامل canonical.</summary>
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        Def("admin.dashboard.view", "Admin", delegable: false),
        Def("product.view", "Product", delegable: true, GlobalProductBrand),
        Def("product.create", "Product", delegable: true),
        Def("product.edit", "Product", delegable: true, GlobalProductBrand),
        Def("product.publish", "Product", delegable: true, GlobalProductBrand),
        Def("order.view", "Order", delegable: true, GlobalAndCategory),
        Def("order.detail", "Order", delegable: true, GlobalAndCategory),
        Def("order.handle", "Order", delegable: true, GlobalAndCategory),
        Def("order.fulfill", "Order", delegable: true, GlobalAndCategory),
        Def("order.cancel", "Order", delegable: true, GlobalAndCategory),
        Def("order.refund", "Order", delegable: true, GlobalAndCategory),
        Def("order.export", "Order", delegable: true),
        Def("seller.view", "Seller", delegable: false),
        Def("seller.approve", "Seller", delegable: false),
        Def("payment.view", "Payment", delegable: false),
        Def("payment.reconcile", "Payment", delegable: false),
        Def("promotion.view", "Promotion", delegable: true),
        Def("promotion.manage", "Promotion", delegable: true),
        Def("review.view", "Review", delegable: true),
        Def("review.moderate", "Review", delegable: false),
        Def("story.view", "Story", delegable: true),
        Def("story.create", "Story", delegable: true),
        Def("story.edit", "Story", delegable: true),
        Def("story.submit", "Story", delegable: true),
        Def("story.approve", "Story", delegable: false),
        Def("story.reject", "Story", delegable: false),
        Def("story.publish", "Story", delegable: false),
        Def("content.view", "Content", delegable: false),
        Def("content.create", "Content", delegable: false),
        Def("content.edit", "Content", delegable: false),
        Def("content.publish", "Content", delegable: false),
        Def("pagecomposition.view", "PageComposition", delegable: false),
        Def("pagecomposition.manage", "PageComposition", delegable: false),
        Def("fulfillment.view", "Fulfillment", delegable: true),
        Def("fulfillment.manage", "Fulfillment", delegable: true),
        Def("return.view", "Return", delegable: true),
        Def("return.manage", "Return", delegable: true),
        Def("refund.view", "Refund", delegable: true),
        Def("refund.manage", "Refund", delegable: false),
        Def("settlement.view", "Settlement", delegable: true),
        Def("settlement.manage", "Settlement", delegable: false),
        Def("accesscontrol.view", "AccessControl", delegable: true),
        Def("accesscontrol.manage", "AccessControl", delegable: true),
        Def("support.view", "Support", delegable: true),
        Def("support.create", "Support", delegable: true),
        Def("support.reply", "Support", delegable: true),
        Def("support.manage", "Support", delegable: true),
        Def("wallet.view", "Wallet", delegable: false),
        Def("wallet.adjust", "Wallet", delegable: false),
        Def("giftcard.view", "Wallet", delegable: false),
        Def("giftcard.manage", "Wallet", delegable: false),
    ];

    private static readonly Dictionary<string, PermissionDefinition> ById =
        All.ToDictionary(x => x.PermissionId, StringComparer.Ordinal);

    /// <summary>مجوز را با شناسه برمی‌گرداند یا null.</summary>
    public static PermissionDefinition? Find(string permissionId) =>
        ById.TryGetValue(permissionId, out var def) ? def : null;

    /// <summary>وجود canonical را الزام می‌کند.</summary>
    public static PermissionDefinition Require(string permissionId) =>
        Find(permissionId) ?? throw new InvalidOperationException($"مجوز ناشناخته: {permissionId}");

    /// <summary>آیا مجوز قابل تفویض به فروشنده است.</summary>
    public static bool IsDelegable(string permissionId) =>
        Find(permissionId)?.Delegable == true;

    private static PermissionDefinition Def(
        string id,
        string module,
        bool delegable,
        IReadOnlyList<AccessScopeKind>? scopes = null) =>
        new(
            id,
            module,
            $"perm.{id}.name",
            $"perm.{id}.desc",
            delegable,
            scopes ?? GlobalOnly);
}
