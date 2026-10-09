namespace Tooba.Support.Contracts.Errors;

/// <summary>
/// کدهای خطای معنایی پایدارِ مالکیت‌شدهٔ ماژول Support برای پیامدهای HTTP/مورداستفاده و ناورداهای دامنه.
/// <para>
/// هویت، خودِ کد است — نه متن پیام و نه متن بومی‌شده. این مقادیر همان کدهای ماشینی‌ای هستند که
/// دامنه/دایرکتوری Support صادر می‌کنند و کاتالوگ خطای ترکیب‌شده آن‌ها را نگاشت می‌کند؛ بنابراین
/// هرگز نباید تغییر نام یابند یا بازاستفاده شوند.
/// </para>
/// <para>
/// این کلاس تنها خانهٔ canonical هویت کدهای Support است (آینهٔ
/// <c>PromotionErrorCodes</c>/<c>ReturnsErrorCodes</c>/<c>SettlementErrorCodes</c>). مالکیت یکتاست:
/// فضای‌نام <c>support.*</c> در اختیار Support است و توسط <c>SupportErrorResourceSet</c> بومی‌سازی
/// می‌شود. Support هرگز توصیف‌گر کد متعلق به ماژول دیگر را دوباره ثبت نمی‌کند — کدهای عرضی مشترک
/// <c>customer.session.required</c>، <c>seller.authorization.denied</c> و
/// <c>admin.authorization.denied</c> مالکیت توصیف‌گرشان در <c>FoundationErrorCatalogContributor</c>
/// است و عمداً در این‌جا اعلام نمی‌شوند — و هیچ ماژول دیگری کد Support را ثبت نمی‌کند.
/// </para>
/// <para>
/// سطح خطاها بر پایهٔ «دسترس‌پذیری» تفکیک شده تا دروازه‌های AMSC صادق بمانند:
/// <list type="bullet">
/// <item><see cref="IsHttpReachable"/> — کدهایی که کلاینت HTTP می‌تواند ببیند؛ هرکدام دقیقاً یک
/// توصیف‌گر در <c>SupportErrorCatalogContributor</c> دارد.</item>
/// <item><see cref="IsDomainInvariant"/> — کدهایی که تجمیع‌های دامنه و دایرکتوری زیرساخت به‌صورت
/// خطای نوع‌دار پرتاب می‌کنند. آن‌ها هویتِ <c>Result</c>-محور یک ناوردای رد‌شده‌اند و بومی‌سازی
/// می‌شوند، اما هرگز توصیف‌گر HTTP اختصاصی نمی‌گیرند: مورد استفادهٔ کاربردی آن‌ها را روی کد
/// نتیجهٔ عمومی پایدار همان عملیات نگاشت می‌کند، پس هیچ کد قابل‌مشاهده‌ای برای کلاینت اضافه
/// نمی‌شود و شکل پاسخ موجود دقیقاً حفظ می‌گردد.</item>
/// </list>
/// <see cref="IsKnown"/> اجتماع این دو مجموعه است و همان چیزی است که <c>SupportOperation</c> بر آن
/// فیلتر می‌کند؛ پس خطای Support به <c>Result</c> نگاشت می‌شود، در حالی که کد متعلق به ماژول دیگر
/// (یا خطای نامنتظر) دست‌نخورده به مرز استثنای سراسری canonical می‌رسد. تشخیص فقط بر پایهٔ کد
/// نوع‌دار است و هرگز بر پایهٔ متن پیام.
/// </para>
/// </summary>
public static class SupportErrorCodes
{
    private static readonly HashSet<string> HttpReachableCodes = new(StringComparer.Ordinal)
    {
        Missing,
        Rejected,
        ReplyRejected,
        ActionRejected,
        PatchRejected,
        AuthorizationUnavailable,
        DemoNotReady,
    };

