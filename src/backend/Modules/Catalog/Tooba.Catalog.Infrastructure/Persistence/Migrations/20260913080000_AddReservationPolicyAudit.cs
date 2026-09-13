using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R18 — ممیزی تغییر تنظیم سیاست رزرو.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260913080000_AddReservationPolicyAudit")]
public partial class AddReservationPolicyAudit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS catalog.reservation_policy_audit_events
            (
                event_id uuid NOT NULL,
                level character varying(16) NOT NULL,
                scope_id uuid NULL,
                field character varying(64) NOT NULL,
                old_override character varying(32) NULL,
                new_override character varying(32) NULL,
                actor_user_id uuid NOT NULL,
                occurred_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_reservation_policy_audit_events PRIMARY KEY (event_id)
            );

            CREATE INDEX IF NOT EXISTS ix_reservation_policy_audit_occurred
                ON catalog.reservation_policy_audit_events (occurred_at);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS catalog.reservation_policy_audit_events;");
    }
}
