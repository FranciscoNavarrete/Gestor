#!/usr/bin/env bash
# Prueba del catálogo de marcas (igual que categorías) y de la marca en productos, búsqueda y Excel.
set -u
BASE="${1:-http://localhost:5090}"; SUF="${SMOKE_SUFIJO:-mk$(date +%s)}"; H='Content-Type: application/json'
req() { local m="$1" r="$2" b="${3:-}" t="${4:-}"; local a=(-s -X "$m" "$BASE$r" -H "$H"); [ -n "$t" ] && a+=(-H "Authorization: Bearer $t"); [ -n "$b" ] && a+=(-d "$b"); curl "${a[@]}"; }
PASS=0; FAIL=0
check() { if [ "$2" = "$3" ]; then PASS=$((PASS+1)); echo "  PASS  $1"; else FAIL=$((FAIL+1)); echo "  FAIL  $1  (obtenido: $2 | esperado: $3)"; fi; }
for i in $(seq 1 15); do c=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE/api/admin/auth/login -H "$H" -d '{"email":"admin@gestorpos.com","password":"Admin123!"}'); [ "$c" = "200" ] && break; sleep 2; done
ADMIN=$(req POST /api/admin/auth/login '{"email":"admin@gestorpos.com","password":"Admin123!"}' | jq -r .token)
T=$(req POST /api/admin/tenants/sin-cobro "{\"nombreNegocio\":\"Marcas $SUF\",\"nombreAdmin\":\"D\",\"telefono\":\"1155550004\",\"email\":\"marcas${SUF}@test.local\",\"password\":\"Humo1234!\"}" "$ADMIN" | jq -r .id)
L=$(req POST /api/auth/login "{\"email\":\"marcas${SUF}@test.local\",\"password\":\"Humo1234!\"}"); K=$(echo "$L" | jq -r .token)
echo "$L" | jq -r .terminosPendientes | grep -q true && req POST /api/terminos/aceptar "" "$K" >/dev/null

M1=$(req POST /api/marcas '{"nombre":"Royal Canin"}' "$K"); M1ID=$(echo "$M1" | jq -r .id)
check "crea una marca" "$(echo "$M1" | jq -r .nombre)" "Royal Canin"
D=$(req POST /api/marcas '{"nombre":"royal canin"}' "$K")
check "no deja repetir (sin importar mayúsculas)" "$(echo "$D" | jq -r .error)" "Ya hay una marca llamada 'royal canin'."
V=$(req POST /api/marcas '{"nombre":"   "}' "$K")
check "rechaza una marca vacía" "$(echo "$V" | jq -r .error)" "El nombre de la marca es obligatorio."
M2=$(req POST /api/marcas '{"nombre":"Pedigree"}' "$K"); M2ID=$(echo "$M2" | jq -r .id)
ED=$(req PUT "/api/marcas/$M2ID" '{"nombre":"Pedigree Pro"}' "$K")
check "renombra una marca" "$(echo "$ED" | jq -r .nombre)" "Pedigree Pro"
check "lista las 2 marcas" "$(req GET /api/marcas "" "$K" | jq 'length')" "2"

P=$(req POST /api/productos "{\"sku\":\"ALI-$SUF\",\"nombre\":\"Alimento adulto\",\"categoriaId\":null,\"precio\":4200,\"costo\":2900,\"stockActual\":10,\"stockMinimo\":2,\"marcaId\":\"$M1ID\"}" "$K"); PID=$(echo "$P" | jq -r .id)
check "producto con marca" "$(echo "$P" | jq -r '.marcaNombre')" "Royal Canin"
P0=$(req POST /api/productos "{\"sku\":\"SIN-$SUF\",\"nombre\":\"Sin marca\",\"categoriaId\":null,\"precio\":100,\"costo\":50,\"stockActual\":5,\"stockMinimo\":1}" "$K")
check "la marca es opcional" "$(echo "$P0" | jq -r '.marcaId')" "null"
BAD=$(req POST /api/productos "{\"sku\":\"BAD-$SUF\",\"nombre\":\"X\",\"categoriaId\":null,\"precio\":1,\"costo\":1,\"stockActual\":1,\"stockMinimo\":0,\"marcaId\":\"00000000-0000-0000-0000-000000000001\"}" "$K")
check "marca inexistente se rechaza" "$(echo "$BAD" | jq -r .error)" "La marca indicada no existe."

