using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Domain.Rates;
using Tooba.Shipping.Infrastructure.Authoring;
using ContractVerdict = Tooba.Shipping.Contracts.Rates.ShippingCapturedQuoteVerdict;
using ContractReasons = Tooba.Shipping.Contracts.Rates.ShippingCapturedQuoteVerdictCodes;
using DomainOutcome = Tooba.Shipping.Domain.Rates.ShippingCapturedQuoteOutcome;

namespace Tooba.Shipping.Infrastructure.Rates;

/// <summary>
/// درز Contracts برای قیمت‌گذاری یک بازبینی <b>capture‌شدهٔ دقیق</b>: هویت مالک/پیش‌نویس/بازبینی،
/// مسیر واقعی Geography و سپس همان موتور قیمت موجود.
/// </summary>
/// <remarks>
/// <para>
/// <b>One authority.</b> این آداپتور هیچ قاعدهٔ قیمتی نمی‌سازد؛ یک‌بار
/// <c>ShippingRatePolicyEngine.QuoteCapturedRevision</c> را صدا می‌زند و نتیجه را صریح به Contracts
/// نگاشت می‌کند. هیچ JSON round-trip، cast بین ماژولی یا کپی lossy در کار نیست.
/// </para>
/// <para>
/// <b>Identity first.</b> اول انطباق گونهٔ مالک با زمینهٔ نصب/تجارت جاری، بعد خواندن <em>دقیق</em>
/// بازبینی با کلید مالک. نه latest، نه current، نه active. بازبینی غایب یا مالک دیگر همان
/// «در دسترس نیست» است.
/// </para>
/// <para>
/// <b>Verified geography only.</b> شناسه‌های مقصد ادعای فراخوان‌اند؛ کشور/ناحیه/شهر/ناحیهٔ خدمت از
/// درز Contracts جغرافیا خوانده و زنجیرهٔ والدها اثبات می‌شود. هیچ شناسهٔ جعلی، والد پذیرفته‌شده و
/// هیچ پوشش ساختگی ساخته نمی‌شود. شهر بدون ناحیهٔ اداری مجاز است.
/// </para>
/// <para>
/// <b>Read-only.</b> هیچ نوشتن/SaveChanges/تراکنش/تغییر snapshot نیست و استثنا یا لغو فراخوان عیناً
/// منتشر می‌شود؛ هیچ شکست بیرونی به «قیمت خالی» تبدیل نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingCapturedRevisionQuoteLookup(
    IShippingAuthoringDraftDirectory drafts,
    IGeographyPlaceSnapshotReader snapshots,
    IShippingCarrierAccountDirectory carrierAccounts,
    ICarrierRateProviderRegistry carrierProviders,
    ICurrentEdition edition,
    ICurrentTenant tenant,
    IModuleCallTracer moduleCallTracer,
    ILogger<ShippingCapturedRevisionQuoteLookup> logger) : IShippingCapturedRevisionQuoteLookup
{
    private const string SourceModule = "Shipping";
    private const string GeographyModule = "Geography";
    private const string CountriesOperation = "GetDestinationCountriesForCapturedQuote";
    private const string AreasOperation = "GetDestinationAdministrativeAreasForCapturedQuote";
    private const string CitiesOperation = "GetDestinationCitiesForCapturedQuote";
    private const string ServiceAreasOperation = "GetDestinationServiceAreasForCapturedQuote";

    /// <inheritdoc />
    public async Task<Result<ShippingCapturedQuoteResult>> QuoteAsync(
        ShippingCapturedQuoteLookupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var owner = ResolveOwnerScope(request);
        if (owner is null)
        {
            return Refused(ContractVerdict.OwnerScopeUnavailable);
        }

        // EXACT identity (owner + draft + revision). No newest/current/active fallback exists here either.
        var captured = await drafts.GetCapturedRevisionAsync(
            owner,
            request.DraftId,
            request.SourceRevision,
            cancellationToken);
        if (!captured.IsSuccess)
        {
            return Refused(ContractVerdict.CapturedRevisionUnavailable);
        }

        var snapshot = captured.Value;
        ShippingAuthoringRevisionPayload payload;
        try
        {
            // The row headers are the authority; a body describing another identity is corrupt, not a read.
            payload = snapshot.ReadPayload();
        }
        catch (SemanticException)
        {
            return Refused(ContractVerdict.CapturedRevisionUnavailable);
        }

        var destination = await ResolveDestinationAsync(request.Destination, cancellationToken);
        if (destination is null)
        {
            return Refused(ContractVerdict.DestinationUnresolved);
        }

        var (path, ancestors) = destination.Value;

        var quote = ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                new ShippingCapturedRevisionIdentity(
                    snapshot.OwnerKind,
                    snapshot.OwnerTenantId,
                    snapshot.OwnerSellerPartyId,
                    snapshot.DraftId,
                    snapshot.SourceRevision),
                payload,
                path,
                request.GroupWeightGrams,
                request.GroupItemCount,
                request.FinalGroupGoodsAmountAfterDiscountAndTax,
                request.CurrencyCode,
                ancestors));

        if (quote.Outcome == DomainOutcome.CarrierProviderRequired)
        {
            return await QuoteCarrierApiAsync(
                request,
                snapshot,
                payload,
                quote,
                cancellationToken);
        }

        return Result.Success(Map(quote, snapshot.SnapshotId));
    }

    private async Task<Result<ShippingCapturedQuoteResult>> QuoteCarrierApiAsync(
        ShippingCapturedQuoteLookupRequest request,
        ShippingAuthoringRevisionSnapshot snapshot,
        ShippingAuthoringRevisionPayload payload,
        ShippingCapturedRevisionQuote coverage,
        CancellationToken cancellationToken)
    {
        if (coverage.Selection is null)
        {
            return Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierConfigurationIncomplete,
                ContractReasons.CarrierConfigurationIncomplete,
                coverage));
        }

        var binding = await ResolveCarrierBindingAsync(snapshot, payload, cancellationToken);
        if (binding is null)
        {
            return Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierConfigurationIncomplete,
                payload.CarrierBinding is null
                    ? ContractReasons.CarrierBindingMissing
                    : ContractReasons.CarrierAccountUnavailable,
                coverage));
        }

        if (!binding.IsActiveAccount)
        {
            return Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierConfigurationIncomplete,
                ContractReasons.CarrierAccountUnavailable,
                coverage));
        }

        if (request.Destination.CityId is not { } cityId
            || request.GroupWeightGrams is not { } weight
            || weight <= 0
            || request.FinalGroupGoodsAmountAfterDiscountAndTax is not { } goods
            || goods < 0m)
        {
            return Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierConfigurationIncomplete,
                ContractReasons.CarrierConfigurationIncomplete,
                coverage));
        }

        var destinationCode = await carrierAccounts.TryResolveDestinationExternalCodeAsync(
            binding.AccountId,
            cityId,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(destinationCode))
        {
            return Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierConfigurationIncomplete,
                ContractReasons.CarrierDestinationUnmapped,
                coverage));
        }

        if (!carrierProviders.TryResolve(binding.ProviderCode, out var provider) || provider is null)
        {
            return Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierConfigurationIncomplete,
                ContractReasons.CarrierModeUnavailable,
                coverage));
        }

        var carrierRequest = new CarrierRateRequest(
            RequestId: $"{snapshot.SnapshotId:N}:{coverage.Selection.GroupId:N}",
            OriginLocationCode: binding.OriginExternalCityCode,
            DestinationLocationCode: destinationCode,
            ServiceCode: binding.ProviderServiceId,
            WeightGrams: weight,
            DeclaredValue: goods,
            LengthCentimeters: binding.DefaultLengthCentimeters,
            WidthCentimeters: binding.DefaultWidthCentimeters,
            HeightCentimeters: binding.DefaultHeightCentimeters,
            PaymentKind: payload.MethodSettings!.FreightPaymentMode == Contracts.Packaging.ShippingFreightPayerMode.CollectOnDelivery
                ? CarrierPaymentKind.CollectOnDelivery
                : CarrierPaymentKind.Prepaid,
            Account: binding.Execution);

        CarrierRateResult carrierResult;
        try
        {
            carrierResult = await provider.QuoteAsync(carrierRequest, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierTemporarilyUnavailable,
                ContractReasons.CarrierTemporarilyUnavailable,
                coverage));
        }

        return carrierResult.Outcome switch
        {
            CarrierRateOutcome.Quoted when carrierResult.Options.FirstOrDefault(o =>
                string.Equals(o.ServiceCode, binding.ProviderServiceId, StringComparison.Ordinal)) is { } exact
                => MapQuotedOrReject(coverage, snapshot.SnapshotId, exact, binding, carrierRequest),
            CarrierRateOutcome.Quoted when carrierResult.Options.Count == 1
                => MapQuotedOrReject(coverage, snapshot.SnapshotId, carrierResult.Options[0], binding, carrierRequest),
            CarrierRateOutcome.Quoted
                => Result.Success(MapCarrierFailure(
                    ContractVerdict.CarrierNoService,
                    ContractReasons.CarrierNoService,
                    coverage)),
            CarrierRateOutcome.Unauthorized
                => Result.Success(MapCarrierFailure(
                    ContractVerdict.CarrierUnauthorized,
                    ContractReasons.CarrierUnauthorized,
                    coverage)),
            CarrierRateOutcome.NoService
                => Result.Success(MapCarrierFailure(
                    ContractVerdict.CarrierNoService,
                    ContractReasons.CarrierNoService,
                    coverage)),
            CarrierRateOutcome.TemporarilyUnavailable
                => Result.Success(MapCarrierFailure(
                    ContractVerdict.CarrierTemporarilyUnavailable,
                    ContractReasons.CarrierTemporarilyUnavailable,
                    coverage)),
            _ => Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierConfigurationIncomplete,
                ContractReasons.CarrierConfigurationIncomplete,
                coverage)),
        };
    }

    private async Task<CarrierBindingExecution?> ResolveCarrierBindingAsync(
        ShippingAuthoringRevisionSnapshot snapshot,
        ShippingAuthoringRevisionPayload payload,
        CancellationToken cancellationToken)
    {
        if (payload.CarrierBinding is { } captured)
        {
            var context = await carrierAccounts.TryResolveExecutionContextAsync(
                snapshot.OwnerKind,
                snapshot.OwnerTenantId,
                snapshot.OwnerSellerPartyId,
                captured.CarrierAccountId,
                cancellationToken);
            if (context is null)
            {
                return null;
            }

            return new CarrierBindingExecution(
                captured.BindingId,
                captured.CarrierAccountId,
                captured.ProviderCode,
                captured.ProviderServiceId,
                captured.OriginExternalCityCode,
                captured.DefaultLengthCentimeters,
                captured.DefaultWidthCentimeters,
                captured.DefaultHeightCentimeters,
                context.IsActive,
                new CarrierAccountExecution(
                    context.AccountId,
                    context.ConfigurationVersion,
                    context.Username,
                    context.Password,
                    context.CurrencyCode,
                    captured.PaymentTypeId,
                    captured.PickupTypeId,
                    captured.DistributionTypeId,
                    captured.PackingId,
                    captured.PackageContentId,
                    captured.PackTypeId,
                    context.ApiBaseUrl,
                    context.TokenUrl));
        }

        return await carrierAccounts.GetMethodBindingForDraftAsync(
            snapshot.DraftId,
            snapshot.OwnerKind,
            snapshot.OwnerTenantId,
            snapshot.OwnerSellerPartyId,
            cancellationToken);
    }

    private static Result<ShippingCapturedQuoteResult> MapQuotedOrReject(
        ShippingCapturedRevisionQuote coverage,
        Guid snapshotId,
        CarrierRateOption option,
        CarrierBindingExecution binding,
        CarrierRateRequest request)
    {
        if (option.FinalAmount < 0m
            || option.TransportationAmount < 0m
            || option.TaxAmount < 0m
            || option.PackagingAmount < 0m
            || option.PickupAmount < 0m
            || option.DeliveryAmount < 0m
            || option.FinalAmount
                != option.TransportationAmount
                    + option.TaxAmount
                    + option.PackagingAmount
                    + option.PickupAmount
                    + option.DeliveryAmount)
        {
            return Result.Success(MapCarrierFailure(
                ContractVerdict.CarrierTemporarilyUnavailable,
                ContractReasons.CarrierQuoteUnavailable,
                coverage));
        }

        var mapped = MapCarrierQuoted(coverage, snapshotId, option, binding, request);
        return Result.Success(mapped);
    }

    private static ShippingCapturedQuoteResult MapCarrierQuoted(
        ShippingCapturedRevisionQuote coverage,
        Guid snapshotId,
        CarrierRateOption option,
        CarrierBindingExecution binding,
        CarrierRateRequest request)
    {
        var packaging = option.PackagingAmount;
        // Final is the authority and already includes tax/packing/pickup/delivery. Freight for
        // checkout is Final minus packaging so tax is never added a second time into packaging.
        var freight = option.FinalAmount - packaging;
        if (freight < 0m)
        {
            freight = option.FinalAmount;
            packaging = 0m;
        }

        var total = freight + packaging;
        var collect = coverage.FreightPayerMode == Contracts.Packaging.ShippingFreightPayerMode.CollectOnDelivery
            ? freight
            : 0m;
        var payable = total - collect;
        var selection = coverage.Selection!;
        var issued = DateTimeOffset.UtcNow;
        var identity = CarrierQuoteInputIdentity.Compute(
            binding.AccountId,
            binding.Execution.ConfigurationVersion,
            option.ProviderCode,
            request.OriginLocationCode,
            request.DestinationLocationCode,
            option.ServiceCode,
            request.WeightGrams,
            request.DeclaredValue,
            request.LengthCentimeters,
            request.WidthCentimeters,
            request.HeightCentimeters,
            request.PaymentKind);
        var quoted = new ShippingCapturedRevisionQuote(
            ShippingCapturedQuoteOutcome.Quoted,
            ShippingCapturedQuoteReasonCodes.Quoted,
            coverage.ServiceCode,
            coverage.OptionCode,
            option.CurrencyCode,
            freight,
            packaging,
            total,
            payable,
            collect,
            coverage.FreightPayerMode,
            selection with { RateRowId = binding.BindingId });
        var mapped = Map(quoted, snapshotId);
        var providerReference = string.IsNullOrWhiteSpace(option.ProviderReference)
            ? null
            : option.ProviderReference.Trim();
        return mapped with
        {
            CarrierQuote = new ShippingCapturedCarrierQuoteEvidence(
                option.ProviderCode,
                binding.AccountId,
                binding.Execution.ConfigurationVersion,
                option.ServiceCode,
                identity,
                option.CurrencyCode,
                option.FinalAmount,
                option.TransportationAmount,
                option.TaxAmount,
                option.PackagingAmount,
                option.PickupAmount,
                option.DeliveryAmount,
                issued,
                issued.Add(CarrierQuoteLocalFreshness.Window),
                request.OriginLocationCode,
                request.DestinationLocationCode,
                request.WeightGrams,
                request.DeclaredValue,
                request.LengthCentimeters,
                request.WidthCentimeters,
                request.HeightCentimeters,
                providerReference)
        };
    }

    private static ShippingCapturedQuoteResult MapCarrierFailure(
        ContractVerdict verdict,
        string reason,
        ShippingCapturedRevisionQuote coverage)
        => new(
            verdict,
            reason,
            coverage.ServiceCode,
            coverage.OptionCode,
            CurrencyCode: null,
            FreightAmount: null,
            PackagingAmount: null,
            TotalExpenseAmount: null,
            PayableAtCheckoutAmount: null,
            CarrierCollectAmount: null,
            Settings: coverage.Selection is null
                ? null
                : new ShippingCapturedQuoteSettings(
                    coverage.Selection.Settings.FreightPaymentMode,
                    coverage.Selection.Settings.CalculationMode),
            SnapshotId: null,
            DraftId: coverage.Selection?.DraftId,
            SourceRevision: coverage.Selection?.SourceRevision,
            FormatVersion: coverage.Selection?.FormatVersion,
            ShippingTypeId: coverage.Selection?.ShippingTypeId,
            GroupId: coverage.Selection?.GroupId,
            RateRowId: coverage.Selection?.RateRowId,
            MinDeliveryDays: null,
            MaxDeliveryDays: null,
            ReceivingWeek: null);

    /// <summary>
    /// گونهٔ مالک درخواستی را با زمینهٔ trusted همین درخواست می‌سنجد و در صورت انطباق، scope مالک
    /// قابل استفاده در خواندن دقیق را می‌سازد. زمینهٔ ناقص/ناسازگار همیشه <c>null</c> است.
    /// </summary>
    /// <param name="request">درخواست Contracts.</param>
    private ShippingAuthoringOwnerScope? ResolveOwnerScope(ShippingCapturedQuoteLookupRequest request)
        => ShippingCapturedOwnerScopeResolver.Resolve(
            request.OwnerKind,
            request.OwnerTenantId,
            request.OwnerSellerPartyId,
            edition,
            tenant);

    /// <summary>
    /// مسیر مقصد را با فکت‌های واقعی Geography اثبات می‌کند: کشور فعال، ناحیهٔ اداری با زنجیرهٔ والد
    /// کامل (ریشه..برگ)، شهر با والد واقعی و ناحیهٔ خدمت با شهر مالک. هر تناقض <c>null</c> است.
    /// </summary>
    /// <param name="requested">شناسه‌های ادعاشدهٔ فراخوان.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    private async Task<(ShippingDestinationScope Path, IReadOnlyList<Guid>? Ancestors)?> ResolveDestinationAsync(
        DestinationIdentifiers requested,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requested);

        if (requested.CountryId == Guid.Empty)
        {
            return null;
        }

        // A service area without its owning city cannot be proven, so it is never resolved by guess.
        if (requested.ServiceAreaId is not null && requested.CityId is null)
        {
            return null;
        }

        var countries = await ReadAsync(
            CountriesOperation,
            [requested.CountryId],
            ids => snapshots.GetCountriesAsync(ids, cancellationToken),
            row => row.CountryId,
            cancellationToken);
        if (countries is null
            || !countries.TryGetValue(requested.CountryId, out var country)
            || !country.IsActive)
        {
            return null;
        }

        CitySnapshot? city = null;
        if (requested.CityId is { } cityId)
        {
            var cities = await ReadAsync(
                CitiesOperation,
                [cityId],
                ids => snapshots.GetCitiesAsync(ids, cancellationToken),
                row => row.CityId,
                cancellationToken);
            if (cities is null
                || !cities.TryGetValue(cityId, out city)
                || !city.IsActive
                || city.CountryId != requested.CountryId)
            {
                return null;
            }
        }

        // The declared administrative area and the city's real parent must be the same identity.
        var declaredArea = requested.AdministrativeAreaId;
        var cityArea = city?.AdministrativeAreaId;
        if (declaredArea is not null && cityArea is not null && declaredArea != cityArea)
        {
            return null;
        }

        if (declaredArea is not null && city is not null && cityArea is null)
        {
            // A city with no real administrative parent cannot be covered by a declared parent.
            return null;
        }

        var leafArea = cityArea ?? declaredArea;
        IReadOnlyList<Guid>? ancestors = null;
        if (leafArea is { } areaId)
        {
            ancestors = await BuildAdministrativeClosureAsync(areaId, requested.CountryId, cancellationToken);
            if (ancestors is null)
            {
                return null;
            }
        }

        if (requested.ServiceAreaId is { } serviceAreaId)
        {
            var serviceAreas = await ReadAsync(
                ServiceAreasOperation,
                [serviceAreaId],
                ids => snapshots.GetServiceAreasAsync(ids, cancellationToken),
                row => row.ServiceAreaId,
                cancellationToken);
            if (serviceAreas is null
                || !serviceAreas.TryGetValue(serviceAreaId, out var serviceArea)
                || !serviceArea.IsActive
                || serviceArea.CityId != requested.CityId)
            {
                return null;
            }
        }

        return (new ShippingDestinationScope(
            requested.CountryId,
            leafArea,
            requested.CityId,
            requested.ServiceAreaId), ancestors);
    }

    /// <summary>
    /// زنجیرهٔ <b>واقعی</b> ناحیهٔ اداری را از برگ تا ریشه با خواندن batch والدها می‌سازد.
    /// </summary>
    /// <remarks>
    /// غیبت، غیرفعال‌بودن، کشور ناسازگار یا تکرار (چرخه) زنجیره را نامعتبر می‌کند. خروجی ریشه..برگ
    /// است تا تطبیق پوشش والد برای شهر عمیق ممکن باشد، بدون هیچ حلقهٔ قیمت‌گذاری روی والدها.
    /// </remarks>
    /// <param name="areaId">برگ زنجیره.</param>
    /// <param name="countryId">کشور مورد انتظار.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    private async Task<IReadOnlyList<Guid>?> BuildAdministrativeClosureAsync(
        Guid areaId,
        Guid countryId,
        CancellationToken cancellationToken)
    {
        var reversed = new List<Guid>();
        var visited = new HashSet<Guid>();
        Guid? current = areaId;

        while (current is { } id)
        {
            if (!visited.Add(id))
            {
                return null;
            }

            var read = await ReadAsync(
                AreasOperation,
                [id],
                ids => snapshots.GetAdministrativeAreasAsync(ids, cancellationToken),
                row => row.AdministrativeAreaId,
                cancellationToken);
            if (read is null
                || !read.TryGetValue(id, out var area)
                || !area.IsActive
                || area.CountryId != countryId)
            {
                return null;
            }

            reversed.Add(area.AdministrativeAreaId);
            current = area.ParentAdministrativeAreaId;
        }

        reversed.Reverse();
        return reversed;
    }

    /// <summary>
    /// یک خواندن واقعی Geography را با span فرزند canonical می‌پوشاند و پاسخ را از نظر هویت می‌سنجد:
    /// شناسهٔ بیرون از مجموعهٔ درخواستی یا تکرار هویت، پاسخ متناقض است و fail-closed می‌شود. استثنا و
    /// لغو فراخوان عیناً منتشر می‌شوند و هرگز به مجموعهٔ خالی تبدیل نمی‌شوند.
    /// </summary>
    /// <param name="operation">برچسب عملیات ثابت.</param>
    /// <param name="ids">شناسه‌های درخواستی.</param>
    /// <param name="call">درز واقعی Geography.</param>
    /// <param name="idOf">استخراج هویت از هر ردیف.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    private async Task<Dictionary<Guid, T>?> ReadAsync<T>(
        string operation,
        IReadOnlyCollection<Guid> ids,
        Func<IReadOnlyCollection<Guid>, Task<IReadOnlyList<T>>> call,
        Func<T, Guid> idOf,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        IReadOnlyList<T> rows;
        using (var moduleCall = moduleCallTracer.Begin(SourceModule, GeographyModule, operation))
        {
            try
            {
                rows = await call(ids);
                moduleCall.SetOk();
            }
            catch (Exception exception)
            {
                // Only the exception TYPE is recorded; the same exception/cancellation is rethrown unchanged.
                moduleCall.SetError(exception);
                throw;
            }
        }

        var requested = new HashSet<Guid>(ids);
        var result = new Dictionary<Guid, T>(rows.Count);
        foreach (var row in rows)
        {
            var id = idOf(row);
            if (!requested.Contains(id) || !result.TryAdd(id, row))
            {
                return null;
            }
        }

        return result;
    }

    /// <summary>
    /// نگاشت صریح Domain → Contracts؛ هیچ نوع Domain به مصرف‌کننده نشت نمی‌کند و هیچ مبلغ غیرقیمت‌دار
    /// با صفر جای‌گزین نمی‌شود.
    /// </summary>
    /// <param name="quote">نتیجهٔ موتور.</param>
    /// <param name="snapshotId">شناسهٔ ردیف capture خوانده‌شده.</param>
    private static ShippingCapturedQuoteResult Map(ShippingCapturedRevisionQuote quote, Guid snapshotId)
    {
        var selection = quote.Selection;
        var week = selection?.WeeklySchedule;

        return new ShippingCapturedQuoteResult(
            MapOutcome(quote.Outcome),
            quote.ReasonCode,
            quote.ServiceCode,
            quote.OptionCode,
            quote.CurrencyCode,
            quote.FreightAmount,
            quote.PackagingAmount,
            quote.TotalExpenseAmount,
            quote.PayableAtCheckoutAmount,
            quote.CarrierCollectAmount,
            selection is null
                ? null
                : new ShippingCapturedQuoteSettings(
                    selection.Settings.FreightPaymentMode,
                    selection.Settings.CalculationMode,
                    selection.Settings.PreparationMinutes,
                    selection.Settings.HandoffMinutes),
            snapshotId,
            selection?.DraftId,
            selection?.SourceRevision,
            selection?.FormatVersion,
            selection?.ShippingTypeId,
            selection?.GroupId,
            selection?.RateRowId,
            selection?.MinDeliveryDays,
            selection?.MaxDeliveryDays,
            week is null
                ? null
                : new ShippingCapturedReceivingWeek(
                    week.TimeZoneId,
                    week.Days
                        .Select(day => new ShippingCapturedReceivingWeekDay(
                            day.Weekday,
                            day.IsWorking,
                            day.StartTime,
                            day.EndTime))
                        .ToList(),
                    week.SlotDurationMinutes),
            CarrierWeek: selection?.CarrierSchedule is { } carrier
                ? new ShippingCapturedCarrierWeek(
                    carrier.TimeZoneId,
                    carrier.Days
                        .Select(day => new ShippingCapturedCarrierWeekDay(
                            day.Weekday,
                            day.IsWorking,
                            day.OpensAt,
                            day.ClosesAt))
                        .ToList())
                : null);
    }

    private static ContractVerdict MapOutcome(DomainOutcome outcome)
        => outcome switch
        {
            DomainOutcome.Quoted => ContractVerdict.Quoted,
            DomainOutcome.IdentityMismatch =>
                ContractVerdict.CapturedRevisionUnavailable,
            DomainOutcome.DestinationPathInvalid =>
                ContractVerdict.DestinationUnresolved,
            DomainOutcome.NotConfigured => ContractVerdict.NotConfigured,
            DomainOutcome.CalculationModeUnavailable =>
                ContractVerdict.CalculationModeUnavailable,
            DomainOutcome.CoverageMissing => ContractVerdict.CoverageMissing,
            DomainOutcome.GroupAmbiguous => ContractVerdict.GroupAmbiguous,
            DomainOutcome.CurrencyMissing => ContractVerdict.CurrencyMissing,
            DomainOutcome.CurrencyMismatch => ContractVerdict.CurrencyMismatch,
            DomainOutcome.RateRowMissing => ContractVerdict.RateRowMissing,
            DomainOutcome.MeasurementRequired =>
                ContractVerdict.MeasurementRequired,
            DomainOutcome.ConditionNotMatched =>
                ContractVerdict.ConditionNotMatched,
            DomainOutcome.ConditionAmbiguous =>
                ContractVerdict.ConditionAmbiguous,
            DomainOutcome.CarrierProviderRequired =>
                ContractVerdict.CarrierConfigurationIncomplete,
            _ => ContractVerdict.ConfigurationInvalid,
        };

    /// <summary>
    /// یک امتناع <b>تعریف‌شده</b> می‌سازد: هیچ مبلغی، هیچ ادعای provenance، و هیچ استثنایی برای
    /// فراخوان. استثنا/لغو درزهای پایین‌دست هرگز به این مسیر تبدیل نمی‌شوند.
    /// </summary>
    /// <param name="outcome">دلیل امتناع.</param>
    private Result<ShippingCapturedQuoteResult> Refused(ContractVerdict outcome)
    {
        var reason = outcome == ContractVerdict.OwnerScopeUnavailable
            ? ContractReasons.OwnerScopeUnavailable
            : ContractReasons.CapturedRevisionUnavailable;

        logger.LogDebug("Captured quote refused with {Outcome}.", outcome);
        return Result.Success(new ShippingCapturedQuoteResult(
            outcome,
            reason,
            ServiceCode: null,
            OptionCode: null,
            CurrencyCode: null,
            FreightAmount: null,
            PackagingAmount: null,
            TotalExpenseAmount: null,
            PayableAtCheckoutAmount: null,
            CarrierCollectAmount: null,
            Settings: null,
            SnapshotId: null,
            DraftId: null,
            SourceRevision: null,
            FormatVersion: null,
            ShippingTypeId: null,
            GroupId: null,
            RateRowId: null,
            MinDeliveryDays: null,
            MaxDeliveryDays: null,
            ReceivingWeek: null));
    }
}
