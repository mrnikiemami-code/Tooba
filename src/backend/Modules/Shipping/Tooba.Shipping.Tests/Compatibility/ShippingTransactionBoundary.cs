namespace Tooba.Shipping.Tests.Compatibility;

/// <summary>
/// One transaction-shaped site found in a Shipping production source file.
/// </summary>
/// <param name="RelativePath">Repo-relative path.</param>
/// <param name="Member">Enclosing member name, as detected by brace scanning.</param>
/// <param name="LineNumber">1-based line number of the marker.</param>
/// <param name="Marker">The marker text that matched.</param>
internal sealed record ShippingTransactionSite(string RelativePath, string Member, int LineNumber, string Marker);

/// <summary>
/// One breach of the reviewed Shipping transaction / schema-authority boundary.
/// </summary>
/// <param name="RelativePath">Repo-relative path.</param>
/// <param name="Rule">Stable rule code.</param>
/// <param name="Detail">Human-readable explanation.</param>
internal sealed record ShippingBoundaryViolation(string RelativePath, string Rule, string Detail);

/// <summary>
/// Machine-checkable form of the TB-190-R1 architect decision: Shipping may open exactly ONE database
/// transaction, and only the reviewed staged draft-replacement boundary may do it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> The earlier TB-076 guard rejected the literal text <c>BeginTransaction</c>
/// anywhere in Shipping, because at that time no persisted authoring aggregate existed. TB-190-R1
/// explicitly authorised one module-owned transaction around the staged draft-order replacement, so that
/// blanket prohibition became stale. This detector replaces it with a narrower and stronger invariant and
/// is deliberately kept side-effect free so the negative fixtures can feed it synthetic sources.
/// </para>
/// <para>
/// <b>Scope of the proof.</b> This is a static source scanner. It proves that exactly one reviewed
/// transaction start site exists, that it lives in the reviewed member, that the reviewed member still has
/// its staged-save/commit/rollback statement shape, and that the only members calling it are the two typed
/// writers. Marker/member checks are <b>not</b> control-flow or data-flow enforcement: they cannot prove
/// that every failure path returns through the rollback, nor that the commit really happens after both
/// saves. The executed PostgreSQL rollback/cancellation/race facts are the behavioural half of the same
/// boundary, and the scanner never claims runtime atomicity on its own.
/// </para>
/// <para>
/// <b>No filename-only exemption.</b> The reviewed file is not skipped: its content and its enclosing
/// member are both checked, so a second transaction added to a different member of the same file fails.
/// </para>
/// </remarks>
internal static class ShippingTransactionBoundary
{
    /// <summary>Stable rule: a transaction site outside the reviewed boundary.</summary>
    public const string UnexpectedTransactionRule = "UNEXPECTED_SHIPPING_TRANSACTION";

    /// <summary>Stable rule: a Shipping production schema/startup migration authority.</summary>
    public const string StartupSchemaAuthorityRule = "SHIPPING_STARTUP_SCHEMA_AUTHORITY";

    /// <summary>Stable rule: operational quote / money authority leaking into Shipping.</summary>
    public const string OperationalWriteAuthorityRule = "SHIPPING_OPERATIONAL_WRITE_AUTHORITY";

    /// <summary>Stable rule: the reviewed boundary itself lost its required shape.</summary>
    public const string BoundaryShapeRule = "AUTHORING_TRANSACTION_SHAPE";

    /// <summary>Stable rule: the reviewed revision-capture boundary lost its required shape.</summary>
    public const string CaptureBoundaryShapeRule = "AUTHORING_CAPTURE_TRANSACTION_SHAPE";

    /// <summary>Stable rule: the reviewed boundary gained a caller or a callback indirection.</summary>
    public const string CallerUniverseRule = "AUTHORING_TRANSACTION_CALLER_UNIVERSE";

    /// <summary>The Shipping module root this guard owns.</summary>
    public const string ShippingRoot = "src/backend/Modules/Shipping/";

    /// <summary>The single reviewed transaction boundary.</summary>
    public const string AllowedBoundaryPath =
        "src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDraftDirectory.cs";

    /// <summary>
    /// The single reviewed member of that boundary. TB-199 extracted the staged-save boundary out of the
    /// region writer into this narrowly named typed helper so the region writer and the editor document
    /// writer share ONE begin-save-commit shape; the caller universe stays inside this file.
    /// </summary>
    public const string AllowedBoundaryMember = "SaveAuthoringReplacementAsync";

