using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroSolution.Core.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class _001_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "iot_data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_type = table.Column<int>(type: "integer", nullable: false),
                    raw_data = table.Column<string>(type: "text", nullable: false),
                    device_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processing_status = table.Column<int>(type: "integer", nullable: false),
                    processing_queue_id = table.Column<string>(type: "text", nullable: true),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processing_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iot_data", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    ProducerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CropType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    AreaInHectares = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plots_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_iot_data_plot_id",
                table: "iot_data",
                column: "plot_id");

            migrationBuilder.CreateIndex(
                name: "ix_iot_data_plot_timestamp",
                table: "iot_data",
                columns: new[] { "plot_id", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_iot_data_processing_status",
                table: "iot_data",
                column: "processing_status");

            migrationBuilder.CreateIndex(
                name: "ix_iot_data_received_at",
                table: "iot_data",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "IX_Plots_PropertyId",
                table: "Plots",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "iot_data");

            migrationBuilder.DropTable(
                name: "Plots");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
