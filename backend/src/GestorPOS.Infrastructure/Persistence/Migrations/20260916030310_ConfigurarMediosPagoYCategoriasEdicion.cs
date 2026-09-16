using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurarMediosPagoYCategoriasEdicion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediosPago",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    EsProtegido = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediosPago", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediosPago_TenantId_Nombre",
                table: "MediosPago",
                columns: new[] { "TenantId", "Nombre" },
                unique: true);

            // Siembra los 3 medios de pago por defecto para cada tenant que ya existía antes de
            // esta migración — sin esto, los negocios actuales quedarían sin ningún medio de pago
            // configurado y no podrían vender hasta entrar a Configuración a crearlos a mano.
            migrationBuilder.Sql(@"
                INSERT INTO ""MediosPago"" (""Id"", ""TenantId"", ""Nombre"", ""Activo"", ""EsProtegido"", ""FechaCreacion"")
                SELECT gen_random_uuid(), ""Id"", 'Efectivo', true, true, now() FROM ""Tenants"";

                INSERT INTO ""MediosPago"" (""Id"", ""TenantId"", ""Nombre"", ""Activo"", ""EsProtegido"", ""FechaCreacion"")
                SELECT gen_random_uuid(), ""Id"", 'Tarjeta', true, false, now() FROM ""Tenants"";

                INSERT INTO ""MediosPago"" (""Id"", ""TenantId"", ""Nombre"", ""Activo"", ""EsProtegido"", ""FechaCreacion"")
                SELECT gen_random_uuid(), ""Id"", 'Otro', true, false, now() FROM ""Tenants"";
            ");

            // Convierte Ventas.MedioPago de int (enum) a texto, traduciendo los valores existentes
            // en vez de solo cambiar el tipo — preserva el historial de ventas ya registradas.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Ventas""
                ALTER COLUMN ""MedioPago"" TYPE text
                USING (CASE ""MedioPago""
                    WHEN 1 THEN 'Efectivo'
                    WHEN 2 THEN 'Tarjeta'
                    ELSE 'Otro'
                END);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Ventas""
                ALTER COLUMN ""MedioPago"" TYPE integer
                USING (CASE ""MedioPago""
                    WHEN 'Efectivo' THEN 1
                    WHEN 'Tarjeta' THEN 2
                    ELSE 3
                END);
            ");

            migrationBuilder.DropTable(
                name: "MediosPago");
        }
    }
}
