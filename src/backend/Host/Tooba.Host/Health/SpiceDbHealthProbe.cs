using Authzed.Api.V1;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace Tooba.Host;

/// <summary>
/// probe سبک readiness برای SpiceDB بدون full permission scan.
/// </summary>
internal sealed class SpiceDbHealthProbe : IDisposable
{
    private readonly AuthorizationHostOptions _options;
    private readonly GrpcChannel? _channel;
    private readonly PermissionsService.PermissionsServiceClient? _permissions;

    /// <summary>
    /// probe را فقط وقتی Mode=SpiceDb می‌سازد.
    /// </summary>
    public SpiceDbHealthProbe(IOptions<AuthorizationHostOptions> options)
    {
        _options = options.Value;
        if (!string.Equals(_options.Mode, "SpiceDb", StringComparison.Ordinal))
        {
            return;
        }

        _channel = CreateChannel(_options);
        _permissions = new PermissionsService.PermissionsServiceClient(_channel);
    }

    /// <summary>
    /// CheckPermission سبک با deadline کوتاه؛ هر پاسخ gRPC غیر Unavailable یعنی SpiceDB زنده است.
    /// </summary>
    internal async Task<bool> CheckAsync(CancellationToken cancellationToken)
    {
        if (_permissions is null || !_options.SpiceDb.ReadinessProbeEnabled)
        {
            return true;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Min(3, Math.Max(1, _options.SpiceDb.TimeoutSeconds))));
            _ = await _permissions.CheckPermissionAsync(
                new CheckPermissionRequest
                {
                    Resource = new ObjectReference { ObjectType = "tenant", ObjectId = "readiness-probe" },
                    Permission = "view",
                    Subject = new SubjectReference
                    {
                        Object = new ObjectReference { ObjectType = "user", ObjectId = "00000000-0000-0000-0000-000000000001" },
                    },
                    Consistency = new Consistency { MinimizeLatency = true },
                },
                deadline: Deadline(),
                cancellationToken: cts.Token);
            return true;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return false;
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _channel?.Dispose();

    private DateTime Deadline() => DateTime.UtcNow.AddSeconds(Math.Max(1, _options.SpiceDb.TimeoutSeconds));

    private static bool IsTransportFailure(Exception ex, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested && ex is OperationCanceledException)
        {
            return false;
        }

        return ex is RpcException or HttpRequestException or TaskCanceledException or IOException;
    }

    private static GrpcChannel CreateChannel(AuthorizationHostOptions options)
    {
        var endpoint = options.SpiceDb.Endpoint.Trim();
        var token = options.SpiceDb.Token;
        var credentials = CallCredentials.FromInterceptor((_, metadata) =>
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                metadata.Add("Authorization", $"Bearer {token}");
            }

            return Task.CompletedTask;
        });

        if (options.SpiceDb.UseTls)
        {
            var tls = ChannelCredentials.Create(new SslCredentials(), credentials);
            return GrpcChannel.ForAddress(NormalizeAddress(endpoint, tls: true), new GrpcChannelOptions { Credentials = tls });
        }

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        var insecure = ChannelCredentials.Create(ChannelCredentials.Insecure, credentials);
        return GrpcChannel.ForAddress(NormalizeAddress(endpoint, tls: false), new GrpcChannelOptions
        {
            Credentials = insecure,
            UnsafeUseInsecureChannelCallCredentials = true,
            HttpHandler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true },
        });
    }

    private static string NormalizeAddress(string endpoint, bool tls)
    {
        if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        return tls ? $"https://{endpoint}" : $"http://{endpoint}";
    }
}
