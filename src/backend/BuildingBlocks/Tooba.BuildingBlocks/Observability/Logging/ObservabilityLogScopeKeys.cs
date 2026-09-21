namespace Tooba.BuildingBlocks.Observability.Logging;

/// <summary>کلیدهای scope لاگ ساختاریافته.</summary>
public static class ObservabilityLogScopeKeys
{
    /// <summary>شناسه همبستگی.</summary>
    public const string CorrelationId = "CorrelationId";

    /// <summary>TraceId.</summary>
    public const string TraceId = "TraceId";

    /// <summary>SpanId.</summary>
    public const string SpanId = "SpanId";

    /// <summary>شناسه درخواست ASP.NET.</summary>
    public const string RequestId = "RequestId";

    /// <summary>Tenant در صورت شناخته بودن.</summary>
    public const string TenantId = "TenantId";

    /// <summary>Store در صورت شناخته بودن.</summary>
    public const string StoreId = "StoreId";

    /// <summary>Actor در صورت شناخته بودن.</summary>
    public const string ActorId = "ActorId";

    /// <summary>IP کلاینت (فقط اگر trusted-proxy safe).</summary>
    public const string ClientIp = "ClientIp";

    /// <summary>متد HTTP.</summary>
    public const string HttpMethod = "HttpMethod";

    /// <summary>مسیر درخواست.</summary>
    public const string HttpPath = "HttpPath";
}
