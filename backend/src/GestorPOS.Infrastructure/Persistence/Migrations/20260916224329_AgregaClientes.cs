using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Ventas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_TenantId_Telefono",
                table: "Clientes",
                columns: new[] { "TenantId", "Telefono" },
                unique: true);

            // Backfill: las ventas históricas ya tenían un TelefonoCliente suelto (texto libre, sin
            // entidad propia). Acá se crea un Cliente por cada teléfono distinto que ya existía por
            // negocio (normalizado a solo dígitos, igual que el resto del sistema), y se enlazan las
            // ventas viejas — así el historial de compras no arranca vacío para los clientes que ya
            // habían comprado antes de este cambio.
            migrationBuilder.Sql(
                """
                INSERT INTO "Clientes" ("Id", "TenantId", "Telefono", "Nombre", "FechaCreacion")
                SELECT gen_random_uuid(), "TenantId", telefono_normalizado, NULL, now()
                FROM (
                    SELECT DISTINCT "TenantId", regexp_replace("TelefonoCliente", '[^0-9]', '', 'g') AS telefono_normalizado
                    FROM "Ventas"
                    WHERE "TelefonoCliente" IS NOT NULL
                      AND regexp_replace("TelefonoCliente", '[^0-9]', '', 'g') <> ''
                ) AS telefonos_distintos;

                UPDATE "Ventas" v
                SET "ClienteId" = c."Id"
                FROM "Clientes" c
                WHERE c."TenantId" = v."TenantId"
                  AND v."TelefonoCliente" IS NOT NULL
                  AND c."Telefono" = regexp_replace(v."TelefonoCliente", '[^0-9]', '', 'g');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Ventas");
        }
    }
}
