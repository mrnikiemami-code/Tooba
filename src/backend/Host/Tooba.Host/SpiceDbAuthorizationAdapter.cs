using System.Diagnostics;
using Authzed.Api.V1;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// آداپتر واقعی SpiceDB با Authzed.Net 1.6.0. نوع‌های gRPC از Domain/Application/ModuleContracts بیرون نمی‌مانند.
/// شکست شبکه ALLOW نیست. InMemory اینجا fallback تولید نیست.
/// </summary>
internal sealed class SpiceDbAuthorizationAdapter : IAuthorizationService, IAuthorizationTupleWriter, IDisposable
{
    private readonly AuthorizationHostOptions _options;
    private readonly AuthorizationInstrumentation _telemetry;
    private readonly IAuthorizationSecurityEventSink _audit;
    private readonly ILogger<SpiceDbAuthorizationAdapter> _logger;
    private readonly GrpcChannel _channel;
    private readonly PermissionsService.PermissionsServiceClient _permissions;
    private readonly SchemaService.SchemaServiceClient _schema;

    /// <summary>
    /// کانال gRPC را طبق TLS و توکن می‌سازد. توکن به لاگ نمی‌رود تا credential در stdout نشت نکند.
    /// </summary>
    public SpiceDbAuthorizationAdapter(
        IOptions<AuthorizationHostOptions> options,
        AuthorizationInstrumentation telemetry,
        IAuthorizationSecurityEventSink audit,
        ILogger<SpiceDbAuthorizationAdapter> logger)
    {
        _options = options.Value;
        _telemetry = telemetry;
        _audit = audit;
        _logger = logger;
        _channel = CreateChannel(_options);
        _permissions = new PermissionsService.PermissionsServiceClient(_channel);
        _schema = new SchemaService.SchemaServiceClient(_channel);
    }

    /// <summary>
    /// schema نسخه‌بندی‌شده را روی SpiceDB می‌نویسد. بازنویسی کور استارت تولید اینجا تصمیم‌گیری نمی‌شود.
    /// </summary>
    public async Task WriteSchemaAsync(string schemaText, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaText);
        try
        {
            await _schema.WriteSchemaAsync(new WriteSchemaRequest { Schema = schemaText }, deadline: Deadline(), cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            throw new InvalidOperationException("authorization.unavailable", ex);
        }
    }

    /// <inheritdoc />
    public async Task WriteAsync(AuthorizationRelationshipWrite write, CancellationToken cancellationToken)
    {
        AuthorizationContractValidator.Validate(write);
        try
        {
            await _permissions.WriteRelationshipsAsync(
                new WriteRelationshipsRequest
                {
                    Updates =
                    {
                        new RelationshipUpdate
                        {
                            Operation = RelationshipUpdate.Types.Operation.Touch,
                            Relationship = new Relationship
                            {
                                Resource = new ObjectReference
                                {
                                    ObjectType = write.Resource.Type,
                                    ObjectId = write.Resource.Id,
                                },
                                Relation = write.Relation,
                                Subject = new SubjectReference
                                {
                                    Object = new ObjectReference
                                    {
                                        ObjectType = write.Subject.Type,
                                        ObjectId = write.Subject.Id,
                                    },
                                },
                            },
                        },
                    },
                },
                deadline: Deadline(),
                cancellationToken: cancellationToken);
            await _audit.RecordAsync("relationship_changed", write.Resource.Type, write.Relation, cancellationToken);
            _logger.LogInformation(
                "SpiceDB relationship write finished. ResourceType {ResourceType} Relation {Relation}",
                write.Resource.Type,
                write.Relation);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            throw new InvalidOperationException("authorization.unavailable", ex);
        }
    }

    /// <inheritdoc />
    public async Task<AuthorizationDecision> CanAsync(AuthorizationCheck check, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        AuthorizationContractValidator.Validate(check);
        try
        {
            var response = await _permissions.CheckPermissionAsync(
                new CheckPermissionRequest
                {
                    Resource = new ObjectReference
                    {
                        ObjectType = check.Resource.Type,
                        ObjectId = check.Resource.Id,
                    },
                    Permission = check.Permission,
                    Subject = new SubjectReference
                    {
                        Object = new ObjectReference
                        {
                            ObjectType = check.Subject.Type,
                            ObjectId = check.Subject.Id,
                        },
                    },
                    Consistency = new Consistency { FullyConsistent = true },
                },
                deadline: Deadline(),
                cancellationToken: cancellationToken);

            var decision = response.Permissionship switch
            {
                CheckPermissionResponse.Types.Permissionship.HasPermission => AuthorizationDecision.Allow(),
                CheckPermissionResponse.Types.Permissionship.NoPermission => AuthorizationDecision.Deny(),
                _ => AuthorizationDecision.Deny(),
            };
            if (decision.Kind == AuthorizationDecisionKind.Deny)
            {
                await _audit.RecordAsync("permission_denied", check.Resource.Type, check.Permission, cancellationToken);
            }

            _logger.LogInformation(
                "SpiceDB permission check finished. ResourceType {ResourceType} Permission {Permission} Outcome {Outcome} Edition {Edition}",
                check.Resource.Type,
                check.Permission,
                decision.Kind,
                check.CallContext.Edition);
            _telemetry.Record(
                decision.Kind,
                check.Resource.Type,
                check.Permission,
                check.CallContext.Edition,
                Stopwatch.GetElapsedTime(started).Milliseconds);
            return decision;
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "SpiceDB permission check unavailable. ResourceType {ResourceType} Permission {Permission}", check.Resource.Type, check.Permission);
            _telemetry.Record(
                AuthorizationDecisionKind.Unavailable,
                check.Resource.Type,
                check.Permission,
                check.CallContext.Edition,
                Stopwatch.GetElapsedTime(started).Milliseconds);
            return AuthorizationDecision.Unavailable("spicedb.unavailable");
        }
    }

    /// <summary>
    /// کانال را آزاد می‌کند.
    /// </summary>
    public void Dispose() => _channel.Dispose();

    /// <summary>
    /// مهلت gRPC را از TimeoutSeconds می‌سازد تا تماس بی‌پایان Host را باز نگذارد.
    /// </summary>
    private DateTime Deadline() => DateTime.UtcNow.AddSeconds(Math.Max(1, _options.SpiceDb.TimeoutSeconds));

    /// <summary>
    /// کانال را با TLS یا HTTP/2 بدون رمز می‌سازد. توکن فقط در metadata Bearer است نه در لاگ.
    /// </summary>
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
            return GrpcChannel.ForAddress(NormalizeAddress(endpoint, tls: true), new GrpcChannelOptions
            {
                Credentials = tls,
            });
        }

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        var insecure = ChannelCredentials.Create(ChannelCredentials.Insecure, credentials);
        return GrpcChannel.ForAddress(NormalizeAddress(endpoint, tls: false), new GrpcChannelOptions
        {
            Credentials = insecure,
            UnsafeUseInsecureChannelCallCredentials = true,
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
            },
        });
    }

    /// <summary>
    /// خطای شبکه/سرور را از لغو صریح تماس جدا می‌کند تا fail-open نشود و cancel استارت قورت داده نشود.
    /// </summary>
    private static bool IsTransportFailure(Exception ex, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested && ex is OperationCanceledException)
        {
            return false;
        }

        return ex is RpcException or HttpRequestException or TaskCanceledException or IOException;
    }

    /// <summary>
    /// آدرس را به URI کانال تبدیل می‌کند بدون اینکه توکن را در رشته جا دهد.
    /// </summary>
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
