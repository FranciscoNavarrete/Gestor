Build started...
Build succeeded.
The Entity Framework tools version '10.0.0' is older than that of the runtime '10.0.12'. Update the tools for the latest features and bug fixes. See https://aka.ms/AAc1fbw for more information.
START TRANSACTION;
ALTER TABLE "Productos" ADD "MarcaId" uuid;
CREATE TABLE "Marcas" (
    "Id" uuid NOT NULL,
    "Nombre" character varying(150) NOT NULL,
    "Activo" boolean NOT NULL,
    "FechaCreacion" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_Marcas" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_Marcas_TenantId_Nombre" ON "Marcas" ("TenantId", "Nombre");
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261020021811_AgregaMarcas', '10.0.12');
COMMIT;