    /// <summary>
    /// TB-214: the ONE additional reviewed member allowed to open a transaction, for the immutable
    /// revision capture path only. It is named exactly, lives in the same reviewed file, and must keep its
    /// own single begin/save/commit/rollback shape; no filename-wide exemption exists.
    /// </summary>
    public const string AllowedCaptureBoundaryMember = "CaptureRevisionAsync";

    /// <summary>Transaction-start members authorized in the reviewed file.</summary>
    private static readonly string[] AllowedTransactionStartMembers =
        [AllowedBoundaryMember, AllowedCaptureBoundaryMember];

    /// <summary>
    /// The only other reviewed members allowed to mention a transaction type: the rollback/failure helper
    /// that the reviewed boundary calls and the two typed writers that delegate to it. None may open a
    /// transaction.
    /// </summary>
    public const string AllowedBoundaryHelperMember = "RollbackAsync";

    /// <summary>
    /// TB-215: the ONE additional reviewed member allowed to mention a transaction type, for the capture
    /// path only. It takes an optional transaction and can only dispose it: it must never open, save,
    /// commit or roll back anything, and no filename-wide exemption exists.
    /// </summary>
    public const string AllowedCaptureDisposalHelperMember = "DisposeCaptureTransactionAsync";

    /// <summary>Transaction-start markers.</summary>
    private static readonly string[] TransactionStartMarkers = ["BeginTransaction"];

    /// <summary>Transaction-type markers: outside the reviewed pair, using a transaction type is a site.</summary>
    private static readonly string[] TransactionTypeMarkers = ["IDbContextTransaction", "DbTransaction"];

    /// <summary>The only two typed writers allowed to call the reviewed boundary.</summary>
    public static readonly string[] ExpectedBoundaryCallers =
        ["ReplaceEditorDocumentAsync", "ReplaceRegionGroupsAsync"];

    /// <summary>Statement markers that define the reviewed member's staged save / commit / rollback shape.</summary>
    private static readonly string[] ShapeMarkers =
        ["BeginTransaction", "SaveChangesAsync(", "CommitAsync(", "RollbackAsync(", BoundaryCallMarker];

    private const string BoundaryCallMarker = "SaveAuthoringReplacementAsync(";

    /// <summary>Members inside the reviewed file that may legitimately mention a transaction type.</summary>
    private static readonly string[] AllowedTypeMarkerMembers =
    [
        AllowedBoundaryMember,
        AllowedBoundaryHelperMember,
        AllowedCaptureBoundaryMember,
        AllowedCaptureDisposalHelperMember,
    ];

    /// <summary>Shipping production schema-authority markers.</summary>
    private static readonly string[] SchemaAuthorityMarkers =
    [
        "AddModuleSchemaMigrator",
        "Database.MigrateAsync",
        "Migrator.MigrateAsync",
        "MigrateAsync(",
        "EnsureCreated(",
    ];

    /// <summary>
    /// Operational write / money-authority markers. Reading an operational quote Contract is lawful; taking
    /// payment, reservation or capacity authority inside Shipping is not.
    /// </summary>
    private static readonly string[] OperationalWriteMarkers =
    [
        "IShippingCapacityReservation",
        "CapacityReservation",
        "PendingAllocation",
        "SelectedShippingAmount =",
        "AmountCollectOnDelivery =",
        "PaymentDraft",
    ];

