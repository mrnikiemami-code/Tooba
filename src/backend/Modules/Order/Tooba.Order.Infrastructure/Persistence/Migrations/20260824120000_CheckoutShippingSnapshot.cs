using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// تصویر محدود ارسال/تماس روی checkout. ماژول Address جدا ساخته نمی‌شود.
/// </summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20260824120000_CheckoutShippingSnapshot")]
public class CheckoutShippingSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "recipient_name",
            schema: "order",
            table: "checkouts",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "contact_mobile",
            schema: "order",
            table: "checkouts",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "province_name",
            schema: "order",
            table: "checkouts",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "city_name",
            schema: "order",
            table: "checkouts",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "postal_address",
            schema: "order",
            table: "checkouts",
            type: "character varying(512)",
            maxLength: 512,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "postal_code",
            schema: "order",
            table: "checkouts",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "shipping_method_code",
            schema: "order",
            table: "checkouts",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "shipping_method_label",
            schema: "order",
            table: "checkouts",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "recipient_name", schema: "order", table: "checkouts");
        migrationBuilder.DropColumn(name: "contact_mobile", schema: "order", table: "checkouts");
        migrationBuilder.DropColumn(name: "province_name", schema: "order", table: "checkouts");
        migrationBuilder.DropColumn(name: "city_name", schema: "order", table: "checkouts");
        migrationBuilder.DropColumn(name: "postal_address", schema: "order", table: "checkouts");
        migrationBuilder.DropColumn(name: "postal_code", schema: "order", table: "checkouts");
        migrationBuilder.DropColumn(name: "shipping_method_code", schema: "order", table: "checkouts");
        migrationBuilder.DropColumn(name: "shipping_method_label", schema: "order", table: "checkouts");
    }
}
