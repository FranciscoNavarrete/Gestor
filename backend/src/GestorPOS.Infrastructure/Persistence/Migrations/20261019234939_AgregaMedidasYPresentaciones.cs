using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaMedidasYPresentaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Cantidad",
                table: "VentaItems",
                type: "numeric(12,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "FactorBase",
                table: "VentaItems",
                type: "numeric(12,3)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "PresentacionNombre",
                table: "VentaItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Unidad",
                table: "VentaItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "StockMinimo",
                table: "Productos",
                type: "numeric(12,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "StockActual",
                table: "Productos",
                type: "numeric(12,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Unidad",
                table: "Productos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "StockResultante",
                table: "MovimientosStock",
                type: "numeric(12,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Cantidad",
                table: "MovimientosStock",
                type: "numeric(12,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Cantidad",
                table: "CompraItems",
                type: "numeric(12,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "FactorBase",
                table: "CompraItems",
                type: "numeric(12,3)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "PresentacionNombre",
                table: "CompraItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Unidad",
                table: "CompraItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProductoPresentaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CantidadBase = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoPresentaciones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoPresentaciones_TenantId_ProductoId",
                table: "ProductoPresentaciones",
                columns: new[] { "TenantId", "ProductoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductoPresentaciones");

            migrationBuilder.DropColumn(
                name: "FactorBase",
                table: "VentaItems");

            migrationBuilder.DropColumn(
                name: "PresentacionNombre",
                table: "VentaItems");

            migrationBuilder.DropColumn(
                name: "Unidad",
                table: "VentaItems");

            migrationBuilder.DropColumn(
                name: "Unidad",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "FactorBase",
                table: "CompraItems");

            migrationBuilder.DropColumn(
                name: "PresentacionNombre",
                table: "CompraItems");

            migrationBuilder.DropColumn(
                name: "Unidad",
                table: "CompraItems");

            migrationBuilder.AlterColumn<int>(
                name: "Cantidad",
                table: "VentaItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)");

            migrationBuilder.AlterColumn<int>(
                name: "StockMinimo",
                table: "Productos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)");

            migrationBuilder.AlterColumn<int>(
                name: "StockActual",
                table: "Productos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)");

            migrationBuilder.AlterColumn<int>(
                name: "StockResultante",
                table: "MovimientosStock",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)");

            migrationBuilder.AlterColumn<int>(
                name: "Cantidad",
                table: "MovimientosStock",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)");

            migrationBuilder.AlterColumn<int>(
                name: "Cantidad",
                table: "CompraItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)");
        }
    }
}