    /// <summary>
    /// Checks every Shipping module source and returns all boundary violations.
    /// </summary>
    /// <param name="sources">Repo-relative production sources (tests/generated trees already excluded).</param>
    public static IReadOnlyList<ShippingBoundaryViolation> Analyze(IEnumerable<OperationalBoundarySource> sources)
    {
        var violations = new List<ShippingBoundaryViolation>();
        var sites = new List<ShippingTransactionSite>();

        foreach (var source in sources)
        {
            var path = source.RelativePath.Replace('\\', '/');
            if (!path.StartsWith(ShippingRoot, StringComparison.Ordinal))
            {
                continue;
            }

            var lines = source.Text.Split('\n');

            for (var index = 0; index < lines.Length; index++)
            {
                var code = lines[index].TrimStart();
                if (!IsExecutable(code))
                {
                    continue;
                }

                var member = MemberAt(lines, index);
                var inAllowedFile = path == AllowedBoundaryPath;

                var starts = TransactionStartMarkers
                    .Where(marker => code.Contains(marker, StringComparison.Ordinal))
                    .ToArray();
                var types = TransactionTypeMarkers
                    .Where(marker => code.Contains(marker, StringComparison.Ordinal))
                    .ToArray();

                foreach (var marker in starts)
                {
                    var site = new ShippingTransactionSite(path, member ?? "<file>", index + 1, marker);
                    sites.Add(site);
                    // Only the reviewed members may OPEN a transaction, and only in the reviewed file.
                    if (!inAllowedFile
                        || !AllowedTransactionStartMembers.Contains(member ?? string.Empty, StringComparer.Ordinal))
                    {
                        violations.Add(new ShippingBoundaryViolation(
                            path,
                            UnexpectedTransactionRule,
                            $"transaction start '{marker}' at line {index + 1} in member '{site.Member}'; only "
                            + $"{AllowedBoundaryPath}::{{{string.Join(",", AllowedTransactionStartMembers)}}} may "
                            + "open a Shipping transaction"));
                    }
                }

                foreach (var marker in types)
                {
                    var site = new ShippingTransactionSite(path, member ?? "<file>", index + 1, marker);
                    sites.Add(site);
                    if (!inAllowedFile || !AllowedTypeMarkerMembers.Contains(member ?? string.Empty, StringComparer.Ordinal))
                    {
                        violations.Add(new ShippingBoundaryViolation(
                            path,
                            UnexpectedTransactionRule,
                            $"transaction type '{marker}' at line {index + 1} in member '{site.Member}'; outside "
                            + $"{AllowedBoundaryPath}::{{{string.Join(",", AllowedTypeMarkerMembers)}}} a "
                            + "transaction is not authorized"));
                    }
                }

                foreach (var marker in SchemaAuthorityMarkers)
                {
                    if (code.Contains(marker, StringComparison.Ordinal))
                    {
                        violations.Add(new ShippingBoundaryViolation(
                            path,
                            StartupSchemaAuthorityRule,
                            $"schema/startup migration authority '{marker}' at line {index + 1}; Shipping must never "
                            + "migrate or create its own schema at startup, including in the allowed boundary"));
                    }
                }

                foreach (var marker in OperationalWriteMarkers)
                {
                    if (code.Contains(marker, StringComparison.Ordinal))
                    {
                        violations.Add(new ShippingBoundaryViolation(
                            path,
                            OperationalWriteAuthorityRule,
                            $"operational write / money authority '{marker}' at line {index + 1}; the operational "
                            + "quote stays read-only and money authority stays outside Shipping"));
                    }
                }
            }
        }

        violations.AddRange(CheckAllowedBoundaryShape(
            sites,
            sources.FirstOrDefault(source => source.RelativePath.Replace('\\', '/') == AllowedBoundaryPath)));
        return violations;
    }

