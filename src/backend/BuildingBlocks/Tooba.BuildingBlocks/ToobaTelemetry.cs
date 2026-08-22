namespace Tooba.BuildingBlocks;

public static class ToobaTelemetry
{
    public const string ActivitySourceName = "Tooba";
    public const string MeterName = "Tooba";

    public static readonly System.Diagnostics.ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly System.Diagnostics.Metrics.Meter Meter = new(MeterName);
}
