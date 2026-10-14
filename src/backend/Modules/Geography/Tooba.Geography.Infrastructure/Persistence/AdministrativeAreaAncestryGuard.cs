namespace Tooba.Geography.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL-side ancestry (parent chain) integrity guard for
/// <c>geography.administrative_areas</c>: the single enforcement seam that rejects a self-parent and
/// any indirect ancestor cycle (A -&gt; B -&gt; ... -&gt; A) for <em>every</em> real mutation route - both
/// <c>UPDATE</c> and <c>INSERT</c> - including paths that never touch the domain aggregate (raw SQL,
/// bulk scripts, future Application handlers)./// </summary>
/// <remarks>
/// <para>
/// <b>042 amendment (this is the operative version).</b> The 041 source guarded <c>UPDATE</c> only and
/// justified that with the claim "a freshly inserted row cannot already be in a chain". <b>That claim is
/// false and is now falsified and repaired.</b> Two rows created by the <em>same</em> multi-row
/// <c>INSERT</c> can reference each other: the composite foreign key is an <c>AFTER ROW</c> trigger fired
/// at end of statement, so by the time it is checked both rows exist and the pair
/// <c>(A -&gt; B, B -&gt; A)</c> satisfies it, committing a real cycle. The 041 "INSERT is unguarded by
/// proof" paragraph is superseded by <see cref="TriggerSql"/> firing on <c>INSERT OR UPDATE</c>.
/// </para>
/// <para>
/// <b>Strategy (re-confirmed, extended).</b> A domain walk is impossible at the aggregate boundary and
/// would only guard callers that remember it (see the 041 report for the full comparison), so the
/// invariant stays owned by the database in one function plus two immediate triggers - the smallest
/// artifact that applies to every write path without DI, Application layer or Host composition.
/// Two triggers are needed because PostgreSQL forbids <c>OLD</c> in the <c>WHEN</c> condition of an
/// <c>INSERT</c>-capable trigger (see <see cref="TriggerSql"/>).
/// </para>
/// <para>
/// <b>Immediate, not deferrable (042 decision).</b> The triggers are ordinary non-deferrable
/// <c>AFTER INSERT</c> / <c>AFTER UPDATE ... FOR EACH ROW</c> triggers, so the check runs at end of
/// statement inside the mutating transaction:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// It sees every row of the statement that created the candidate edges, which is exactly what makes the
/// multi-row-insert cycle detectable. A <em>deferred</em> check would run at <c>COMMIT</c>, under a
/// commit-time snapshot whose visibility of concurrently committed edges is not something this Task can
/// demonstrate; the immediate form has no such window.
/// </description>
/// </item>
/// <item>
/// <description>
/// It leaves no interval in which a cycle exists inside a transaction, so <c>SET CONSTRAINTS ... DEFERRED</c>
/// can no longer push the check away from the statement that caused it. Every single ancestry edge can
/// always be expressed acyclically, so nothing legitimate needs deferral; a future multi-statement
/// reorganisation must simply order its edges (which is the same discipline any immediate constraint
/// imposes anyway).
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Isolation precondition - fail closed.</b> The serialization argument below needs READ COMMITTED.
/// Instead of assuming it, the guard reads <c>transaction_isolation</c> and rejects the write in
/// REPEATABLE READ / SERIALIZABLE (<c>0A000</c>), where the walk could run against a frozen snapshot and
/// could miss an edge committed after the transaction started.
/// </para>
/// <para>
/// <b>Snapshot / lock ordering analysis (042).</b>
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <c>pg_advisory_xact_lock</c> is a transaction-scoped lock, released only after the committing
/// transaction is marked committed. A transaction that acquires it therefore observes every edge written
/// by every transaction that previously held it.
/// </description>
/// </item>
/// <item>
/// <description>
/// The walk is started <em>after</em> the lock is granted and is executed as its own SQL statement inside
/// this volatile PL/pgSQL function. In READ COMMITTED each such statement takes a fresh snapshot (the
/// function is not declared STABLE/IMMUTABLE, so SPI does not reuse the calling statement's snapshot), so
/// the walk reads the state that includes every commit that happened while this transaction was waiting.
/// That is what makes "last committing edge sees all other edges" true in practice, and it is verified by
/// the two-session test recorded in the 042 evidence rather than asserted from documentation alone.
/// </description>
/// </item>
/// <item>
/// <description>
/// The walk takes <b>no</b> row locks (plain reads only). It must not: the mutating statement already
/// holds its own row, so a locking read of another row could form a real lock cycle (T1 holds row A and
/// wants row B; T2 holds row B and waits for the advisory lock held by T1). Plain reads never block on
/// uncommitted rows, which keeps the advisory lock the only shared resource and mutual exclusion
/// deadlock-free by construction.
/// </description>
/// </item>
/// </list>
/// <para>
/// The recursive walk is bounded (<see cref="MaxAncestryDepth"/> levels) so that even a pre-existing
/// corrupt graph can never make the guard loop forever; exceeding the bound is reported as an error
/// instead of being silently accepted.
/// </para>
/// <para>
/// <b>Deliberately out of scope:</b> ordered application of this migration, module migration
/// registration/ordering, runtime DI wiring and any data load. Nothing in this file is executed by this
/// Task; it is inert migration source. Superuser DDL (<c>ALTER TABLE ... DISABLE TRIGGER</c>,
/// <c>session_replication_role = replica</c>, dropping the trigger) can bypass any trigger-based guard and
/// is an operational surface, not an application write path.
/// </para>
/// </remarks>
public static class AdministrativeAreaAncestryGuard
{
    /// <summary>نام تریگر نگهبان برای <c>UPDATE</c>؛ پایدار و قابل ارجاع در گزارش‌ها و ابزارهای عملیاتی.</summary>
    public const string UpdateTriggerName = "trg_administrative_areas_ancestry_guard_update";

