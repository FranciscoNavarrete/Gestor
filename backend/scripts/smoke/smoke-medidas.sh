#!/usr/bin/env bash
# Guion de humo de "Medidas y presentaciones": un petshop (con la función prendida) y un kiosco (sin ella).
# Imprime PASS/FAIL por cada comprobación. Uso: smoke-medidas.sh <base-url>
set -u
BASE="${1:-http://localhost:5090}"
SUF="${SMOKE_SUFIJO:-$(date +%s)}"
H='Content-Type: application/json'
PASS=0; FAIL=0

req() { local m="$1" r="$2" b="${3:-}" t="${4:-}"; local a=(-s -X "$m" "$BASE$r" -H "$H"); [ -n "$t" ] && a+=(-H "Authorization: Bearer $t"); [ -n "$b" ] && a+=(-d "$b"); curl "${a[@]}"; }
# check "descripción" "valor obtenido" "valor esperado"
check() { if [ "$2" = "$3" ]; then PASS=$((PASS+1)); echo "  PASS  $1"; else FAIL=$((FAIL+1)); echo "  FAIL  $1  (obtenido: $2 | esperado: $3)"; fi; }
# num: normaliza números para comparar (3.000 == 3)
num() { jq -n --argjson v "${1:-null}" '$v*1' 2>/dev/null || echo "$1"; }

ADMIN=$(req POST /api/admin/auth/login '{"email":"admin@gestorpos.com","password":"Admin123!"}' | jq -r .token)

nuevo_negocio() { # nombre email -> imprime "tenantId token"
  local t; t=$(req POST /api/admin/tenants/sin-cobro "{\"nombreNegocio\":\"$1\",\"nombreAdmin\":\"Dueno\",\"telefono\":\"1155550002\",\"email\":\"$2\",\"password\":\"Humo1234!\"}" "$ADMIN" | jq -r .id)
  local l; l=$(req POST /api/auth/login "{\"email\":\"$2\",\"password\":\"Humo1234!\"}")
  local tok; tok=$(echo "$l" | jq -r .token)
  echo "$l" | jq -r '.terminosPendientes' | grep -q true && req POST /api/terminos/aceptar "" "$tok" >/dev/null
  echo "$t $tok"
}

echo "== Petshop con la función prendida"
read -r PT PTOK < <(nuevo_negocio "Petshop Humo $SUF" "pet${SUF}@test.local")
req PUT "/api/admin/tenants/$PT/features/medidas" "" "$ADMIN" >/dev/null
req PUT "/api/admin/tenants/$PT/features/compras" "" "$ADMIN" >/dev/null
# el token viejo no trae la feature nueva: volver a entrar
PTOK=$(req POST /api/auth/login "{\"email\":\"pet${SUF}@test.local\",\"password\":\"Humo1234!\"}" | jq -r .token)
req POST /api/caja/abrir '{"montoApertura":5000}' "$PTOK" >/dev/null

P=$(req POST /api/productos "{\"sku\":\"ALI-$SUF\",\"nombre\":\"Alimento perro\",\"categoriaId\":null,\"precio\":4200,\"costo\":2900,\"stockActual\":35.5,\"stockMinimo\":5,\"unidad\":\"Kilo\",\"presentaciones\":[{\"nombre\":\"Bolsa 3 kg\",\"cantidadBase\":3,\"precio\":11500},{\"nombre\":\"Bolsa 15 kg\",\"cantidadBase\":15,\"precio\":58000}]}" "$PTOK")
PID=$(echo "$P" | jq -r .id)
check "producto por kilo se crea con unidad Kilo" "$(echo "$P" | jq -r .unidad)" "Kilo"
check "producto trae sus 2 presentaciones" "$(echo "$P" | jq '.presentaciones|length')" "2"
check "stock inicial 35,5 kg" "$(echo "$P" | jq '.stockActual*1')" "35.5"
B3=$(echo "$P" | jq -r '.presentaciones[]|select(.nombre=="Bolsa 3 kg")|.id')
B15=$(echo "$P" | jq -r '.presentaciones[]|select(.nombre=="Bolsa 15 kg")|.id')

V1=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":0.35}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "venta suelta de 0,35 kg = \$1470" "$(echo "$V1" | jq '.total*1')" "1470"
check "ticket muestra '0,35 kg Alimento perro' (con coma)" "$(echo "$V1" | jq -r .ticketTexto | grep -c '0,35 kg Alimento perro')" "1"

V2=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":2,\"presentacionId\":\"$B15\"}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "2 bolsas de 15 kg = \$116000" "$(echo "$V2" | jq '.total*1')" "116000"
check "ticket muestra la presentación" "$(echo "$V2" | jq -r .ticketTexto | grep -c '2x Alimento perro (Bolsa 15 kg)')" "1"
PS=$(req GET "/api/productos/$PID" "" "$PTOK")
check "stock = 35,5 - 0,35 - 30 = 5,15 kg" "$(echo "$PS" | jq '.stockActual*1')" "5.15"

