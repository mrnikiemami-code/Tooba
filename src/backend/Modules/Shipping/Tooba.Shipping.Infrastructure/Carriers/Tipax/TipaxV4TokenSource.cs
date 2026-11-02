using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Tooba.Shipping.Infrastructure.Carriers.Tipax;

internal sealed class TipaxV4TokenSource
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<TipaxV4Options> _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TokenState? _current;

    public TipaxV4TokenSource(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<TipaxV4Options> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<string?> GetAsync(CancellationToken cancellationToken)
    {
        var cached = Volatile.Read(ref _current);
        if (cached is not null && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return cached.AccessToken;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            cached = _current;
            if (cached is not null && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return cached.AccessToken;
            }

            var options = _options.CurrentValue;
            var refreshed = cached?.RefreshToken is { Length: > 0 }
                ? await TryRefreshAsync(options, cached, cancellationToken)
                : null;
            var token = refreshed ?? await LoginAsync(options, cancellationToken);
            Volatile.Write(ref _current, token);
            return token?.AccessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() => Volatile.Write(ref _current, null);

    private async Task<TokenState?> LoginAsync(
        TipaxV4Options options,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = options.Username,
                ["password"] = options.Password,
                ["grant_type"] = "password"
            })
        };

        return await SendTokenRequestAsync(request, options.Timeout, cancellationToken);
    }

    private async Task<TokenState?> TryRefreshAsync(
        TipaxV4Options options,
        TokenState current,
        CancellationToken cancellationToken)
    {
        var url = new Uri(new Uri(EnsureTrailingSlash(options.ApiBaseUrl)), "api/OM/v4/Account/RefreshToken");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                token = current.AccessToken,
                refreshToken = current.RefreshToken
            })
        };

        return await SendTokenRequestAsync(request, options.Timeout, cancellationToken);
    }

    private async Task<TokenState?> SendTokenRequestAsync(
        HttpRequestMessage request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            using var response = await _httpClientFactory.CreateClient("Shipping.Tipax.Auth")
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
            return new TokenState(
                accessToken,
                refreshToken,
                DateTimeOffset.UtcNow.AddSeconds(expiresIn));
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

    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string EnsureTrailingSlash(string value)
        => value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";

    private sealed record TokenState(
        string AccessToken,
        string? RefreshToken,
        DateTimeOffset ExpiresAt);
}