    /// <summary>نام تریگر نگهبان برای <c>INSERT</c>؛ پایدار و قابل ارجاع در گزارش‌ها و ابزارهای عملیاتی.</summary>
    public const string InsertTriggerName = "trg_administrative_areas_ancestry_guard_insert";

    /// <summary>نام تابع نگهبان در schema <c>geography</c>.</summary>
    public const string FunctionName = "administrative_areas_ancestry_guard";

    /// <summary>
    /// First component of the advisory-lock key that serializes ancestry writes of this table
    /// (the migration date). The pair is a stable identity for this invariant and must never be reused
    /// for anything else in this database.
    /// </summary>
    public const string SerializationLockKey1 = "20261014";

    /// <summary>
    /// Second component of the advisory-lock key; see <see cref="SerializationLockKey1"/>. It identifies
    /// the slice that originated this invariant and stays stable for the invariant's lifetime, so the
    /// lock identity does not change just because the guard source was amended.
    /// </summary>
    public const string SerializationLockKey2 = "41";

    /// <summary>
    /// Maximum number of ancestor levels the guard will traverse. Far above any real administrative
    /// depth; reaching it means the stored graph is already corrupt, which the guard reports instead
    /// of accepting. The same literal is written inside <see cref="FunctionSql"/> (raw migration SQL
    /// must be literal); the two must stay in sync.
    /// </summary>
    public const int MaxAncestryDepth = 4096;

    /// <summary>
    /// DDL creating the guard function: isolation pre-check, advisory-lock serialization, bounded
    /// non-locking ancestor walk, stable error codes. Used by <c>INSERT</c> and <c>UPDATE</c> alike; it
    /// never reads <c>OLD</c>, so no operation-specific branch is needed.
    /// </summary>
    public const string FunctionSql = @"CREATE FUNCTION geography.administrative_areas_ancestry_guard()
RETURNS trigger
LANGUAGE plpgsql
AS $tooba_ancestry_guard$
DECLARE
    isolation_level text := lower(current_setting('transaction_isolation'));
    reached_depth integer := 0;
    cycle_found boolean := false;
BEGIN
    -- (1) Fail closed outside READ COMMITTED: the serialization argument below relies on each statement
    -- of this function taking a fresh snapshot. In REPEATABLE READ / SERIALIZABLE the walk could run
    -- against the snapshot fixed at transaction start and miss an edge committed after it, so ancestry
    -- writes are rejected instead of being accepted on an unsound check.
    IF isolation_level <> 'read committed' THEN
        RAISE EXCEPTION 'geography.administrative_area.ancestry_guard_isolation_unsupported'
            USING ERRCODE = '0A000',
                  DETAIL = format('administrative area ancestry integrity requires READ COMMITTED; transaction_isolation is %L.', isolation_level);
    END IF;

    -- A root area has no ancestry to validate. The composite FK still guarantees that any non-null
    -- parent exists in the same country.
    IF NEW.parent_administrative_area_id IS NULL THEN
        RETURN NULL;
    END IF;

    -- (2) Serialize every ancestry mutation of this table (transaction-scoped, released at commit).
    -- The walk below is a separate statement, so in READ COMMITTED it snapshots AFTER this wait.
    PERFORM pg_advisory_xact_lock(20261014, 41);

