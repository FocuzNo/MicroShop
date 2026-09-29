using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroShop.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    type = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false),
                    received_at_utc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),
                    processed_at_utc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "pk_inbox_messages",
                        x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    product_id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    quantity = table.Column<int>(
                        type: "integer",
                        nullable: false),
                    created_at_utc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),
                    updated_at_utc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "pk_inventory_items",
                        x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_product_id",
                table: "inventory_items",
                column: "product_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "inventory_items");
        }
    }
}
