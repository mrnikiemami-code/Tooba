using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Domain.Carriers;

namespace Tooba.Shipping.Infrastructure.Carriers.Tipax;

/// <summary>آزمون اتصال و همگام‌سازی مرجع Tipax برای یک حساب مالک.</summary>
internal sealed class TipaxV4AccountClient(IHttpClientFactory httpClientFactory, TipaxV4AccountTokenSource tokens)
{
    public void InvalidateToken(Guid accountId, int configurationVersion)
        => tokens.Invalidate(accountId, configurationVersion);

    public async Task<ShippingCarrierConnectionTestOutcome> TestConnectionAsync(
        ShippingCarrierAccount account,
        ShippingCarrierSecretMaterialPayload secret,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(account.ProviderCode, TipaxV4RateProvider.Code, StringComparison.OrdinalIgnoreCase))
        {
            return ShippingCarrierConnectionTestOutcome.ProviderRejected;
        }

        var token = await tokens.GetAsync(account, secret, cancellationToken);
        return string.IsNullOrWhiteSpace(token)
            ? ShippingCarrierConnectionTestOutcome.Unauthorized
            : ShippingCarrierConnectionTestOutcome.Succeeded;
    }

    public async Task<IReadOnlyList<ShippingCarrierReferenceValue>> SyncReferencesAsync(
        ShippingCarrierAccount account,
        ShippingCarrierSecretMaterialPayload secret,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(account.ProviderCode, TipaxV4RateProvider.Code, StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<ShippingCarrierReferenceValue>();
        }

        var token = await tokens.GetAsync(account, secret, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Array.Empty<ShippingCarrierReferenceValue>();
        }

        var now = DateTimeOffset.UtcNow;
        var result = new List<ShippingCarrierReferenceValue>();
        await AddListAsync(
            account,
            token,
            "api/OM/v4/Pricing/GetServicesList",
            ShippingCarrierReferenceKind.Service,
            result,
            now,
            cancellationToken);
        await AddListAsync(
            account,
            token,
            "api/OM/v4/PaymentType/Get",
            ShippingCarrierReferenceKind.PaymentType,
            result,
            now,
            cancellationToken);
        await AddListAsync(
            account,
            token,
            "api/OM/v4/PackingType/GetPackingTypes",
            ShippingCarrierReferenceKind.Packing,
            result,
            now,
            cancellationToken);
        await AddListAsync(
            account,
            token,
            "api/OM/v4/PackingType/GetPackagingContents",
            ShippingCarrierReferenceKind.PackageContent,
            result,
            now,
            cancellationToken);
        await AddListAsync(
            account,
            token,
            "api/OM/v4/PackingType/GetPackTypes",
            ShippingCarrierReferenceKind.PackType,
            result,
            now,
            cancellationToken);
        return result;
    }

    private async Task AddListAsync(
        ShippingCarrierAccount account,
        string token,
        string relativePath,
        ShippingCarrierReferenceKind kind,
        List<ShippingCarrierReferenceValue> sink,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(account.ApiBaseUrl)
            ? "https://omtestapi.tipax.ir/"
            : account.ApiBaseUrl!;
        var url = new Uri(new Uri(EnsureTrailingSlash(baseUrl)), relativePath);
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            using var response = await httpClientFactory.CreateClient("Shipping.Tipax.Api")
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: deadline.Token);
            foreach (var item in EnumerateObjects(document.RootElement))
            {
                var id = ReadId(item);
                var title = ReadTitle(item) ?? id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                sink.Add(ShippingCarrierReferenceValue.Create(
                    account.AccountId,
                    kind,
                    id,
                    title,
                    isActive: true,
                    now));
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
        }
        catch (JsonException)
        {
        }
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    yield return element;
                }
            }

            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in data.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    yield return element;
                }
            }
        }
    }

    private static string? ReadId(JsonElement element)
    {
        foreach (var name in new[] { "id", "serviceId", "paymentTypeId", "packingId", "packageContentId", "packType" })
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            return value.ValueKind switch
            {
                JsonValueKind.Number => value.TryGetInt64(out var n)
                    ? n.ToString(CultureInfo.InvariantCulture)
                    : null,
                JsonValueKind.String => value.GetString(),
                _ => null
            };
        }

        return null;
    }

    private static string? ReadTitle(JsonElement element)
    {
        foreach (var name in new[] { "title", "name", "serviceTitle", "caption" })
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static string EnsureTrailingSlash(string value)
        => value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
}