    -- (3) Bounded, non-locking walk up from the proposed parent. It reads committed rows plus this
    -- transaction's own changes, so it sees the row just inserted/updated and every sibling row created
    -- by the same statement; self-parent is caught on the first probe.
    WITH RECURSIVE ancestry(administrative_area_id, parent_administrative_area_id, depth) AS (
        SELECT node.administrative_area_id, node.parent_administrative_area_id, 1
        FROM geography.administrative_areas AS node
        WHERE node.administrative_area_id = NEW.parent_administrative_area_id
        UNION ALL
        SELECT parent.administrative_area_id, parent.parent_administrative_area_id, ancestry.depth + 1
        FROM ancestry
        JOIN geography.administrative_areas AS parent
          ON parent.administrative_area_id = ancestry.parent_administrative_area_id
        WHERE ancestry.depth < 4096
    )
    SELECT COALESCE(MAX(depth), 0),
           COALESCE(bool_or(administrative_area_id = NEW.administrative_area_id), false)
    INTO reached_depth, cycle_found
    FROM ancestry;

    IF cycle_found THEN
        RAISE EXCEPTION 'geography.administrative_area.ancestry_cycle'
            USING ERRCODE = '23514',
                  DETAIL = format('administrative area %s cannot point at parent %s: the parent chain already contains it.',
                                  NEW.administrative_area_id, NEW.parent_administrative_area_id);
    END IF;

    -- (4) Fail closed if the walk stopped at the safety bound: an unbounded/stored cycle exists.
    IF reached_depth >= 4096 THEN
        RAISE EXCEPTION 'geography.administrative_area.ancestry_depth_exceeded'
            USING ERRCODE = '54001',
                  DETAIL = format('administrative area %s parent chain did not terminate within %s levels.',
                                  NEW.parent_administrative_area_id, 4096);
    END IF;

    RETURN NULL;
END;
$tooba_ancestry_guard$;";

    /// <summary>
    /// DDL attaching the guard to <c>geography.administrative_areas</c> as two ordinary,
    /// <b>immediate</b> triggers (not deferrable constraint triggers) that share the one function. Both
    /// fire at end of statement together with the mutating statement, so the check sees every row created
    /// by a multi-row <c>INSERT</c>, leaves no window in which a cycle could exist, and raises no
    /// commit-time snapshot question.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two triggers instead of one is deliberate and empirically required: PostgreSQL rejects a trigger
    /// declared for both <c>INSERT</c> and <c>UPDATE</c> whose <c>WHEN</c> condition references
    /// <c>OLD</c> (<c>42P17: INSERT trigger's WHEN condition cannot reference OLD values</c>, observed
    /// against a real server while validating this migration). Splitting keeps the precise
    /// "an ancestry link actually changed" predicate for <c>UPDATE</c> rather than weakening it to a
    /// blanket per-row call.
    /// </para>
    /// <para>
    /// The <c>UPDATE</c> trigger stays off for re-rooting (<c>NEW.parent IS NULL</c> can never close a
    /// cycle) and for updates that change no ancestry link, which also avoids taking the serialization
    /// lock for unrelated column updates such as <c>UpdateCode</c>/<c>Activate</c>/<c>Deactivate</c>. The
    /// <c>INSERT</c> trigger must not mention <c>OLD</c>; it stays off only for a root row. Any inserted
    /// row with a parent is checked, including <c>INSERT ... ON CONFLICT DO UPDATE</c>.
    /// </para>
    /// </remarks>
    public const string TriggerSql = @"CREATE TRIGGER trg_administrative_areas_ancestry_guard_update
AFTER UPDATE ON geography.administrative_areas
FOR EACH ROW
WHEN (NEW.parent_administrative_area_id IS NOT NULL
      AND (OLD.parent_administrative_area_id IS DISTINCT FROM NEW.parent_administrative_area_id
           OR OLD.country_id IS DISTINCT FROM NEW.country_id))
EXECUTE FUNCTION geography.administrative_areas_ancestry_guard();

CREATE TRIGGER trg_administrative_areas_ancestry_guard_insert
AFTER INSERT ON geography.administrative_areas
FOR EACH ROW
WHEN (NEW.parent_administrative_area_id IS NOT NULL)
EXECUTE FUNCTION geography.administrative_areas_ancestry_guard();";

    /// <summary>DDL detaching both guard triggers; used by the migration's <c>Down</c> before the table is dropped.</summary>
    public const string DropTriggerSql =
        @"DROP TRIGGER trg_administrative_areas_ancestry_guard_update ON geography.administrative_areas;
DROP TRIGGER trg_administrative_areas_ancestry_guard_insert ON geography.administrative_areas;";

    /// <summary>DDL dropping the guard function; used by the migration's <c>Down</c>.</summary>
    public const string DropFunctionSql =
        @"DROP FUNCTION geography.administrative_areas_ancestry_guard();";
}
