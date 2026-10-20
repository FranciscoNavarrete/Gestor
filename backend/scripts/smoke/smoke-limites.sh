#!/usr/bin/env bash
# Casos límite de "Medidas y presentaciones": cantidades inválidas, desbordes, aislamiento entre negocios,
# presentaciones inactivas o ajenas, cuenta corriente, redondeos y ventas simultáneas.
# Imprime PASS/FAIL por comprobación. Uso: smoke-limites.sh <base-url>
set -u
BASE="${1:-http://localhost:5090}"
SUF="${SMOKE_SUFIJO:-$(date +%s)}"
H='Content-Type: application/json'
PASS=0; FAIL=0

req() { local m="$1" r="$2" b="${3:-}" t="${4:-}"; local a=(-s -X "$m" "$BASE$r" -H "$H"); [ -n "$t" ] && a+=(-H "Authorization: Bearer $t"); [ -n "$b" ] && a+=(-d "$b"); curl "${a[@]}"; }
check() { if [ "$2" = "$3" ]; then PASS=$((PASS+1)); echo "  PASS  $1"; else FAIL=$((FAIL+1)); echo "  FAIL  $1  (obtenido: $2 | esperado: $3)"; fi; }
# err: el cuerpo es un error controlado (trae "error") y no un 500
err() { echo "$1" | jq -r 'if type=="object" and has("error") then "error" else "SIN-ERROR: "+(tostring|.[0:300]) end'; }

ADMIN=$(req POST /api/admin/auth/login '{"email":"admin@gestorpos.com","password":"Admin123!"}' | jq -r .token)
nuevo_negocio() { # nombre email features... -> "tenantId token"
  local n="$1" e="$2"; shift 2
  local t; t=$(req POST /api/admin/tenants/sin-cobro "{\"nombreNegocio\":\"$n\",\"nombreAdmin\":\"Dueno\",\"telefono\":\"1155550002\",\"email\":\"$e\",\"password\":\"Humo1234!\"}" "$ADMIN" | jq -r .id)
  local f; for f in "$@"; do req PUT "/api/admin/tenants/$t/features/$f" "" "$ADMIN" >/dev/null; done
  local l; l=$(req POST /api/auth/login "{\"email\":\"$e\",\"password\":\"Humo1234!\"}")
  local tok; tok=$(echo "$l" | jq -r .token)
  echo "$l" | jq -r '.terminosPendientes' | grep -q true && req POST /api/terminos/aceptar "" "$tok" >/dev/null
  echo "$t $tok"
}

echo "== Preparación: dos petshops (A y B) y un kiosco"
read -r TA _ < <(nuevo_negocio "pruebaFran-Pet A $SUF" "limA${SUF}@test.local" medidas compras cuenta-corriente)
TOKA=$(req POST /api/auth/login "{\"email\":\"limA${SUF}@test.local\",\"password\":\"Humo1234!\"}" | jq -r .token)
read -r TB TOKB < <(nuevo_negocio "pruebaFran-Pet B $SUF" "limB${SUF}@test.local" medidas)
TOKB=$(req POST /api/auth/login "{\"email\":\"limB${SUF}@test.local\",\"password\":\"Humo1234!\"}" | jq -r .token)
req POST /api/caja/abrir '{"montoApertura":1000}' "$TOKA" >/dev/null
req POST /api/caja/abrir '{"montoApertura":1000}' "$TOKB" >/dev/null