    private static readonly HashSet<string> DomainInvariantCodes = new(StringComparer.Ordinal)
    {
        // ناورداهای تجمیع تیکت.
        TicketIdRequired,
        RequesterRequired,
        SellerPartyRequired,
        SubjectInvalid,
        CategoryInvalid,
        PriorityInvalid,
        StatusInvalid,
        RequesterKindInvalid,
        RelatedIdWithoutType,
        RelatedTypeInvalid,
        RelatedIdRequired,
        RelatedOrderIdInvalid,
        IdempotencyKeyInvalid,

        // ناورداهای تجمیع پیام.
        MessageIdsRequired,
        MessageBodyInvalid,
        MessageInternalAdminOnly,

        // خطاهای دایرکتوری/چرخهٔ عمر.
        TicketNotFound,
        ReplyClosed,
        CloseNotAllowed,
        ReopenNotAllowed,

        // خطای سمت پلتفرم (ترجمهٔ Outbox).
        OutboxEmitNotSupported,
    };

    /// <summary>دقیقاً همان مجموعهٔ کدهایی که کلاینت HTTP می‌تواند ببیند و هرکدام یک‌بار کاتالوگ می‌شوند.</summary>
    public static IReadOnlyCollection<string> HttpReachable => HttpReachableCodes;

    /// <summary>
    /// دقیقاً همان مجموعهٔ کدهای ناوردای دامنه/دایرکتوری که خطای نوع‌دار Support را می‌سازند و
    /// به‌عنوان کد نتیجهٔ عمومی نگاشت می‌شوند، بدون توصیف‌گر HTTP اختصاصی.
    /// </summary>
    public static IReadOnlyCollection<string> DomainInvariants => DomainInvariantCodes;

    /// <summary>
    /// درست است وقتی <paramref name="code"/> یک کد پایدار باشد که این ماژول می‌تواند آن را به‌عنوان
    /// خطای کاربردی آشکار کند. این متد مبنای فیلتر <c>SupportOperation</c> است تا خطاهای Support به
    /// <c>Result</c> نگاشت شوند و کدهای متعلق به ماژول دیگر (یا خطای نامنتظر) دست‌نخورده به مرز
    /// استثنای سراسری برسند. تشخیص فقط بر پایهٔ کد نوع‌دار است، نه متن پیام.
    /// </summary>
    /// <param name="code">کد ماشینی نامزد.</param>
    /// <returns>درست وقتی کد متعلق به سطح خطاهای شناخته‌شدهٔ Support باشد.</returns>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && (HttpReachableCodes.Contains(code) || DomainInvariantCodes.Contains(code));

    /// <summary>درست است وقتی کد یک خطای نتیجهٔ قابل‌مشاهده برای کلاینت و کاتالوگ‌شده باشد.</summary>
    /// <param name="code">کد ماشینی نامزد.</param>
    /// <returns>درست وقتی کد در مجموعهٔ <see cref="HttpReachable"/> باشد.</returns>
    public static bool IsHttpReachable(string? code) =>
        !string.IsNullOrWhiteSpace(code) && HttpReachableCodes.Contains(code);

    /// <summary>درست است وقتی کد یک ناوردای دامنه/دایرکتوری بدون توصیف‌گر HTTP اختصاصی باشد.</summary>
    /// <param name="code">کد ماشینی نامزد.</param>
    /// <returns>درست وقتی کد در مجموعهٔ <see cref="DomainInvariants"/> باشد.</returns>
    public static bool IsDomainInvariant(string? code) =>
        !string.IsNullOrWhiteSpace(code) && DomainInvariantCodes.Contains(code);

    // --- پیامدهای HTTP قابل‌مشاهده برای کلاینت (هرکدام یک توصیف‌گر کاتالوگ) ---------------------

    /// <summary>تیکت در scope مخاطب پیدا نشد (404).</summary>
    public const string Missing = "support.missing";

    /// <summary>ردیابی ایجاد/فهرست/عمومی Support (400).</summary>
    public const string Rejected = "support.rejected";

    /// <summary>ردیابی پاسخ تیکت (400).</summary>
    public const string ReplyRejected = "support.reply.rejected";

    /// <summary>ردیابی عملیات بستن/بازگشایی تیکت (400).</summary>
    public const string ActionRejected = "support.action.rejected";

    /// <summary>ردیابی پچ مدیر روی تیکت (400).</summary>
    public const string PatchRejected = "support.patch.rejected";

