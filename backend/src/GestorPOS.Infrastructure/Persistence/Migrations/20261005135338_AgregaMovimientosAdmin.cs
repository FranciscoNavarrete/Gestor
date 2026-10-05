using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaMovimientosAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimientosAdmin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AdminRol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Accion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Entidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntidadId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntidadNombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosAdmin", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosAdmin_FechaUtc",
                table: "MovimientosAdmin",
                column: "FechaUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosAdmin");
        }
    }
}
