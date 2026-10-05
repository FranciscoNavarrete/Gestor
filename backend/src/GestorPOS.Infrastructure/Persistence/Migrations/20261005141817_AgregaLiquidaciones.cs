using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaLiquidaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Liquidaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendedorNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaCierreUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CerradaPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CerradaPorNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaPago = table.Column<DateOnly>(type: "date", nullable: true),
                    PagadaPorNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Nota = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TotalComision = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalBono = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Liquidaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiquidacionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LiquidacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaAltaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Comision = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Bono = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiquidacionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiquidacionItems_Liquidaciones_LiquidacionId",
                        column: x => x.LiquidacionId,
                        principalTable: "Liquidaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Liquidaciones_VendedorId_Anio_Mes",
                table: "Liquidaciones",
                columns: new[] { "VendedorId", "Anio", "Mes" });

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionItems_LiquidacionId",
                table: "LiquidacionItems",
                column: "LiquidacionId");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionItems_TenantId",
                table: "LiquidacionItems",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiquidacionItems");

            migrationBuilder.DropTable(
                name: "Liquidaciones");
        }
    }
}
