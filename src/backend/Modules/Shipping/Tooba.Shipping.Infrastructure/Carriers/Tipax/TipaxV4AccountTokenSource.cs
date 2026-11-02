using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Domain.Carriers;

namespace Tooba.Shipping.Infrastructure.Carriers.Tipax;

/// <summary>
/// کش توکن Tipax به‌ازای accountId + configurationVersion؛ چرخش/غیرفعال‌سازی نسخهٔ قبلی را بی‌اعتبار می‌کند.
/// </summary>
internal sealed class TipaxV4AccountTokenSource(IHttpClientFactory httpClientFactory)
{
    private readonly ConcurrentDictionary<string, TokenState> _cache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);

    public async Task<string?> GetAsync(
        ShippingCarrierAccount account,
        ShippingCarrierSecretMaterialPayload secret,
        CancellationToken cancellationToken)
        => await GetAsync(
            account.AccountId,
            account.ConfigurationVersion,
            account.ApiBaseUrl,
            account.TokenUrl,
            secret.Username,
            secret.Password,
            cancellationToken);

    public async Task<string?> GetAsync(
        Guid accountId,
        int configurationVersion,
        string? apiBaseUrl,
        string? tokenUrl,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var key = CacheKey(accountId, configurationVersion);
        if (_cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return cached.AccessToken;
        }

        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out cached) && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return cached.AccessToken;
            }

            var refreshed = cached?.RefreshToken is { Length: > 0 }
                ? await TryRefreshAsync(apiBaseUrl, cached, cancellationToken)
                : null;
            var token = refreshed ?? await LoginAsync(tokenUrl, username, password, cancellationToken);
            if (token is not null)
            {
                _cache[key] = token;
            }
            else
            {
                _cache.TryRemove(key, out _);
            }

            return token?.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Invalidate(Guid accountId, int configurationVersion)
        => _cache.TryRemove(CacheKey(accountId, configurationVersion), out _);

    private async Task<TokenState?> LoginAsync(
        string? tokenUrl,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var url = string.IsNullOrWhiteSpace(tokenUrl)
            ? "https://igtgatewayapi.tipax.ir/Token"
            : tokenUrl!;
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password,
                ["grant_type"] = "password"
            })
        };
        return await SendTokenRequestAsync(request, cancellationToken);
    }

    private async Task<TokenState?> TryRefreshAsync(
        string? apiBaseUrl,
        TokenState current,
        CancellationToken cancellationToken)
    {
        var baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? "https://omtestapi.tipax.ir/" : apiBaseUrl!;
        var url = new Uri(new Uri(EnsureTrailingSlash(baseUrl)), "api/OM/v4/Account/RefreshToken");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                token = current.AccessToken,
                refreshToken = current.RefreshToken
            })
        };
        return await SendTokenRequestAsync(request, cancellationToken);
    }

    private async Task<TokenState?> SendTokenRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            using var response = await httpClientFactory.CreateClient("Shipping.Tipax.Auth")
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: deadline.Token);
            var tokenNode = document.RootElement;
            if (tokenNode.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                tokenNode = data;
            }

            var accessToken = ReadString(tokenNode, "access_token");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var refreshToken = ReadString(tokenNode, "refresh_token");
            var expiresIn = tokenNode.TryGetProperty("expires_in", out var expires)
                            && expires.TryGetInt32(out var seconds)
                ? Math.Max(seconds, 60)
                : 3600;
            return new TokenState(accessToken, refreshToken, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string CacheKey(Guid accountId, int configurationVersion)
        => accountId.ToString("N") + ":" + configurationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string EnsureTrailingSlash(string value)
        => value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";

    private sealed record TokenState(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt);
}