S=$(req GET "/api/productos/buscar?busqueda=royal&bajoStock=false&soloInactivos=false&pagina=1&tamanoPagina=20" "" "$K")
check "la búsqueda encuentra por marca" "$(echo "$S" | jq -r '[.items[].nombre]|join(",")')" "Alimento adulto"
LST=$(req GET /api/productos "" "$K")
check "el listado trae la marca" "$(echo "$LST" | jq -r '[.[]|select(.sku|startswith("ALI"))|.marcaNombre]|first')" "Royal Canin"

EP=$(req PUT "/api/productos/$PID" "{\"nombre\":\"Alimento adulto\",\"categoriaId\":null,\"precio\":4200,\"costo\":2900,\"stockMinimo\":2,\"marcaId\":\"$M2ID\"}" "$K")
check "cambia la marca de un producto" "$(echo "$EP" | jq -r .marcaNombre)" "Pedigree Pro"
EQ=$(req PUT "/api/productos/$PID" "{\"nombre\":\"Alimento adulto\",\"categoriaId\":null,\"precio\":4200,\"costo\":2900,\"stockMinimo\":2,\"marcaId\":null}" "$K")
check "saca la marca de un producto" "$(echo "$EQ" | jq -r .marcaId)" "null"
req PUT "/api/productos/$PID" "{\"nombre\":\"Alimento adulto\",\"categoriaId\":null,\"precio\":4200,\"costo\":2900,\"stockMinimo\":2,\"marcaId\":\"$M1ID\"}" "$K" >/dev/null

EL=$(req DELETE "/api/marcas/$M1ID/permanente" "" "$K")
check "no borra una marca ya usada" "$(echo "$EL" | jq -r '.error|startswith("\"Royal Canin\" ya se usó")')" "true"
DESACT=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE/api/marcas/$M1ID" -H "Authorization: Bearer $K")
check "desactiva la marca" "$DESACT" "204"
check "queda inactiva en el listado" "$(req GET /api/marcas "" "$K" | jq -r '[.[]|select(.nombre=="Royal Canin")|.activo]|first')" "false"
check "reactiva la marca" "$(req POST "/api/marcas/$M1ID/activar" "" "$K" | jq -r .activo)" "true"
M3ID=$(req POST /api/marcas '{"nombre":"Sin uso"}' "$K" | jq -r .id)
BR=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE/api/marcas/$M3ID/permanente" -H "Authorization: Bearer $K")
check "borra una marca que nunca se usó" "$BR" "204"

# Aislamiento entre negocios
T2=$(req POST /api/admin/tenants/sin-cobro "{\"nombreNegocio\":\"Otro $SUF\",\"nombreAdmin\":\"D\",\"telefono\":\"1155550005\",\"email\":\"otro${SUF}@test.local\",\"password\":\"Humo1234!\"}" "$ADMIN" >/dev/null)
L2=$(req POST /api/auth/login "{\"email\":\"otro${SUF}@test.local\",\"password\":\"Humo1234!\"}"); K2=$(echo "$L2" | jq -r .token)
echo "$L2" | jq -r .terminosPendientes | grep -q true && req POST /api/terminos/aceptar "" "$K2" >/dev/null
check "otro negocio no ve mis marcas" "$(req GET /api/marcas "" "$K2" | jq 'length')" "0"
BAD2=$(req POST /api/productos "{\"sku\":\"X-$SUF\",\"nombre\":\"X\",\"categoriaId\":null,\"precio\":1,\"costo\":1,\"stockActual\":1,\"stockMinimo\":0,\"marcaId\":\"$M1ID\"}" "$K2")
check "otro negocio no puede usar mi marca" "$(echo "$BAD2" | jq -r .error)" "La marca indicada no existe."

echo; echo "RESULTADO: $PASS pasaron, $FAIL fallaron"; [ "$FAIL" -eq 0 ]
