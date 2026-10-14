namespace Tooba.Geography.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL-side ancestry (parent chain) integrity guard for
/// <c>geography.administrative_areas</c>: the single enforcement seam that rejects a self-parent and
/// any indirect ancestor cycle (A -&gt; B -&gt; ... -&gt; A) for <em>every</em> future write path, including
/// paths that never touch the domain aggregate (raw SQL, bulk scripts, future Application handlers).
/// </summary>
/// <remarks>
/// <para>
/// Strategy selection (041). Two concrete strategies were compared:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Application/domain ancestor-walk with transaction serialization.</b> A domain walk inside
/// <c>AdministrativeArea</c> cannot see sibling rows at all (single-aggregate boundary), so the walk
/// would have to live in an Application handler or an Infrastructure repository, and a pessimistic
/// serialization of the whole ancestry would have to be owned by the caller. That strategy guards
/// only the code paths that remember to call it, needs a runtime write layer and DI registration that
/// this Task explicitly forbids, and cannot guard direct SQL. Rejected as larger and weaker.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>PostgreSQL constraint trigger (selected).</b> One deferrable, per-row, bounded recursive walk
/// that is owned by the database and therefore applies to every future write path. It adds no runtime
/// registration, no DI, no Application layer and no Host composition - the smallest artifact that can
/// hold the invariant at the boundary where rows actually meet.
/// </description>
/// </item>
/// </list>
/// <para>
/// Note on ordinary relational primitives: a CHECK constraint and a FOREIGN KEY cannot express a
/// recursive property such as "no ancestor cycle". A composite FK
/// (<c>(parent_administrative_area_id, country_id) -&gt; (administrative_area_id, country_id)</c>, added in
/// 040) already guarantees that a parent exists and lives in the same country, but it happily accepts
/// A -&gt; B -&gt; A. Cycles therefore need an explicit recursive check; this file is that check.
/// </para>
/// <para>
/// Concurrency design (the reason the trigger also serializes):
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// Every ancestry mutation of this table takes the <em>same</em> transaction-scoped advisory lock
/// (<see cref="SerializationLockKey1"/>, <see cref="SerializationLockKey2"/>) as its very first action,
/// so at most one transaction can be inside the walk at any time.
/// </description>
/// </item>
/// <item>
/// <description>
/// The walk itself takes <b>no</b> row locks (plain reads). It must not: the mutating statement has
/// already locked its own row, so a locking read of another row would create a real lock cycle
/// (transaction 1 holds row A and wants row B; transaction 2 holds row B and wants the advisory lock
/// held by transaction 1). Plain reads cannot block on uncommitted rows, so the advisory lock stays
/// the only shared resource and mutual exclusion is deadlock-free.
/// </description>
/// </item>
/// <item>
/// <description>
/// A transaction that acquires the lock after another transaction released it observes the other's
/// committed rows: PostgreSQL releases transaction locks only after the committing transaction is
/// marked committed, and a READ COMMITTED statement takes its snapshot when it starts. Therefore the
/// transaction that commits the <em>last</em> edge of a potential cycle sees every other edge of that
/// cycle as committed, and detects it. No ordering of concurrent reparenting transactions can commit a
/// cycle.
/// </description>
/// </item>
/// <item>
/// <description>
/// The argument depends on READ COMMITTED, so the guard does not merely document that assumption: it
/// reads <c>transaction_isolation</c> and <b>fails closed</b> in REPEATABLE READ / SERIALIZABLE, where
/// the walk could be evaluated against a stale snapshot and could miss a concurrently committed edge.
/// The platform default (Npgsql: server default READ COMMITTED) satisfies the guard.
/// </description>
/// </item>
/// </list>
/// <para>
/// The recursive walk is bounded (<see cref="MaxAncestryDepth"/> levels) so that even a pre-existing
/// corrupt graph can never make the guard loop forever; exceeding the bound is reported as an error
/// instead of being silently accepted.
/// </para>
/// <para>
/// Deliberate out of scope here: the ordered application of this migration, module migration
/// registration/ordering, runtime DI wiring and any data load. Nothing in this file is executed by
/// this Task; it is inert migration source.
/// </para>
/// </remarks>
public static class AdministrativeAreaAncestryGuard
{
    /// <summary>نام تریگر نگهبان؛ پایدار و قابل ارجاع در گزارش‌ها و ابزارهای عملیاتی.</summary>
    public const string TriggerName = "trg_administrative_areas_ancestry_guard";

    /// <summary>نام تابع نگهبان در schema <c>geography</c>.</summary>
    public const string FunctionName = "administrative_areas_ancestry_guard";

    /// <summary>
    /// First component of the advisory-lock key that serializes ancestry writes of this table
    /// (the migration date; <see cref="SerializationLockKey2"/> is the owning Task number).
    /// The pair is a stable identity for this invariant and must never be reused for anything else in
    /// this database.
    /// </summary>
    public const string SerializationLockKey1 = "20261014";

    /// <summary>Second component of the advisory-lock key; see <see cref="SerializationLockKey1"/>.</summary>
    public const string SerializationLockKey2 = "41";

    /// <summary>
    /// Maximum number of ancestor levels the guard will traverse. Far above any real administrative
    /// depth; reaching it means the stored graph is already corrupt, which the guard reports instead
    /// of accepting.
    /// </summary>
    public const int MaxAncestryDepth = 4096;

    /// <summary>
    /// DDL creating the guard function: isolation pre-check, advisory-lock serialization, bounded
    /// non-locking ancestor walk, stable error codes.
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
    -- (1) Fail closed outside READ COMMITTED: the serialization argument below relies on each
    -- statement taking a fresh snapshot. In REPEATABLE READ / SERIALIZABLE the walk could run against
    -- a stale snapshot and miss an edge committed after this transaction started, so ancestry writes
    -- are rejected instead of being accepted on an unsound check.
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
    PERFORM pg_advisory_xact_lock(20261014, 41);

    -- (3) Bounded, non-locking walk up from the proposed parent. The walk reads committed rows only;
    -- the row under update is already visible to this transaction, so self-parent is caught here too.
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
    /// DDL attaching the guard to <c>geography.administrative_areas</c>. Deferrable-but-immediate: the
    /// check runs with the mutating statement by default, and a future authorized reorg transaction may
    /// opt into <c>SET CONSTRAINTS ... DEFERRED</c>. Fires only when an ancestry link actually changed.
    /// </summary>
    public const string TriggerSql = @"CREATE CONSTRAINT TRIGGER trg_administrative_areas_ancestry_guard
AFTER UPDATE ON geography.administrative_areas
DEFERRABLE INITIALLY IMMEDIATE
FOR EACH ROW
WHEN (OLD.parent_administrative_area_id IS DISTINCT FROM NEW.parent_administrative_area_id
      OR OLD.country_id IS DISTINCT FROM NEW.country_id)
EXECUTE FUNCTION geography.administrative_areas_ancestry_guard();";

    /// <summary>DDL detaching the guard; used by the migration's <c>Down</c> before the table is dropped.</summary>
    public const string DropTriggerSql =
        @"DROP TRIGGER trg_administrative_areas_ancestry_guard ON geography.administrative_areas;";

    /// <summary>DDL dropping the guard function; used by the migration's <c>Down</c>.</summary>
    public const string DropFunctionSql =
        @"DROP FUNCTION geography.administrative_areas_ancestry_guard();";
}