P=$(req POST /api/productos "{\"sku\":\"AL-$SUF\",\"nombre\":\"pruebaFran-Alimento\",\"categoriaId\":null,\"precio\":1999.99,\"costo\":1000,\"stockActual\":10,\"stockMinimo\":1,\"unidad\":\"Kilo\",\"presentaciones\":[{\"nombre\":\"Bolsa 5 kg\",\"cantidadBase\":5,\"precio\":9000},{\"nombre\":\"Bolsa 1 kg\",\"cantidadBase\":1,\"precio\":2100}]}" "$TOKA")
PID=$(echo "$P" | jq -r .id)
B5=$(echo "$P" | jq -r '.presentaciones[]|select(.nombre=="Bolsa 5 kg")|.id')
B1=$(echo "$P" | jq -r '.presentaciones[]|select(.nombre=="Bolsa 1 kg")|.id')
PB=$(req POST /api/productos "{\"sku\":\"BB-$SUF\",\"nombre\":\"pruebaFran-Otro negocio\",\"categoriaId\":null,\"precio\":100,\"costo\":50,\"stockActual\":10,\"stockMinimo\":1,\"unidad\":\"Kilo\",\"presentaciones\":[{\"nombre\":\"Bolsa B\",\"cantidadBase\":2,\"precio\":180}]}" "$TOKB")
PIDB=$(echo "$PB" | jq -r .id); PRB=$(echo "$PB" | jq -r '.presentaciones[0].id')
PU=$(req POST /api/productos "{\"sku\":\"CO-$SUF\",\"nombre\":\"pruebaFran-Collar\",\"categoriaId\":null,\"precio\":3800,\"costo\":2400,\"stockActual\":5,\"stockMinimo\":1}" "$TOKA")
PIDU=$(echo "$PU" | jq -r .id)

echo "== Cantidades inválidas en la venta"
v() { req POST /api/ventas "{\"items\":[{\"productoId\":\"$1\",\"cantidad\":$2${3:+,\"presentacionId\":\"$3\"}}],\"medioPago\":\"Efectivo\"}" "$TOKA"; }
check "cantidad 0 se rechaza" "$(err "$(v $PID 0)")" "error"
check "cantidad negativa se rechaza" "$(err "$(v $PID -1)")" "error"
check "cantidad enorme (9999999999) se rechaza sin romper" "$(err "$(v $PID 9999999999)")" "error"
check "cantidad con 4 decimales se rechaza" "$(err "$(v $PID 0.1234)")" "error"
check "producto por unidad no admite 0,5" "$(err "$(v $PIDU 0.5)")" "error"
check "producto inexistente" "$(err "$(v 00000000-0000-0000-0000-000000000001 1)")" "error"
check "presentación inexistente" "$(err "$(v $PID 1 00000000-0000-0000-0000-000000000002)")" "error"
check "presentación de otro negocio no se puede usar" "$(err "$(v $PID 1 $PRB)")" "error"
check "producto de otro negocio no se puede vender" "$(err "$(v $PIDB 1)")" "error"
BODY_VACIO='{"items":[],"medioPago":"Efectivo"}'
check "carrito vacío" "$(err "$(req POST /api/ventas "$BODY_VACIO" "$TOKA")")" "error"
check "no se vendió nada con tantos rechazos (stock intacto)" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')" "10"

echo "== Redondeo y ganancia"
R=$(v $PID 0.333)
check "0,333 kg x 1999,99 = 665,997 -> \$666,00" "$(echo "$R" | jq '.total*1')" "666"
R=$(req POST /api/ventas "{\"items\":[{\"productoId\":\"$PID\",\"cantidad\":3,\"presentacionId\":\"$B1\"}],\"medioPago\":\"Efectivo\"}" "$TOKA")
check "3 x Bolsa 1 kg a \$2100 = \$6300 (precio de la presentación, no el del kg)" "$(echo "$R" | jq '.total*1')" "6300"
check "stock: 10 - 0,333 - 3 = 6,667" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')" "6.667"

echo "== Presentación desactivada y producto desactivado"
req DELETE "/api/productos/$PID/presentaciones/$B1" "" "$TOKA" >/dev/null
check "presentación desactivada no se vende" "$(err "$(v $PID 1 $B1)")" "error"
req POST "/api/productos/$PID/presentaciones/$B1/activar" "" "$TOKA" >/dev/null
check "reactivada vuelve a venderse" "$(v $PID 1 $B1 | jq '.total*1')" "2100"
req DELETE "/api/productos/$PID" "" "$TOKA" >/dev/null
check "producto desactivado no se vende" "$(err "$(v $PID 1)")" "error"
req POST "/api/productos/$PID/activar" "" "$TOKA" >/dev/null
check "reactivado vuelve a venderse" "$(v $PID 0.5 | jq -r '.id|length>0')" "true"

