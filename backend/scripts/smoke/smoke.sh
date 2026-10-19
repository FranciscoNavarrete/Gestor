#!/usr/bin/env bash
# Guion de humo de GestorPOS: recorre un kiosco de punta a punta (alta sin cobro, productos, caja, venta,
# compra, ajuste de stock y reportes) y guarda salidas "normalizadas" (sin ids ni fechas) para comparar
# la versión de hoy contra la nueva. Uso: smoke.sh <base-url> <carpeta-salida>
set -u
BASE="${1:-http://localhost:5090}"
OUT="${2:-./out}"
mkdir -p "$OUT"
SUF="${SMOKE_SUFIJO:-$(date +%s)}"
EMAIL="humo${SUF}@test.local"
PASS="Humo1234!"
H='Content-Type: application/json'

req() { # metodo ruta [cuerpo] [token]
  local m="$1" r="$2" b="${3:-}" t="${4:-}"
  local args=(-s -X "$m" "$BASE$r" -H "$H")
  [ -n "$t" ] && args+=(-H "Authorization: Bearer $t")
  [ -n "$b" ] && args+=(-d "$b")
  curl "${args[@]}"
}

norm() { # quita ids, fechas y tokens para poder comparar
  jq -S 'walk(if type=="object" then with_entries(select(.key|test("(^id$|Id$|^fecha|Fecha|Utc$|token|Token)";"i")|not)) else . end)'
}

ADMIN=$(req POST /api/admin/auth/login '{"email":"admin@gestorpos.com","password":"Admin123!"}' | jq -r .token)
[ "$ADMIN" = "null" ] || [ -z "$ADMIN" ] && { echo "FALLO login operador"; exit 1; }

TEN=$(req POST /api/admin/tenants/sin-cobro "{\"nombreNegocio\":\"Kiosco Humo $SUF\",\"nombreAdmin\":\"Dueno Humo\",\"telefono\":\"1155550001\",\"email\":\"$EMAIL\",\"password\":\"$PASS\",\"nota\":\"guion de humo\"}" "$ADMIN")
TID=$(echo "$TEN" | jq -r '.id // .tenantId')
[ -z "$TID" ] || [ "$TID" = "null" ] && { echo "FALLO alta: $TEN"; exit 1; }
req PUT "/api/admin/tenants/$TID/features/compras" "" "$ADMIN" >/dev/null

LOGIN=$(req POST /api/auth/login "{\"email\":\"$EMAIL\",\"password\":\"$PASS\"}")
TOK=$(echo "$LOGIN" | jq -r .token)
[ "$TOK" = "null" ] && { echo "FALLO login negocio: $LOGIN"; exit 1; }
echo "$LOGIN" | jq -r '.terminosPendientes' | grep -q true && req POST /api/terminos/aceptar "" "$TOK" >/dev/null
echo "$LOGIN" | jq '{features,terminosPendientes,debeCambiarPassword}' | norm > "$OUT/00-login.json"

CAT=$(req POST /api/categorias '{"nombre":"Bebidas"}' "$TOK" | jq -r .id)
P1=$(req POST /api/productos "{\"sku\":\"GAS-$SUF\",\"nombre\":\"Gaseosa 500\",\"categoriaId\":\"$CAT\",\"precio\":1200,\"costo\":800,\"stockActual\":24,\"stockMinimo\":6}" "$TOK")
P2=$(req POST /api/productos "{\"sku\":\"ALF-$SUF\",\"nombre\":\"Alfajor\",\"categoriaId\":null,\"precio\":500,\"costo\":300,\"stockActual\":50,\"stockMinimo\":10}" "$TOK")
P1ID=$(echo "$P1" | jq -r .id); P2ID=$(echo "$P2" | jq -r .id)
{ echo "$P1"; echo "$P2"; } | norm > "$OUT/01-productos-creados.json"

req POST /api/caja/abrir '{"montoApertura":10000}' "$TOK" | norm > "$OUT/02-caja-abrir.json"
req POST /api/ventas "{\"items\":[{\"productoId\":\"$P1ID\",\"cantidad\":3},{\"productoId\":\"$P2ID\",\"cantidad\":10}],\"medioPago\":\"Efectivo\"}" "$TOK" | norm > "$OUT/03-venta.json"

PROV=$(req POST /api/proveedores '{"nombre":"Distribuidora Humo","cuit":null,"telefono":null,"email":null}' "$TOK" | jq -r .id)
req POST /api/compras "{\"proveedorId\":\"$PROV\",\"items\":[{\"productoId\":\"$P1ID\",\"cantidad\":12,\"costoUnitario\":850}]}" "$TOK" | norm > "$OUT/04-compra.json"
req POST "/api/productos/$P2ID/ajustar-stock" '{"cantidad":-5,"motivo":"merma"}' "$TOK" | norm > "$OUT/05-ajuste.json"

req GET "/api/productos/$P1ID" "" "$TOK" | norm > "$OUT/06-producto1-final.json"
req GET "/api/productos/$P2ID" "" "$TOK" | norm > "$OUT/07-producto2-final.json"
req GET "/api/movimientos-stock?pagina=1&tamanoPagina=50" "" "$TOK" | norm > "$OUT/08-movimientos.json"
req GET "/api/reportes/ranking-productos" "" "$TOK" | norm > "$OUT/09-ranking.json"
req GET "/api/reportes/ganancias" "" "$TOK" | norm > "$OUT/10-ganancias.json"
req GET "/api/reportes/dashboard" "" "$TOK" | norm > "$OUT/11-dashboard.json"
curl -s -o "$OUT/12-stock.pdf" -w "%{http_code} %{size_download}\n" "$BASE/api/reportes/stock/pdf" -H "Authorization: Bearer $TOK" > "$OUT/12-stock-pdf.txt"
sed -i '' 's/ [0-9]*$//' "$OUT/12-stock-pdf.txt" 2>/dev/null
rm -f "$OUT/12-stock.pdf"

# venta con stock insuficiente: debe seguir rechazándose
req POST /api/ventas "{\"items\":[{\"productoId\":\"$P1ID\",\"cantidad\":9999}],\"medioPago\":\"Efectivo\"}" "$TOK" | norm > "$OUT/13-venta-sin-stock.json"

echo "OK $EMAIL -> $OUT"
