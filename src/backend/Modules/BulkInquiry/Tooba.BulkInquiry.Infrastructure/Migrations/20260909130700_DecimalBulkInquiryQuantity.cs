using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.BulkInquiry.Infrastructure.Persistence;

#nullable disable

namespace Tooba.BulkInquiry.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(BulkInquiryDbContext))]
    [Migration("20260909130700_DecimalBulkInquiryQuantity")]
    public partial class DecimalBulkInquiryQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE bulk_inquiry.bulk_purchase_inquiries
                ALTER COLUMN quantity TYPE numeric(18,6)
                USING quantity::numeric(18,6);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE bulk_inquiry.bulk_purchase_inquiries
                ALTER COLUMN quantity TYPE integer
                USING round(quantity)::integer;
                """);
        }
    }
}