echo "== Ajustes de stock"
S0=$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')
check "ajuste que deja stock negativo se rechaza" "$(err "$(req POST "/api/productos/$PID/ajustar-stock" '{"cantidad":-9999,"motivo":"merma"}' "$TOKA")")" "error"
check "ajuste con 4 decimales se rechaza" "$(err "$(req POST "/api/productos/$PID/ajustar-stock" '{"cantidad":0.0001,"motivo":"merma"}' "$TOKA")")" "error"
check "ajuste en 0 se rechaza" "$(err "$(req POST "/api/productos/$PID/ajustar-stock" '{"cantidad":0,"motivo":"merma"}' "$TOKA")")" "error"
check "ajuste fallido no cambia el stock" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')" "$S0"
check "ajuste decimal válido (+0,25)" "$(req POST "/api/productos/$PID/ajustar-stock" '{"cantidad":0.25,"motivo":"reposición"}' "$TOKA" | jq '.stockActual*1')" "$(jq -n "$S0+0.25")"
check "ajuste en producto por unidad con decimales se rechaza" "$(err "$(req POST "/api/productos/$PIDU/ajustar-stock" '{"cantidad":0.5,"motivo":"x"}' "$TOKA")")" "error"

echo "== Unidad inmutable y edición"
U=$(req PUT "/api/productos/$PID" "{\"nombre\":\"pruebaFran-Alimento\",\"categoriaId\":null,\"precio\":2100,\"costo\":1000,\"stockMinimo\":1,\"unidad\":\"Litro\"}" "$TOKA")
check "editar no cambia la unidad (sigue Kilo)" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq -r .unidad)" "Kilo"
check "editar el precio funciona" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.precio*1')" "2100"

echo "== Cuenta corriente (fiado) con cantidades medidas"
req POST "/api/productos/$PID/ajustar-stock" '{"cantidad":10,"motivo":"reposición"}' "$TOKA" >/dev/null
BODY_FIADO=$(jq -nc --arg p "$PID" --arg b "$B5" '{items:[{productoId:$p,cantidad:1.5},{productoId:$p,cantidad:1,presentacionId:$b}],medioPago:"A cuenta",telefonoCliente:"1144440000",nombreCliente:"pruebaFran-Cliente"}')
F=$(req POST /api/ventas "$BODY_FIADO" "$TOKA")
check "venta a cuenta: 1,5 x 2100 + 1 bolsa de 5 kg a 9000 = 12150" "$(echo "$F" | jq '.total*1')" "12150"
# (el id del cliente no hace falta: se lee el saldo de la lista)
check "el cliente queda debiendo 12150" "$(req GET "/api/clientes" "" "$TOKA" | jq '.items[]|select(.nombre=="pruebaFran-Cliente")|.saldoCuentaCorriente*1')" "12150"

