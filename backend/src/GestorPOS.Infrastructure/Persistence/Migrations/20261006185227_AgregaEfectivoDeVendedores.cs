using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaEfectivoDeVendedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EfectivoCompensado",
                table: "Liquidaciones",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "MovimientosEfectivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Nota = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LiquidacionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RegistradoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistradoPorNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaRegistroUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosEfectivo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosEfectivo_LiquidacionId",
                table: "MovimientosEfectivo",
                column: "LiquidacionId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosEfectivo_VendedorId",
                table: "MovimientosEfectivo",
                column: "VendedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosEfectivo");

            migrationBuilder.DropColumn(
                name: "EfectivoCompensado",
                table: "Liquidaciones");
        }
    }
}
