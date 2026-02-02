using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuditLogsAndSampleItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_audit_logs_tenant_occurred",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_user",
                table: "audit_logs");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "sample_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "sample_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "audit_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                table: "audit_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_occurred",
                table: "audit_logs",
                columns: new[] { "tenant_id", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_user_occurred",
                table: "audit_logs",
                columns: new[] { "tenant_id", "user_id", "occurred_at" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_audit_logs_tenant_occurred",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_tenant_user_occurred",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "sample_items");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "sample_items");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "audit_logs");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_occurred",
                table: "audit_logs",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user",
                table: "audit_logs",
                column: "user_id");
        }
    }
}