    /// <summary>
    /// سرویس مجوز در دسترس نیست؛ مسیر admin پشتیبانی باید fail-closed بماند (503).
    /// سطح کد canonical برای آداپتور Host همان
    /// <c>Tooba.Support.Endpoints.Admin.SupportAdminAuthorizationCodes.AuthorizationUnavailable</c>
    /// است که کنار <c>ISupportAdminAuthorizer</c> قرار دارد تا Host هرگز به Application ارجاع ندهد.
    /// </summary>
    public const string AuthorizationUnavailable = "support.authorization.unavailable";

    /// <summary>دانهٔ توسعهٔ demo آماده نیست (503).</summary>
    public const string DemoNotReady = "support.demo.not_ready";

    // --- ناورداهای دامنه/دایرکتوری (خطای نوع‌دار؛ بدون توصیف‌گر HTTP اختصاصی) -------------------

    /// <summary>شناسهٔ تیکت الزامی است.</summary>
    public const string TicketIdRequired = "support.ticket.id_required";

    /// <summary>درخواست‌کننده الزامی است.</summary>
    public const string RequesterRequired = "support.requester_required";

    /// <summary>طرف فروشنده برای تیکت فروشنده الزامی است.</summary>
    public const string SellerPartyRequired = "support.seller_party_required";

    /// <summary>موضوع تیکت نامعتبر است (خالی یا بیش از حد مجاز).</summary>
    public const string SubjectInvalid = "support.subject_invalid";

    /// <summary>دستهٔ تیکت نامعتبر است.</summary>
    public const string CategoryInvalid = "support.category_invalid";

    /// <summary>اولویت تیکت نامعتبر است.</summary>
    public const string PriorityInvalid = "support.priority_invalid";

    /// <summary>وضعیت تیکت نامعتبر است.</summary>
    public const string StatusInvalid = "support.status_invalid";

    /// <summary>نوع درخواست‌کننده نامعتبر است.</summary>
    public const string RequesterKindInvalid = "support.requester_kind_invalid";

    /// <summary>شناسهٔ موجودیت مرتبط بدون نوع آن ارسال شده است.</summary>
    public const string RelatedIdWithoutType = "support.related_id_without_type";

    /// <summary>نوع موجودیت مرتبط نامعتبر است.</summary>
    public const string RelatedTypeInvalid = "support.related_type_invalid";

    /// <summary>شناسهٔ موجودیت مرتبط الزامی است.</summary>
    public const string RelatedIdRequired = "support.related_id_required";

    /// <summary>شناسهٔ سفارش مرتبط نامعتبر است (اعتبارسنجی soft بدون JOIN).</summary>
    public const string RelatedOrderIdInvalid = "support.related_order_id_invalid";

    /// <summary>کلید idempotency نامعتبر است (بیش از طول مجاز).</summary>
    public const string IdempotencyKeyInvalid = "support.idempotency_key_invalid";

    /// <summary>شناسه‌های پیام الزامی‌اند.</summary>
    public const string MessageIdsRequired = "support.message.ids_required";

    /// <summary>بدنهٔ پیام نامعتبر است (خالی یا بیش از حد مجاز).</summary>
    public const string MessageBodyInvalid = "support.message.body_invalid";

    /// <summary>یادداشت داخلی فقط برای مدیر مجاز است.</summary>
    public const string MessageInternalAdminOnly = "support.message.internal_admin_only";

    /// <summary>تیکت در scope مورد نظر یافت نشد (خطای دایرکتوری).</summary>
    public const string TicketNotFound = "support.ticket_not_found";

    /// <summary>پاسخ روی تیکت بسته‌شده مجاز نیست.</summary>
    public const string ReplyClosed = "support.reply_closed";

    /// <summary>بستن تیکت در وضعیت فعلی مجاز نیست.</summary>
    public const string CloseNotAllowed = "support.close_not_allowed";

    /// <summary>بازگشایی تیکت در وضعیت فعلی مجاز نیست.</summary>
    public const string ReopenNotAllowed = "support.reopen_not_allowed";

    /// <summary>انتشار رویداد بیرونی از Outbox پشتیبانی پشتیبانی نمی‌شود (خطای سمت پلتفرم).</summary>
    public const string OutboxEmitNotSupported = "support.outbox.emit_not_supported";
}
