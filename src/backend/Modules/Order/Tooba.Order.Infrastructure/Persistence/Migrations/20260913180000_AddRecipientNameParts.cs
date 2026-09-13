using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R24-R1 — نام و نام خانوادگی جدا در پیش‌نویس ارسال و تصویر سفارش.</summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20260913180000_AddRecipientNameParts")]
public partial class AddRecipientNameParts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_drafts
                ADD COLUMN IF NOT EXISTS first_name character varying(64) NOT NULL DEFAULT '';
            ALTER TABLE "order".cart_shipping_drafts
                ADD COLUMN IF NOT EXISTS last_name character varying(64) NOT NULL DEFAULT '';
            ALTER TABLE "order".checkouts
                ADD COLUMN IF NOT EXISTS recipient_first_name character varying(64) NOT NULL DEFAULT '';
            ALTER TABLE "order".checkouts
                ADD COLUMN IF NOT EXISTS recipient_last_name character varying(64) NOT NULL DEFAULT '';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_drafts DROP COLUMN IF EXISTS first_name;
            ALTER TABLE "order".cart_shipping_drafts DROP COLUMN IF EXISTS last_name;
            ALTER TABLE "order".checkouts DROP COLUMN IF EXISTS recipient_first_name;
            ALTER TABLE "order".checkouts DROP COLUMN IF EXISTS recipient_last_name;
            """);
    }
}
