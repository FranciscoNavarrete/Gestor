using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaPagosManuales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PendienteActivacion",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PagosManuales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Metodo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaRecepcion = table.Column<DateOnly>(type: "date", nullable: true),
                    Nota = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RegistradoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistradoPorNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegistradoPorRol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaRegistroUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmadoPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmadoPorNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FechaConfirmacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosManuales", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagosManuales_TenantId",
                table: "PagosManuales",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosManuales");

            migrationBuilder.DropColumn(
                name: "PendienteActivacion",
                table: "Tenants");
        }
    }
}
