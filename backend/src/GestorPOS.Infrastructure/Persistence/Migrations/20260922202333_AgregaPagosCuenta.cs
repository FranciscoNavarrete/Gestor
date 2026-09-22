using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaPagosCuenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagosCuenta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    MedioPago = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosCuenta", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagosCuenta_TenantId_ClienteId",
                table: "PagosCuenta",
                columns: new[] { "TenantId", "ClienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_PagosCuenta_TenantId_FechaCreacion",
                table: "PagosCuenta",
                columns: new[] { "TenantId", "FechaCreacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosCuenta");
        }
    }
}
