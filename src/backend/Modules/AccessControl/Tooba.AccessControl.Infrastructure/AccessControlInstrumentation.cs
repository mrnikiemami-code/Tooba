namespace Tooba.AccessControl.Infrastructure;

/// <summary>
/// شمارنده‌های سبک Access Control برای تله‌متری Host.
/// </summary>
public sealed class AccessControlInstrumentation
{
    private long _roleMutations;
    private long _assignmentMutations;
    private long _ceilingMutations;
    private long _tupleSyncs;

    /// <summary>تغییر نقش.</summary>
    public void RecordRoleMutation() => Interlocked.Increment(ref _roleMutations);

    /// <summary>تغییر تخصیص.</summary>
    public void RecordAssignmentMutation() => Interlocked.Increment(ref _assignmentMutations);

    /// <summary>تغییر سقف.</summary>
    public void RecordCeilingMutation() => Interlocked.Increment(ref _ceilingMutations);

    /// <summary>همگام‌سازی tuple.</summary>
    public void RecordTupleSync() => Interlocked.Increment(ref _tupleSyncs);

    /// <summary>نمونه‌برداری شمارنده‌ها.</summary>
    public (long Roles, long Assignments, long Ceilings, long TupleSyncs) Snapshot() =>
        (Volatile.Read(ref _roleMutations),
            Volatile.Read(ref _assignmentMutations),
            Volatile.Read(ref _ceilingMutations),
            Volatile.Read(ref _tupleSyncs));
}
