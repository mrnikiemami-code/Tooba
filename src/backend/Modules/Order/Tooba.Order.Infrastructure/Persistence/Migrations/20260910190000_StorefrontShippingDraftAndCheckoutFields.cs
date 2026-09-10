using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260910190000_StorefrontShippingDraftAndCheckoutFields")]
    public partial class StorefrontShippingDraftAndCheckoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "order".checkouts
                    ADD COLUMN IF NOT EXISTS shipping_amount numeric(18,2) NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS minimum_delivery_date date NULL,
                    ADD COLUMN IF NOT EXISTS requested_delivery_date date NULL,
                    ADD COLUMN IF NOT EXISTS requested_delivery_time_window character varying(32) NOT NULL DEFAULT '',
                    ADD COLUMN IF NOT EXISTS customer_note character varying(500) NOT NULL DEFAULT '';

                CREATE TABLE IF NOT EXISTS "order".cart_shipping_drafts
                (
                    cart_id uuid NOT NULL,
                    guest_secret_hash character varying(128) NOT NULL,
                    cart_version integer NOT NULL,
                    recipient_name character varying(128) NOT NULL,
                    contact_mobile character varying(32) NOT NULL,
                    province_name character varying(64) NOT NULL,
                    city_name character varying(64) NOT NULL,
                    postal_address character varying(512) NOT NULL,
                    postal_code character varying(16) NOT NULL,
                    saved_address_id uuid NULL,
                    shipping_method_code character varying(64) NOT NULL,
                    shipping_method_label character varying(128) NOT NULL,
                    shipping_amount numeric(18,2) NOT NULL,
                    minimum_delivery_date date NOT NULL,
                    selected_delivery_date date NULL,
                    selected_delivery_time_window character varying(32) NULL,
                    customer_note character varying(500) NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_cart_shipping_drafts PRIMARY KEY (cart_id)
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "order".cart_shipping_drafts;
                ALTER TABLE "order".checkouts
                    DROP COLUMN IF EXISTS shipping_amount,
                    DROP COLUMN IF EXISTS minimum_delivery_date,
                    DROP COLUMN IF EXISTS requested_delivery_date,
                    DROP COLUMN IF EXISTS requested_delivery_time_window,
                    DROP COLUMN IF EXISTS customer_note;
                """);
        }
    }
}
