using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OutboxAndDemoTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_messages");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_status_attempts",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "demo_event_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_event_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status_occurred",
                table: "outbox_messages",
                columns: new[] { "status", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_demo_event_logs_TenantId",
                table: "demo_event_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_demo_event_logs_TenantId_CreatedAt",
                table: "demo_event_logs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_demo_event_logs_tenant_message",
                table: "demo_event_logs",
                columns: new[] { "TenantId", "MessageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demo_event_logs");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_status_occurred",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "outbox_messages");

            migrationBuilder.CreateTable(
                name: "processed_messages",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_messages", x => new { x.tenant_id, x.message_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status_attempts",
                table: "outbox_messages",
                columns: new[] { "status", "attempts" });

            migrationBuilder.CreateIndex(
                name: "ix_processed_messages_tenant_message",
                table: "processed_messages",
                columns: new[] { "tenant_id", "message_id" },
                unique: true);
        }
    }
}