echo "== Ventas simultáneas contra el mismo stock"
req POST "/api/productos/$PID/ajustar-stock" "{\"cantidad\":$(jq -n "(10-$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1'))*1000|round/1000"),\"motivo\":\"corrección\"}" "$TOKA" >/dev/null
check "stock puesto en 10 kg para la carrera" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')" "10"
for i in 1 2 3 4; do v $PID 6 > "/tmp/carrera-$SUF-$i.json" & done; wait
OK=0; RECH=0; for i in 1 2 3 4; do
  if jq -e '.total' "/tmp/carrera-$SUF-$i.json" >/dev/null 2>&1; then OK=$((OK+1));
  elif jq -r '.error' "/tmp/carrera-$SUF-$i.json" | grep -q "^Stock insuficiente de 'pruebaFran-Alimento' (disponible: 4 kg)"; then RECH=$((RECH+1)); fi
done
FINAL=$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')
rm -f /tmp/carrera-$SUF-*.json
check "4 ventas de 6 kg con 10 kg: entra una sola" "$OK" "1"
check "las otras 3 dicen «Stock insuficiente … (disponible: 4 kg)»" "$RECH" "3"
check "el stock queda exacto en 4 kg" "$FINAL" "4"
# lo mismo con un producto por unidad (la falla ya existía antes de esta función)
req POST "/api/productos/$PIDU/ajustar-stock" "{\"cantidad\":$(jq -n "10-$(req GET "/api/productos/$PIDU" "" "$TOKA" | jq '.stockActual*1')"),\"motivo\":\"corrección\"}" "$TOKA" >/dev/null
for i in 1 2 3 4 5 6; do v $PIDU 4 > "/tmp/carreraU-$SUF-$i.json" & done; wait
OKU=0; for i in 1 2 3 4 5 6; do jq -e '.total' "/tmp/carreraU-$SUF-$i.json" >/dev/null 2>&1 && OKU=$((OKU+1)); done; rm -f /tmp/carreraU-$SUF-*.json
check "6 ventas de 4 u. con 10 u.: entran 2 y el stock queda en 2" "$OKU $(req GET "/api/productos/$PIDU" "" "$TOKA" | jq '.stockActual*1')" "2 2"
# ventas y ajustes a la vez sobre el mismo producto
req POST "/api/productos/$PIDU/ajustar-stock" '{"cantidad":8,"motivo":"corrección"}' "$TOKA" >/dev/null
for i in 1 2 3; do v $PIDU 4 > "/tmp/mezcla-$SUF-$i.json" & done
for i in 1 2 3; do req POST "/api/productos/$PIDU/ajustar-stock" '{"cantidad":-4,"motivo":"merma"}' "$TOKA" > "/tmp/mezclaA-$SUF-$i.json" & done; wait
MV=0; for i in 1 2 3; do jq -e '.total' "/tmp/mezcla-$SUF-$i.json" >/dev/null 2>&1 && MV=$((MV+1)); jq -e '.stockActual' "/tmp/mezclaA-$SUF-$i.json" >/dev/null 2>&1 && MV=$((MV+1)); done
rm -f /tmp/mezcla-$SUF-* /tmp/mezclaA-$SUF-*
check "3 ventas y 3 ajustes de 4 u. con 10 u. a la vez: entran 2 en total y el stock queda en 2" "$MV $(req GET "/api/productos/$PIDU" "" "$TOKA" | jq '.stockActual*1')" "2 2"

echo "== Compras"
PROV=$(req POST /api/proveedores '{"nombre":"pruebaFran-Prov","cuit":null,"telefono":null,"email":null}' "$TOKA" | jq -r .id)
c() { req POST /api/compras "{\"proveedorId\":\"$PROV\",\"items\":[{\"productoId\":\"$1\",\"cantidad\":$2,\"costoUnitario\":$3${4:+,\"presentacionId\":\"$4\"}}]}" "$TOKA"; }
check "compra con cantidad 0 se rechaza" "$(err "$(c $PID 0 100)")" "error"
check "compra con costo negativo se rechaza" "$(err "$(c $PID 1 -5)")" "error"
check "compra con presentación de otro negocio se rechaza" "$(err "$(c $PID 1 100 $PRB)")" "error"
S1=$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')
c $PID 3 10000 $B5 >/dev/null
check "3 bolsas de 5 kg suman 15 kg" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')" "$(jq -n "$S1+15")"
check "costo por kg = 10000 / 5 = 2000" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.costo*1')" "2000"
SU=$(req GET "/api/productos/$PIDU" "" "$TOKA" | jq '.stockActual*1')
c $PIDU 2 24000 >/dev/null
check "compra de producto por unidad (2 a \$24000 c/u) suma 2" "$(req GET "/api/productos/$PIDU" "" "$TOKA" | jq '.stockActual*1')" "$(jq -n "$SU+2")"

echo "== Aislamiento entre negocios"
check "B no ve productos de A" "$(req GET "/api/productos" "" "$TOKB" | jq '[.[]|select(.nombre=="pruebaFran-Alimento")]|length')" "0"
check "B no puede leer el producto de A por id" "$(req GET "/api/productos/$PID" "" "$TOKB" | jq -r 'if type=="object" and (has("id") and .id!=null) then "VISIBLE" else "oculto" end')" "oculto"
check "B no puede ajustar stock de A" "$(err "$(req POST "/api/productos/$PID/ajustar-stock" '{"cantidad":1,"motivo":"x"}' "$TOKB")")" "error"
check "B no puede agregar presentaciones a un producto de A" "$(err "$(req POST "/api/productos/$PID/presentaciones" '{"nombre":"Hack","cantidadBase":1,"precio":1}' "$TOKB")")" "error"
check "el stock de A no cambió por los intentos de B" "$(req GET "/api/productos/$PID" "" "$TOKA" | jq '.stockActual*1')" "$(jq -n "$S1+15")"

echo "== Reportes y PDFs con cantidades decimales"
for r in reportes/stock/pdf reportes/ventas/pdf; do
  echo "  $r -> $(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$BASE/api/$r" -H "Authorization: Bearer $TOKA")"
