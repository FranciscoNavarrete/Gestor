# Guion de humo

Recorre un kiosco de punta a punta contra un backend local (alta sin cobro, productos, caja, venta, compra,
ajuste de stock y reportes) y guarda las respuestas sin ids ni fechas para comparar versiones.

    # backend local con una base propia (NO la base de desarrollo)
    SMOKE_SUFIJO=run1 backend/scripts/smoke/smoke.sh http://localhost:5090 /tmp/humo-antes
    SMOKE_SUFIJO=run2 backend/scripts/smoke/smoke.sh http://localhost:5090 /tmp/humo-despues
    backend/scripts/smoke/compare.sh /tmp/humo-antes /tmp/humo-despues

Usa el operador inicial de desarrollo (`admin@gestorpos.com`) y crea un negocio de prueba nuevo en cada corrida.
Requiere `curl` y `jq`. El sufijo debe ser `base<N>` o `run<N>` para que el comparador lo ignore.

## Medidas y presentaciones

    backend/scripts/smoke/smoke-medidas.sh http://localhost:5090      # petshop, caja x12, switch apagado, kiosco
    backend/scripts/smoke/import-test.sh http://localhost:5090        # Excel con columna Unidad (requiere openpyxl)

`migracion-medidas-y-presentaciones.sql` es el SQL que genera la migración `AgregaMedidasYPresentaciones`,
para revisarlo antes de desplegar. Su `Down` solo es seguro si todavía no hay cantidades con decimales.

## Marcas

    backend/scripts/smoke/smoke-marcas.sh http://localhost:5090       # catálogo de marcas, marca en productos y búsqueda

`migracion-marcas.sql` es el SQL de la migración `AgregaMarcas` (una columna `MarcaId` nullable y la tabla `Marcas`).
