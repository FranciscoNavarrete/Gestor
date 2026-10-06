using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaAceptacionesTerminos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AceptacionesTerminos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AceptacionesTerminos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceptacionesTerminos_TenantId_Version",
                table: "AceptacionesTerminos",
                columns: new[] { "TenantId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AceptacionesTerminos");
        }
    }
}