done
check "PDF de stock responde 200 PDF" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$BASE/api/reportes/stock/pdf" -H "Authorization: Bearer $TOKA")" "200 application/pdf"
check "PDF de ventas responde 200 PDF" "$(curl -s -o /dev/null -w '%{http_code} %{content_type}' "$BASE/api/reportes/ventas/pdf" -H "Authorization: Bearer $TOKA")" "200 application/pdf"

echo "== Aumento masivo de precios"
CAT=$(req POST /api/categorias '{"nombre":"pruebaFran-Cat"}' "$TOKA" | jq -r .id)
MR1=$(req POST /api/marcas '{"nombre":"pruebaFran-M1"}' "$TOKA" | jq -r .id)
MR2=$(req POST /api/marcas '{"nombre":"pruebaFran-M2"}' "$TOKA" | jq -r .id)
pm() { req POST /api/productos "{\"sku\":\"$1-$SUF\",\"nombre\":\"pruebaFran-$1\",\"categoriaId\":${2:-null},\"marcaId\":${3:-null},\"precio\":1000,\"costo\":500,\"stockActual\":5,\"stockMinimo\":1,\"unidad\":\"Kilo\",\"presentaciones\":[{\"nombre\":\"Bolsa\",\"cantidadBase\":2,\"precio\":1800}]}" "$TOKA" | jq -r .id; }
PM1=$(pm MA "\"$CAT\"" "\"$MR1\""); PM2=$(pm MB "null" "\"$MR1\""); PM3=$(pm MC "\"$CAT\"" "\"$MR2\"")
precio() { req GET "/api/productos/$1" "" "$TOKA" | jq -r '"\(.precio*1) \([.presentaciones[]|.precio*1]|first)"'; }
check "el alcance cuenta productos y presentaciones de la marca M1 (2 y 2)" "$(req GET "/api/productos/actualizar-precios-masivo/alcance?marcaId=$MR1" "" "$TOKA" | jq -c '[.productos,.presentaciones]')" "[2,2]"
check "el alcance con categoría y marca a la vez cuenta solo 1 producto" "$(req GET "/api/productos/actualizar-precios-masivo/alcance?categoriaId=$CAT&marcaId=$MR1" "" "$TOKA" | jq -c '[.productos,.presentaciones]')" "[1,1]"
R=$(req POST /api/productos/actualizar-precios-masivo "{\"porcentaje\":10,\"categoriaId\":null,\"marcaId\":\"$MR1\"}" "$TOKA")
check "+10% a la marca M1 actualiza 2 productos y 2 presentaciones" "$(echo "$R" | jq -c '[.productosActualizados,.presentacionesActualizadas]')" "[2,2]"
check "la marca M1 sube el producto y su bolsa (1000→1100 y 1800→1980)" "$(precio $PM1) | $(precio $PM2)" "1100 1980 | 1100 1980"
check "la marca M2 no se tocó" "$(precio $PM3)" "1000 1800"
req POST /api/productos/actualizar-precios-masivo "{\"porcentaje\":-50,\"categoriaId\":\"$CAT\",\"marcaId\":null}" "$TOKA" >/dev/null
check "-50% a la categoría baja el producto y su bolsa (1100→550, 1000→500)" "$(precio $PM1) | $(precio $PM3)" "550 990 | 500 900"
check "el producto de otra categoría (M1 sin categoría) quedó igual" "$(precio $PM2)" "1100 1980"
check "un porcentaje de 0 se rechaza" "$(err "$(req POST /api/productos/actualizar-precios-masivo '{"porcentaje":0,"categoriaId":null}' "$TOKA")")" "error"
check "-100% se rechaza (dejaría todo gratis)" "$(err "$(req POST /api/productos/actualizar-precios-masivo '{"porcentaje":-100,"categoriaId":null}' "$TOKA")")" "error"
check "+5000% se rechaza" "$(err "$(req POST /api/productos/actualizar-precios-masivo '{"porcentaje":5000,"categoriaId":null}' "$TOKA")")" "error"
check "B no puede tocar los precios de A" "$(req POST /api/productos/actualizar-precios-masivo '{"porcentaje":10,"categoriaId":null}' "$TOKB" | jq -c '[.productosActualizados]')" "[1]"
check "…y los productos de A siguen igual" "$(precio $PM3)" "500 900"

