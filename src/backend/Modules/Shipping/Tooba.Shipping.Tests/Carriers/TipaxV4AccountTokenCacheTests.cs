using System.Net;
using System.Text;
using Tooba.Shipping.Infrastructure.Carriers.Tipax;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

public sealed class TipaxV4AccountTokenCacheTests
{
    [Fact]
    public async Task Cache_is_scoped_to_account_id_and_configuration_version()
    {
        var handler = new CountingHandler();
        var source = new TipaxV4AccountTokenSource(new SingleClientFactory(handler));
        var accountA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var accountB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");

        var first = await source.GetAsync(accountA, 1, "https://api.test/", "https://tokens.test/Token", "u", "p", CancellationToken.None);
        var cached = await source.GetAsync(accountA, 1, "https://api.test/", "https://tokens.test/Token", "u", "p", CancellationToken.None);
        var bumped = await source.GetAsync(accountA, 2, "https://api.test/", "https://tokens.test/Token", "u", "p", CancellationToken.None);
        var other = await source.GetAsync(accountB, 1, "https://api.test/", "https://tokens.test/Token", "u", "p", CancellationToken.None);

        Assert.Equal("token-1", first);
        Assert.Equal("token-1", cached);
        Assert.Equal("token-2", bumped);
        Assert.Equal("token-3", other);
        Assert.Equal(3, handler.CallCount);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"access_token\":\"token-{CallCount}\",\"expires_in\":3600}}",
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        private readonly HttpClient _client = new(handler, disposeHandler: false);

        public HttpClient CreateClient(string name) => _client;
    }
}
