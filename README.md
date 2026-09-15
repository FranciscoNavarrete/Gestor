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
- `GET /health` — health check (usado por Railway)

## Roadmap

Ver plan de fases completo en la memoria del proyecto. Resumen:

0. ✅ Setup + núcleo multi-tenant + Auth
1. Gestión de Stock (productos, categorías, alertas de stock mínimo, precios masivos)
2. Ventas/POS (venta rápida, cobro, ticket digital por WhatsApp/Email, descuento de stock)
3. Reportes (caja diaria, ranking de productos, ganancias netas, dashboard)
4. Panel interno de feature flags por tenant
5. Deploy productivo (Railway + Netlify) + pulido