    /// <summary>
    /// The reviewed boundary must keep its exact shape: one begin, at least two staged saves, one commit
    /// and a rollback path INSIDE the reviewed member, and a caller universe of exactly the two typed
    /// writers with no callback/delegate indirection. A file that simply lost its transaction, its commit,
    /// its rollback or its staged save would otherwise pass "at most one".
    /// </summary>
    /// <remarks>
    /// This is marker/member scanning, not control flow. It cannot prove the commit is reached after both
    /// saves on every path, nor that the rollback is reachable from every failure; the executed PostgreSQL
    /// facts are what prove that behaviour.
    /// </remarks>
    private static IEnumerable<ShippingBoundaryViolation> CheckAllowedBoundaryShape(
        IReadOnlyList<ShippingTransactionSite> sites,
        OperationalBoundarySource? boundarySource)
    {
        var starts = sites
            .Where(site => site.RelativePath == AllowedBoundaryPath
                           && string.Equals(site.Member, AllowedBoundaryMember, StringComparison.Ordinal)
                           && TransactionStartMarkers.Contains(site.Marker, StringComparer.Ordinal))
            .ToArray();

        if (starts.Length != 1)
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                BoundaryShapeRule,
                $"the reviewed replacement boundary must open exactly one transaction, found {starts.Length}");
        }

        if (boundarySource is null)
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                BoundaryShapeRule,
                "the reviewed replacement boundary file is missing, so its staged shape cannot be verified");
            yield break;
        }

        var shape = ShapeSites(boundarySource)
            .Where(site => string.Equals(site.Member, AllowedBoundaryMember, StringComparison.Ordinal))
            .ToArray();

        if (shape.Count(site => site.Marker == "SaveChangesAsync(") < 2)
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                BoundaryShapeRule,
                $"the reviewed replacement boundary must keep BOTH staged saves, found "
                + $"{shape.Count(site => site.Marker == "SaveChangesAsync(")} SaveChangesAsync call(s) in "
                + $"{AllowedBoundaryMember}");
        }

        if (!shape.Any(site => site.Marker == "CommitAsync("))
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                BoundaryShapeRule,
                $"the reviewed replacement boundary must commit its transaction, found no CommitAsync in "
                + $"{AllowedBoundaryMember}");
        }

        if (!shape.Any(site => site.Marker == "RollbackAsync("))
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                BoundaryShapeRule,
                $"the reviewed replacement boundary must keep its rollback path, found no RollbackAsync in "
                + $"{AllowedBoundaryMember}");
        }

        // Caller universe: the declaration line is excluded; every remaining call must be an awaited direct
        // call inside one of the two typed writers. A third caller, or a delegate/callback indirection that
        // would let an unreviewed path open the boundary, fails.
        var callSites = ShapeSites(boundarySource)
            .Where(site => site.Marker == BoundaryCallMarker)
            .Where(site => !IsBoundaryDeclaration(boundarySource, site.LineNumber))
            .ToArray();

        var callers = callSites
            .Select(site => site.Member)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(member => member, StringComparer.Ordinal)
            .ToArray();

        if (!callers.SequenceEqual(ExpectedBoundaryCallers, StringComparer.Ordinal))
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                CallerUniverseRule,
                $"the reviewed boundary may only be called by "
                + $"[{string.Join(", ", ExpectedBoundaryCallers)}]; found [{string.Join(", ", callers)}]");
        }

        var lines = boundarySource.Text.Split('\n');
        foreach (var site in callSites)
        {
            var code = lines[site.LineNumber - 1].TrimStart();
            if (!code.Contains("await ", StringComparison.Ordinal)
                || code.Contains("=>", StringComparison.Ordinal)
                || code.Contains("Func<", StringComparison.Ordinal)
                || code.Contains("Invoke(", StringComparison.Ordinal))
            {
                yield return new ShippingBoundaryViolation(
                    AllowedBoundaryPath,
                    CallerUniverseRule,
                    $"the reviewed boundary call at line {site.LineNumber} in member '{site.Member}' must be a "
                    + "direct awaited call, not a delegate/callback indirection");
            }
        }

        // TB-214: the capture member has its OWN exact transaction shape. A capture that silently lost its
        // begin, its save, its commit or its rollback would otherwise pass "one start in an allowed member".
        var captureShape = ShapeSites(boundarySource)
            .Where(site => string.Equals(site.Member, AllowedCaptureBoundaryMember, StringComparison.Ordinal))
            .ToArray();
        var captureStarts = sites.Count(site =>
            site.RelativePath == AllowedBoundaryPath
            && string.Equals(site.Member, AllowedCaptureBoundaryMember, StringComparison.Ordinal)
            && TransactionStartMarkers.Contains(site.Marker, StringComparer.Ordinal));

        if (captureStarts != 1)
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                CaptureBoundaryShapeRule,
                $"the reviewed capture boundary must open exactly one transaction, found {captureStarts}");
        }

        if (captureShape.Count(site => site.Marker == "SaveChangesAsync(") < 1)
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                CaptureBoundaryShapeRule,
                $"the reviewed capture boundary must persist its snapshot, found no SaveChangesAsync in "
                + AllowedCaptureBoundaryMember);
        }

        if (!captureShape.Any(site => site.Marker == "CommitAsync("))
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                CaptureBoundaryShapeRule,
                $"the reviewed capture boundary must commit its transaction, found no CommitAsync in "
                + AllowedCaptureBoundaryMember);
        }

        if (!captureShape.Any(site => site.Marker == "RollbackAsync("))
        {
            yield return new ShippingBoundaryViolation(
                AllowedBoundaryPath,
                CaptureBoundaryShapeRule,
                $"the reviewed capture boundary must keep its rollback path, found no RollbackAsync in "
                + AllowedCaptureBoundaryMember);
        }
    }

    /// <summary>The one reviewed declaration line, as opposed to a call site.</summary>
    private static bool IsBoundaryDeclaration(OperationalBoundarySource boundarySource, int lineNumber)
    {
        var lines = boundarySource.Text.Split('\n');
        return lineNumber >= 1
               && lineNumber <= lines.Length
               && lines[lineNumber - 1].Contains(
                   "<Result<ShippingAuthoringDraft>> " + BoundaryCallMarker,
                   StringComparison.Ordinal);
    }

    /// <summary>Every statement marker in the reviewed file, attributed to its enclosing member.</summary>
    private static IReadOnlyList<ShippingTransactionSite> ShapeSites(OperationalBoundarySource boundarySource)
    {
        var lines = boundarySource.Text.Split('\n');
        var sites = new List<ShippingTransactionSite>();
        for (var index = 0; index < lines.Length; index++)
        {
            var code = lines[index].TrimStart();
            if (!IsExecutable(code))
            {
                continue;
            }

            var member = MemberAt(lines, index) ?? "<file>";
            foreach (var marker in ShapeMarkers)
            {
                if (code.Contains(marker, StringComparison.Ordinal))
                {
                    sites.Add(new ShippingTransactionSite(boundarySource.RelativePath, member, index + 1, marker));
                }
            }
        }

        return sites;
    }

    /// <summary>
    /// Finds the member that encloses a 0-based line by scanning backwards for the nearest
    /// one-indent-deep (4-space) declaration that opens a parameter list. This module's production style is
    /// consistent, and a brace-depth parser is more fragile here because transaction code is written inside
    /// try/catch/finally and switch expressions.
    /// </summary>
    private static string? MemberAt(IReadOnlyList<string> lines, int lineIndex)
    {
        for (var index = lineIndex; index >= 0; index--)
        {
            var raw = lines[index];
            var trimmed = raw.TrimStart();
            if (raw.Length - trimmed.Length != MemberIndent)
            {
                continue;
            }

            if (!IsExecutable(trimmed)
                || !trimmed.Contains('(', StringComparison.Ordinal)
                || trimmed.EndsWith(';')
                || IsControlFlow(trimmed))
            {
                continue;
            }

            var name = MemberName(trimmed);
            if (name.Length > 0)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>Indentation of a class-body member in this repository.</summary>
    private const int MemberIndent = 4;

    /// <summary>Statement keywords that contain a parameter list but are not members.</summary>
    private static bool IsControlFlow(string trimmed)
        => ControlFlowKeywords.Any(keyword =>
            trimmed.StartsWith(keyword, StringComparison.Ordinal));

    private static readonly string[] ControlFlowKeywords =
    [
        "if (", "if(", "for (", "for(", "foreach (", "foreach(", "while (", "while(", "switch (", "switch(",
        "using (", "using(", "catch (", "catch(", "lock (", "lock(", "return ", "await ", "throw ", "yield ",
        "new ", "else", "case ", "when (",
    ];

    /// <summary>Extracts the identifier that precedes the first parameter list in a declaration.</summary>
    private static string MemberName(string branch)
    {
        var paren = branch.IndexOf('(', StringComparison.Ordinal);
        if (paren < 0)
        {
            return string.Empty;
        }

        var cursor = paren - 1;
        while (cursor >= 0 && char.IsWhiteSpace(branch[cursor]))
        {
            cursor--;
        }

        var end = cursor;
        while (cursor >= 0 && (char.IsLetterOrDigit(branch[cursor]) || branch[cursor] == '_'))
        {
            cursor--;
        }

        return end > cursor ? branch[(cursor + 1)..(end + 1)] : string.Empty;
    }

    /// <summary>Executable lines only: XML docs and line comments explain boundaries and must stay legal.</summary>
    private static bool IsExecutable(string trimmed)
        => trimmed.Length > 0
           && !trimmed.StartsWith("///", StringComparison.Ordinal)
           && !trimmed.StartsWith("//", StringComparison.Ordinal)
           && !trimmed.StartsWith("*", StringComparison.Ordinal)
           && !trimmed.StartsWith("/*", StringComparison.Ordinal);
}
