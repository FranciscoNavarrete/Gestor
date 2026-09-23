using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaFacturasProveedorYCuit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cuit",
                table: "Proveedores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacturasProveedor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProveedorNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NumeroFactura = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasProveedor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PagosFacturaProveedor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FacturaProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    MedioPago = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosFacturaProveedor", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturasProveedor");

            migrationBuilder.DropTable(
                name: "PagosFacturaProveedor");

            migrationBuilder.DropColumn(
                name: "Cuit",
                table: "Proveedores");
        }
    }
}
