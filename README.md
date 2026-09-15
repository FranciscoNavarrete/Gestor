# GestorPOS

Sistema de Gestión y Punto de Venta (POS) 100% digital — multi-tenant, mobile & desktop.

## Stack

- **Backend**: .NET 10, Clean Architecture (`Domain` / `Application` / `Infrastructure` / `WebAPI`)
- **DB**: PostgreSQL, EF Core con *global query filters* por `TenantId`
- **Auth**: JWT (tenant + rol + feature flags embebidos en el token)
- **Frontend**: Angular standalone + Angular Material (pendiente de scaffold)
- **Deploy**: Railway (API + Postgres) + Netlify (Angular)

## Arquitectura multi-tenant

Una sola base de código y una sola base de datos sirven a todos los negocios. Cada entidad de negocio
hereda de `TenantEntity` y EF Core aplica un filtro global automático por `TenantId` — ninguna query
puede ver datos de otro negocio por error.

Pedidos custom de un cliente se resuelven con **feature flags por tenant** (`TenantFeature`), nunca
forkeando el código: se activa la funcionalidad solo para ese negocio, el resto no la ve hasta que se
decida ofrecerla.

## Nota sobre .NET 10 en este Mac

El build **arm64** nativo de .NET 10 (SDK 10.0.401) es rechazado por macOS en este equipo
(`Taskgated Invalid Signature` — bug de compatibilidad con esta versión de macOS). Se usa en su lugar
el build **x64 vía Rosetta**, instalado en `~/.dotnet-x64`, que funciona sin problemas. El `~/.dotnet`
(arm64) original con .NET 8 sigue intacto para el resto de tus proyectos.

Para trabajar en este proyecto, exportá esto en tu terminal (o agregalo a tu `.zshrc` si vas a laburar
seguido acá):

```bash
export PATH="$HOME/.dotnet-x64:$HOME/.dotnet/tools:$PATH"
export DOTNET_ROOT="$HOME/.dotnet-x64"
export DOTNET_ROOT_X64="$HOME/.dotnet-x64"
```

## Correr en local

1. Levantar Postgres (puerto **5433**, para no chocar con otros proyectos que ya usan 5432):

```bash
docker run -d --name gestorpos-postgres \
  -e POSTGRES_DB=gestorpos -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres \
  -p 5433:5432 -v gestorpos-postgres-data:/var/lib/postgresql/data \
  postgres:16-alpine
```

2. Aplicar migraciones (con el PATH exportado arriba):

```bash
cd backend
dotnet-ef database update \
  --project src/GestorPOS.Infrastructure/GestorPOS.Infrastructure.csproj \
  --startup-project src/GestorPOS.WebAPI/GestorPOS.WebAPI.csproj
```

3. Correr la API:

```bash
cd backend/src/GestorPOS.WebAPI
dotnet run --urls http://localhost:5080
```

Swagger en `http://localhost:5080/swagger` (solo en Development).

## Endpoints actuales

- `POST /api/auth/registro-negocio` — crea un negocio (tenant) + su usuario admin, devuelve JWT
- `POST /api/auth/login` — login por email/password, devuelve JWT
- `GET /api/usuarios` — lista usuarios del negocio del token (requiere `Authorization: Bearer <token>`)
- `GET/POST/PUT/DELETE /api/categorias` — CRUD de categorías (delete = baja lógica)
- `GET /api/productos?bajoStock=true` — lista productos activos; el filtro trae solo los que están en o bajo su stock mínimo
- `GET/POST/PUT/DELETE /api/productos` — CRUD de productos (SKU único por negocio, delete = baja lógica)
- `POST /api/productos/{id}/ajustar-stock` — suma o resta stock manualmente (reposición, merma, corrección)
- `POST /api/productos/actualizar-precios-masivo` — sube/baja el precio de todos los productos activos (o de una categoría) un `porcentaje` dado
- `GET /api/ventas?fecha=2026-09-15` — lista resumida de ventas (todas, o filtradas por día)
- `GET /api/ventas/{id}` — detalle de una venta con items, ticket de texto y link de WhatsApp
- `POST /api/ventas` — registra una venta (items + medioPago + telefonoCliente opcional), descuenta stock automáticamente y devuelve el ticket + link `wa.me` listo para enviar
- `GET /api/caja/actual` — la caja abierta en este momento (o `null` si no hay ninguna)
- `POST /api/caja/abrir` — abre caja con un monto inicial (falla si ya hay una abierta)
- `POST /api/caja/cerrar` — cierra la caja abierta: calcula el monto esperado (apertura + ventas en efectivo del período) contra el monto real contado, y la diferencia
- `GET /api/reportes/ranking-productos?desde=&hasta=&top=10` — productos más vendidos por cantidad, en un rango de fechas opcional
- `GET /api/reportes/ganancias?desde=&hasta=` — ventas, costo y ganancia neta en un rango de fechas
- `GET /api/reportes/dashboard` — resumen: ventas/ganancia de hoy, ventas del mes, productos en alerta de stock, producto más vendido del día
- `GET /health` — health check (usado por Railway)

### Panel interno (no es para los negocios, es para vos)

Protegido por header `X-Admin-Api-Key` (configurable en `Admin:ApiKey`), no por JWT — es un canal de auth
separado porque este panel cruza todos los tenants a propósito.

- `GET /api/admin/tenants` — lista todos los negocios registrados
- `GET /api/admin/tenants/{tenantId}/features` — features activados/desactivados de un negocio
- `PUT /api/admin/tenants/{tenantId}/features/{clave}` — activa un feature para ese negocio (lo crea si no existía)
- `DELETE /api/admin/tenants/{tenantId}/features/{clave}` — lo desactiva

El feature aparece/desaparece del claim `feature` del JWT en el próximo login de ese negocio — así el
código (backend o frontend) puede chequear `TieneFeature("clave")` para mostrar/habilitar algo puntual
que le pediste a un cliente, sin que el resto de los negocios lo vean.

## Roadmap

Ver plan de fases completo en la memoria del proyecto. Resumen:

0. ✅ Setup + núcleo multi-tenant + Auth
1. ✅ Gestión de Stock (productos, categorías, alertas de stock mínimo, precios masivos)
2. ✅ Ventas/POS (venta rápida, cobro, ticket digital vía link de WhatsApp, descuento de stock)
3. ✅ Reportes (caja diaria, ranking de productos, ganancias netas, dashboard)
4. ✅ Panel interno de feature flags por tenant
5. Deploy productivo (Railway + Netlify) + pulido — o arrancar el frontend Angular
