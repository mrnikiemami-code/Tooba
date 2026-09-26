using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.AddressBook.Infrastructure.Persistence;

#nullable disable

namespace Tooba.AddressBook.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T004-R24-R1 — نام و نام خانوادگی جدا در دفترچه.</summary>
[DbContext(typeof(AddressBookDbContext))]
[Migration("20260913180000_AddRecipientNameParts")]
public partial class AddRecipientNameParts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE address_book.customer_addresses
                ADD COLUMN IF NOT EXISTS first_name character varying(64) NOT NULL DEFAULT '';
            ALTER TABLE address_book.customer_addresses
                ADD COLUMN IF NOT EXISTS last_name character varying(64) NOT NULL DEFAULT '';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE address_book.customer_addresses DROP COLUMN IF EXISTS first_name;
            ALTER TABLE address_book.customer_addresses DROP COLUMN IF EXISTS last_name;
            """);
    }
}
