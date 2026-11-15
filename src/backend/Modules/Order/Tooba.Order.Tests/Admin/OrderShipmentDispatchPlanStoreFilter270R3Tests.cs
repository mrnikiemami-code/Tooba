using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.Order.Infrastructure.Admin.Operations;
using Xunit;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-270-R3 — the plan store's duplicate arbitration matches ONLY its own unique <c>shipment_id</c> index.
/// </summary>
/// <remarks>
/// <para>
/// The R2 store treated ANY <c>23505</c> as "another confirmation of this parcel won" and answered it from the
/// stored row. A different unique constraint (or an unknown one) is a genuine error, and silently reinterpreting it as
/// an exact replay could answer a plan that a different failure produced.
/// </para>
/// <para>
/// These are deterministic unit facts about the store's own filter. The constraint NAME is not invented here: the
/// canonical relational case (<c>AdminShipmentDispatchPlan270R2RelationalTests</c> B) raises the real Postgres
/// violation on that index and asserts the same name, and the additive migration creates it as
/// <c>ux_shipment_dispatch_plans_shipment</c>.
/// </para>
/// </remarks>
public sealed class OrderShipmentDispatchPlanStoreFilter270R3Tests
{
    [Fact]
    public void The_plan_tables_own_unique_shipment_index_is_the_only_duplicate_that_answers_a_replay()
    {
        Assert.True(OrderShipmentDispatchPlanStore.IsUniqueShipmentViolation(
            Violation("23505", OrderShipmentDispatchPlanStore.UniqueShipmentConstraint)));
    }

    [Fact]
    public void A_duplicate_on_another_constraint_is_not_this_parcels_plan_conflict()
    {
        // A 23505 on a different unique index must surface, never be reinterpreted as an exact replay/conflict.
        Assert.False(OrderShipmentDispatchPlanStore.IsUniqueShipmentViolation(
            Violation("23505", "ux_some_other_index")));

        // A unique violation that reports NO constraint cannot be attributed to this index either.
        Assert.False(OrderShipmentDispatchPlanStore.IsUniqueShipmentViolation(Violation("23505", null)));
    }

    [Fact]
    public void A_non_unique_violation_on_the_same_index_name_is_still_an_error()
    {
        // FK/check violations (23503/23514) name constraints too; the SQLSTATE must match, not the name alone.
        Assert.False(OrderShipmentDispatchPlanStore.IsUniqueShipmentViolation(
            Violation("23503", OrderShipmentDispatchPlanStore.UniqueShipmentConstraint)));
        Assert.False(OrderShipmentDispatchPlanStore.IsUniqueShipmentViolation(
            Violation("23514", OrderShipmentDispatchPlanStore.UniqueShipmentConstraint)));
    }

    private static DbUpdateException Violation(string sqlState, string? constraintName)
        => new(
            "the store's duplicate filter",
            new PostgresException(
                "simulated server error",
                "ERROR",
                "ERROR",
                sqlState,
                constraintName: constraintName));
}
