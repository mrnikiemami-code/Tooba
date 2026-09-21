using Tooba.BuildingBlocks.Observability.Correlation;

namespace Tooba.BuildingBlocks.Tests.Observability;

public sealed class CorrelationIdContextTests
{
    public CorrelationIdContextTests() => CorrelationIdContext.Clear();

    [Fact]
    public void TryNormalize_accepts_valid_guid_formats()
    {
        var dashed = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.True(CorrelationIdContext.TryNormalize(dashed.ToString("D"), out var n1));
        Assert.Equal(dashed.ToString("N"), n1);
        Assert.True(CorrelationIdContext.TryNormalize(dashed.ToString("N"), out var n2));
        Assert.Equal(dashed.ToString("N"), n2);
    }

    [Fact]
    public void Set_rejects_invalid_input()
    {
        Assert.Throws<ArgumentException>(() => CorrelationIdContext.Set("not-a-guid"));
        Assert.Throws<ArgumentException>(() => CorrelationIdContext.Set(" "));
    }

    [Fact]
    public void Ensure_generated_id_is_stable_for_flow()
    {
        CorrelationIdContext.Clear();
        var generated = CorrelationIdContext.Ensure();
        Assert.False(string.IsNullOrWhiteSpace(generated));
        Assert.Equal(generated, CorrelationIdContext.Ensure(Guid.NewGuid().ToString("D")));
        Assert.Equal(32, generated.Length);
        Assert.True(Guid.TryParse(generated, out _));
    }

    [Fact]
    public void Ensure_normalizes_valid_incoming_when_empty()
    {
        CorrelationIdContext.Clear();
        var incoming = Guid.Parse("11111111-2222-3333-4444-555555555555");
        Assert.Equal(incoming.ToString("N"), CorrelationIdContext.Ensure(incoming.ToString("D")));
    }

    [Fact]
    public void Nested_scope_restores_previous_value()
    {
        var outer = Guid.NewGuid().ToString("N");
        CorrelationIdContext.Set(outer);
        var inner = Guid.NewGuid().ToString("D");
        using (CorrelationIdContext.BeginScope(inner))
        {
            Assert.Equal(Guid.Parse(inner).ToString("N"), CorrelationIdContext.Current);
        }

        Assert.Equal(outer, CorrelationIdContext.Current);
    }

    [Fact]
    public async Task Async_flows_are_isolated()
    {
        CorrelationIdContext.Clear();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string? first = null;
        string? second = null;

        var t1 = Task.Run(async () =>
        {
            CorrelationIdContext.Clear();
            first = CorrelationIdContext.Ensure();
            await gate.Task;
            Assert.Equal(first, CorrelationIdContext.Current);
        });

        var t2 = Task.Run(async () =>
        {
            CorrelationIdContext.Clear();
            second = CorrelationIdContext.Ensure();
            await gate.Task;
            Assert.Equal(second, CorrelationIdContext.Current);
        });

        await Task.WhenAny(Task.Delay(100), Task.WhenAll(
            Task.Run(async () => { while (first is null || second is null) await Task.Delay(5); })));
        gate.SetResult();
        await Task.WhenAll(t1, t2);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Provider_delegates_to_context()
    {
        CorrelationIdContext.Clear();
        var provider = new CorrelationIdProvider();
        var id = provider.EnsureCorrelationId(Guid.NewGuid().ToString("D"));
        Assert.Equal(id, provider.GetCorrelationId());
        Assert.Equal(Guid.Parse(id), provider.GetCorrelationGuid());
        Assert.Equal(id, new CorrelationContextAdapter().CorrelationId);
    }
}
