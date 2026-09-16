using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaDatosDeNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "Tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoData",
                table: "Tenants",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Tenants",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LogoData",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Tenants");
        }
    }
}
