using System.Linq.Expressions;
using FluentValidation;

namespace Tooba.AddressBook.Application.Validators;

/// <summary>کدهای پایدار خطای شکل انتقال دفترچهٔ آدرس.</summary>
public static class AddressBookValidationCodes
{
    /// <summary>شناسهٔ نشانی مسیر الزامی/معتبر است.</summary>
    public const string AddressIdRequired = "customer.address.id_required";

    /// <summary>نام گیرنده الزامی است (شکل انتقال، نه قواعد کسب‌وکار).</summary>
    public const string RecipientNameRequired = "customer.address.recipient_name_required";

    /// <summary>شمارهٔ تماس الزامی است (شکل انتقال، نه قواعد کسب‌وکار).</summary>
    public const string ContactMobileRequired = "customer.address.contact_mobile_required";

    /// <summary>شهر الزامی است (شکل انتقال، نه قواعد کسب‌وکار).</summary>
    public const string CityNameRequired = "customer.address.city_required";

    /// <summary>کدپستی الزامی است (شکل انتقال، نه قواعد کسب‌وکار).</summary>
    public const string PostalCodeRequired = "customer.address.postal_code_required";

    /// <summary>نشانی پستی الزامی است (شکل انتقال، نه قواعد کسب‌وکار).</summary>
    public const string PostalAddressRequired = "customer.address.postal_address_required";
}

/// <summary>
/// قواعد شکل انتقال مشترک فرمان‌های دفترچهٔ آدرس. فقط حضور/شکل اولیهٔ ورودی نامطمئن بررسی می‌شود؛
/// طول، قالب و همهٔ قواعد کسب‌وکار در <c>CustomerAddress</c>/<c>AddressBookDirectory</c> می‌مانند
/// و <c>ActorUserId</c> هرگز به‌عنوان payload اعتبارسنجی نمی‌شود.
/// </summary>
public static class AddressBookFluentRules
{
    /// <summary>متن الزامی: باید پس از trim غیرخالی باشد.</summary>
    public static void RequireNonBlank<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string>> selector,
        string errorCode)
        => validator.RuleFor(selector)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithErrorCode(errorCode);

    /// <summary>شناسهٔ الزامی: نباید Guid.Empty باشد.</summary>
    public static void RequireId<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid>> selector,
        string errorCode)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(errorCode);
}