E1=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":1,\"presentacionId\":\"$B15\"}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "no vende una bolsa de 15 kg con 5,15 kg de stock" "$(echo "$E1" | jq -r '.error|startswith("Stock insuficiente de \u0027Alimento perro\u0027 (disponible: 5,15 kg)")')" "true"
E2=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":3,\"presentacionId\":\"$B3\"},{\"productoId\":\"$PID\",\"cantidad\":1}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "suma suelto + bolsas del mismo producto contra el mismo stock" "$(echo "$E2" | jq -r '.error|startswith("Stock insuficiente de \u0027Alimento perro\u0027")')" "true"
E3=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":0.0005}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "rechaza más de 3 decimales" "$(echo "$E3" | jq -r .error)" "La cantidad admite hasta 3 decimales."
E4=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":1.5,\"presentacionId\":\"$B3\"}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "las presentaciones se venden de a una" "$(echo "$E4" | jq -r '.error|startswith("Las presentaciones se venden de a una")')" "true"

C=$(req POST /api/proveedores '{"nombre":"Distribuidora Pet","cuit":null,"telefono":null,"email":null}' "$PTOK" | jq -r .id)
CP=$(req POST /api/compras "{\"proveedorId\":\"$C\",\"items\":[{\"productoId\":\"$PID\",\"cantidad\":2,\"costoUnitario\":42000,\"presentacionId\":\"$B15\"}]}" "$PTOK")
check "compra de 2 bolsas de 15 kg a \$42000 = \$84000" "$(echo "$CP" | jq '.total*1')" "84000"
PS=$(req GET "/api/productos/$PID" "" "$PTOK")
check "la compra suma 30 kg: 5,15 + 30 = 35,15" "$(echo "$PS" | jq '.stockActual*1')" "35.15"
check "el costo pasa a \$2800 por kg" "$(echo "$PS" | jq '.costo*1')" "2800"

AJ=$(req POST "/api/productos/$PID/ajustar-stock" '{"cantidad":-0.15,"motivo":"merma"}' "$PTOK")
check "ajuste de -0,15 kg" "$(echo "$AJ" | jq '.stockActual*1')" "35"

RK=$(req GET "/api/reportes/ranking-productos" "" "$PTOK")
check "ranking en kilos: 0,35 + 30 = 30,35" "$(echo "$RK" | jq '.[0].cantidadVendida*1')" "30.35"
check "ranking trae la unidad" "$(echo "$RK" | jq -r '.[0].unidad')" "Kilo"
GA=$(req GET "/api/reportes/ganancias" "" "$PTOK")
# ventas: 1470 + 116000 = 117470 ; costo: 0,35*2900=1015 + 2*43500=87000 -> 88015 ; ganancia 29455
check "ganancias: total 117470" "$(echo "$GA" | jq '.totalVentas*1')" "117470"
check "ganancias: costo 88015 (la bolsa cuesta 15 x 2900)" "$(echo "$GA" | jq '.totalCosto*1')" "88015"
MV=$(req GET "/api/movimientos-stock?pagina=1&tamanoPagina=50" "" "$PTOK")
check "movimientos guardan decimales (-0,35)" "$(echo "$MV" | jq '[.items[]|select(.cantidad==-0.35)]|length')" "1"
PDF=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/reportes/stock/pdf" -H "Authorization: Bearer $PTOK")
check "PDF de stock se genera" "$PDF" "200"

# presentaciones: editar, duplicado, desactivar
ED=$(req PUT "/api/productos/$PID/presentaciones/$B3" '{"nombre":"Bolsa 3 kg","cantidadBase":3,"precio":12000}' "$PTOK")
check "editar el precio de una presentación" "$(echo "$ED" | jq '.precio*1')" "12000"
DU=$(req POST "/api/productos/$PID/presentaciones" '{"nombre":"bolsa 3 KG","cantidadBase":3,"precio":1}' "$PTOK")
check "no deja repetir el nombre (sin importar mayúsculas)" "$(echo "$DU" | jq -r .error)" "Ya hay una presentación llamada 'bolsa 3 KG'."
NU=$(req POST "/api/productos/$PID/presentaciones" '{"nombre":"Bolsa 7,5 kg","cantidadBase":7.5,"precio":30000}' "$PTOK")
check "agrega una presentación nueva con el switch prendido" "$(echo "$NU" | jq -r .nombre)" "Bolsa 7,5 kg"
req DELETE "/api/productos/$PID/presentaciones/$B3" "" "$PTOK" >/dev/null
PS=$(req GET "/api/productos/$PID" "" "$PTOK")
check "desactivar una presentación la saca de la lista" "$(echo "$PS" | jq '[.presentaciones[]|select(.nombre=="Bolsa 3 kg")]|length')" "0"