echo "== Cantidades con coma y funciones sin volver a ingresar"
TK=$(req POST /api/auth/login "{\"email\":\"limB${SUF}@test.local\",\"password\":\"Humo1234!\"}" | jq -r .token)
req POST /api/productos/$PID/ajustar-stock '{"cantidad":10,"motivo":"reposición"}' "$TOKA" >/dev/null
check "el ticket de un kg suelto usa coma («0,333 kg»)" "$(v $PID 0.333 | jq -r .ticketTexto | grep -c '0,333 kg pruebaFran-Alimento')" "1"
check "el aviso de stock insuficiente usa coma" "$(v $PID 9999 | jq -r '.error|test("disponible: [0-9]+(,[0-9]+)? kg")')" "true"
FB=$(req POST /api/auth/login "{\"email\":\"limB${SUF}@test.local\",\"password\":\"Humo1234!\"}" | jq -r .token)
req DELETE "/api/admin/tenants/$TB/features/medidas" "" "$ADMIN" >/dev/null
check "con el MISMO token, apagar la función se nota al instante" "$(err "$(req POST /api/productos '{"sku":"NF-1","nombre":"Sin funcion","categoriaId":null,"precio":1,"costo":1,"stockActual":1,"stockMinimo":0,"unidad":"Kilo"}' "$FB")")" "error"
check "/api/auth/funciones ya no la trae" "$(req GET /api/auth/funciones "" "$FB" | jq -c '.features|index("medidas")')" "null"
req PUT "/api/admin/tenants/$TB/features/medidas" "" "$ADMIN" >/dev/null
check "volver a prenderla también se nota con el mismo token" "$(req POST /api/productos "{\"sku\":\"NF-$SUF\",\"nombre\":\"Con funcion\",\"categoriaId\":null,\"precio\":1,\"costo\":1,\"stockActual\":1,\"stockMinimo\":0,\"unidad\":\"Kilo\"}" "$FB" | jq -r .unidad)" "Kilo"
check "/api/auth/funciones la trae de nuevo" "$(req GET /api/auth/funciones "" "$FB" | jq -c '.features|index("medidas")!=null')" "true"
echo
echo "RESULTADO: $PASS pasaron, $FAIL fallaron"
[ "$FAIL" -eq 0 ]
