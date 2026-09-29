namespace Tooba.Party.Application.Seller;

/// <summary>
/// کدهای پایدار خطای سرور مالک Party برای مسیرهای تنظیمات فروشنده.
/// <para>
/// سه کد قرارداد پنل فروشنده را با پارامتر قبلی Host حفظ می‌کنند:
/// <c>seller.settings.missing</c> (۴۰۴)، <c>seller.settings.rejected</c> (۴۰۰) و
/// <c>seller.authorization.denied</c> (۴۰۳، کد عرضی مالک Foundation). هیچ کد دومی برای این
/// معنای مشترک ثبت نمی‌شود.
/// </para>
/// </summary>
public static class PartySellerSettingsErrorCodes
{
    /// <summary>Organization فروشنده در پروفایل عملیاتی پیدا نشد.</summary>
    public const string Missing = "seller.settings.missing";

    /// <summary>به‌روزرسانی پروفایل سازمانی رد شد (Person یا ورودی نامعتبر کسب‌وکار).</summary>
    public const string Rejected = "seller.settings.rejected";
}

/// <summary>
/// کدهای پایدار خطای انتقالی FluentValidation برای درخواست‌های تنظیمات فروشنده.
/// متن محلی‌سازی‌شده نیست؛ فقط شکل ورودی را توصیف می‌کند.
/// </summary>
public static class PartySellerSettingsValidationCodes
{
    /// <summary>نام نمایشی نباید خالی باشد.</summary>
    public const string DisplayNameRequired = "seller.settings.validation.display_name_required";

    /// <summary>نام نمایشی نباید از مرز ذخیره‌سازی بیشتر باشد.</summary>
    public const string DisplayNameLength = "seller.settings.validation.display_name_length";

    /// <summary>نام حقوقی عرضه‌شده نباید خالی باشد.</summary>
    public const string LegalNameShape = "seller.settings.validation.legal_name_shape";

    /// <summary>توضیح عرضه‌شده نباید خالی باشد.</summary>
    public const string DescriptionShape = "seller.settings.validation.description_shape";

    /// <summary>تلفن پشتیبانی عرضه‌شده نباید خالی باشد.</summary>
    public const string SupportPhoneShape = "seller.settings.validation.support_phone_shape";

    /// <summary>ایمیل پشتیبانی عرضه‌شده نباید خالی باشد.</summary>
    public const string SupportEmailShape = "seller.settings.validation.support_email_shape";

    /// <summary>نشانی عرضه‌شده نباید خالی باشد.</summary>
    public const string AddressLineShape = "seller.settings.validation.address_line_shape";
}
