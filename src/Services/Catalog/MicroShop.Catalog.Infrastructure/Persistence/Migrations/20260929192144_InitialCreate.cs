using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroShop.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    type = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false),
                    content = table.Column<string>(
                        type: "jsonb",
                        nullable: false),
                    occurred_at_utc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false),
                    processed_at_utc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true),
                    error = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "pk_outbox_messages",
                        x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false),
                    price = table.Column<decimal>(
                        type: "numeric(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false),
                    created_at_utc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "pk_products",
                        x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_utc",
                table: "outbox_messages",
                column: "processed_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
