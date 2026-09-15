FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# Copiar .csproj por separado para aprovechar cache de capas
COPY backend/src/GestorPOS.WebAPI/GestorPOS.WebAPI.csproj                 GestorPOS.WebAPI/
COPY backend/src/GestorPOS.Application/GestorPOS.Application.csproj       GestorPOS.Application/
COPY backend/src/GestorPOS.Domain/GestorPOS.Domain.csproj                 GestorPOS.Domain/
COPY backend/src/GestorPOS.Infrastructure/GestorPOS.Infrastructure.csproj GestorPOS.Infrastructure/

RUN dotnet restore "GestorPOS.WebAPI/GestorPOS.WebAPI.csproj"

# Copiar el código fuente
COPY backend/src/GestorPOS.WebAPI/         GestorPOS.WebAPI/
COPY backend/src/GestorPOS.Application/    GestorPOS.Application/
COPY backend/src/GestorPOS.Domain/         GestorPOS.Domain/
COPY backend/src/GestorPOS.Infrastructure/ GestorPOS.Infrastructure/

RUN dotnet publish "GestorPOS.WebAPI/GestorPOS.WebAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# El puerto real lo define Program.cs leyendo la variable PORT en runtime (Railway la
# asigna dinámicamente) — este ENV era una interpolación de shell que Docker no resuelve
# en tiempo de build, así que quedaba siempre fijo en 8080 sin importar el PORT real.
EXPOSE 8080

ENTRYPOINT ["dotnet", "GestorPOS.WebAPI.dll"]
