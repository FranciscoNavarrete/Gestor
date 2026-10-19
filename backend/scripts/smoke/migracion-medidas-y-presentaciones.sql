Build started...
Build succeeded.
The Entity Framework tools version '10.0.0' is older than that of the runtime '10.0.12'. Update the tools for the latest features and bug fixes. See https://aka.ms/AAc1fbw for more information.
START TRANSACTION;
ALTER TABLE "VentaItems" ALTER COLUMN "Cantidad" TYPE numeric(12,3);
ALTER TABLE "VentaItems" ADD "FactorBase" numeric(12,3) NOT NULL DEFAULT 1.0;
ALTER TABLE "VentaItems" ADD "PresentacionNombre" character varying(100);
ALTER TABLE "VentaItems" ADD "Unidad" integer NOT NULL DEFAULT 0;
ALTER TABLE "Productos" ALTER COLUMN "StockMinimo" TYPE numeric(12,3);
ALTER TABLE "Productos" ALTER COLUMN "StockActual" TYPE numeric(12,3);
ALTER TABLE "Productos" ADD "Unidad" integer NOT NULL DEFAULT 0;
ALTER TABLE "MovimientosStock" ALTER COLUMN "StockResultante" TYPE numeric(12,3);
ALTER TABLE "MovimientosStock" ALTER COLUMN "Cantidad" TYPE numeric(12,3);
ALTER TABLE "CompraItems" ALTER COLUMN "Cantidad" TYPE numeric(12,3);
ALTER TABLE "CompraItems" ADD "FactorBase" numeric(12,3) NOT NULL DEFAULT 1.0;
ALTER TABLE "CompraItems" ADD "PresentacionNombre" character varying(100);
ALTER TABLE "CompraItems" ADD "Unidad" integer NOT NULL DEFAULT 0;
CREATE TABLE "ProductoPresentaciones" (
    "Id" uuid NOT NULL,
    "ProductoId" uuid NOT NULL,
    "Nombre" character varying(100) NOT NULL,
    "CantidadBase" numeric(12,3) NOT NULL,
    "Precio" numeric(12,2) NOT NULL,
    "Activo" boolean NOT NULL,
    "FechaCreacion" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_ProductoPresentaciones" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_ProductoPresentaciones_TenantId_ProductoId" ON "ProductoPresentaciones" ("TenantId", "ProductoId");
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261019234939_AgregaMedidasYPresentaciones', '10.0.12');
COMMIT;