# producto por unidad con presentación "Caja x12"
PU=$(req POST /api/productos "{\"sku\":\"GAS-$SUF\",\"nombre\":\"Gaseosa\",\"categoriaId\":null,\"precio\":1200,\"costo\":800,\"stockActual\":24,\"stockMinimo\":6,\"presentaciones\":[{\"nombre\":\"Caja x12\",\"cantidadBase\":12,\"precio\":12000}]}" "$PTOK")
PUID=$(echo "$PU" | jq -r .id); CAJA=$(echo "$PU" | jq -r '.presentaciones[0].id')
VU=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PUID\",\"cantidad\":1,\"presentacionId\":\"$CAJA\"}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "caja x12 vende \$12000 y descuenta 12" "$(echo "$VU" | jq '.total*1') $(req GET "/api/productos/$PUID" "" "$PTOK" | jq '.stockActual*1')" "12000 12"
EU=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PUID\",\"cantidad\":1.5}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "un producto por unidad rechaza 1,5" "$(echo "$EU" | jq -r .error)" "La cantidad de un producto por unidad tiene que ser un número entero."

echo "== Apagar la función con productos ya medidos"
req DELETE "/api/admin/tenants/$PT/features/medidas" "" "$ADMIN" >/dev/null
PTOK=$(req POST /api/auth/login "{\"email\":\"pet${SUF}@test.local\",\"password\":\"Humo1234!\"}" | jq -r .token)
VA=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":0.5}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "con la función apagada sigue vendiendo por kilo" "$(echo "$VA" | jq '.total*1')" "2100"
VB=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":1,\"presentacionId\":\"$(echo "$NU" | jq -r .id)\"}],\"medioPago\":\"Efectivo\"}" "$PTOK")
check "con la función apagada sigue vendiendo la presentación" "$(echo "$VB" | jq '.total*1')" "30000"
NP=$(req POST /api/productos "{\"sku\":\"X-$SUF\",\"nombre\":\"Nuevo medido\",\"categoriaId\":null,\"precio\":1,\"costo\":1,\"stockActual\":1,\"stockMinimo\":0,\"unidad\":\"Kilo\"}" "$PTOK")
check "con la función apagada no crea productos medidos" "$(echo "$NP" | jq -r .error)" "La función Medidas y presentaciones no está activada para tu negocio."
NQ=$(req POST "/api/productos/$PID/presentaciones" '{"nombre":"Otra","cantidadBase":1,"precio":1}' "$PTOK")
check "con la función apagada no crea presentaciones" "$(echo "$NQ" | jq -r .error)" "La función Medidas y presentaciones no está activada para tu negocio."
EDI=$(req PUT "/api/productos/$PID/presentaciones/$B15" '{"nombre":"Bolsa 15 kg","cantidadBase":15,"precio":59000}' "$PTOK")
check "con la función apagada sí edita el precio de una existente" "$(echo "$EDI" | jq '.precio*1')" "59000"

echo "== Kiosco sin la función"
read -r KT KTOK < <(nuevo_negocio "Kiosco Medidas $SUF" "kio${SUF}@test.local")
K1=$(req POST /api/productos "{\"sku\":\"K-$SUF\",\"nombre\":\"Kilo de pan\",\"categoriaId\":null,\"precio\":1,\"costo\":1,\"stockActual\":1,\"stockMinimo\":0,\"unidad\":\"Kilo\"}" "$KTOK")
check "un kiosco sin la función no puede crear productos por kilo" "$(echo "$K1" | jq -r .error)" "La función Medidas y presentaciones no está activada para tu negocio."
K2=$(req POST /api/productos "{\"sku\":\"K2-$SUF\",\"nombre\":\"Alfajor\",\"categoriaId\":null,\"precio\":500,\"costo\":300,\"stockActual\":50,\"stockMinimo\":10,\"unidad\":\"Unidad\"}" "$KTOK")
check "pedir Unidad explícita sin la función anda igual que siempre" "$(echo "$K2" | jq -r .unidad)" "Unidad"
K3=$(req POST /api/productos "{\"sku\":\"K3-$SUF\",\"nombre\":\"X\",\"categoriaId\":null,\"precio\":1,\"costo\":1,\"stockActual\":1.5,\"stockMinimo\":0}" "$KTOK")
check "stock decimal en un producto por unidad se rechaza" "$(echo "$K3" | jq -r .error)" "El stock actual de un producto por unidad tiene que ser un número entero."
K4=$(req POST /api/productos "{\"sku\":\"K4-$SUF\",\"nombre\":\"X\",\"categoriaId\":null,\"precio\":1,\"costo\":1,\"stockActual\":1,\"stockMinimo\":0,\"unidad\":\"Galon\"}" "$KTOK")
check "unidad inexistente" "$(echo "$K4" | jq -r .error)" "La unidad indicada no existe. Usá Unidad, Kilo, Litro o Metro."

echo
echo "RESULTADO: $PASS pasaron, $FAIL fallaron"
[ "$FAIL" -eq 0 ]
