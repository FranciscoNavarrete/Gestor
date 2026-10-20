#!/usr/bin/env bash
# Prueba de la importación de Excel (columna Unidad). Requiere python3 + openpyxl.
set -u
BASE="${1:-http://localhost:5090}"; SUF="${SMOKE_SUFIJO:-imp$(date +%s)}"; H='Content-Type: application/json'
req() { local m="$1" r="$2" b="${3:-}" t="${4:-}"; local a=(-s -X "$m" "$BASE$r" -H "$H"); [ -n "$t" ] && a+=(-H "Authorization: Bearer $t"); [ -n "$b" ] && a+=(-d "$b"); curl "${a[@]}"; }
PASS=0; FAIL=0
check() { if [ "$2" = "$3" ]; then PASS=$((PASS+1)); echo "  PASS  $1"; else FAIL=$((FAIL+1)); echo "  FAIL  $1  (obtenido: $2 | esperado: $3)"; fi; }
ADMIN=$(req POST /api/admin/auth/login '{"email":"admin@gestorpos.com","password":"Admin123!"}' | jq -r .token)
mk() { local t; t=$(req POST /api/admin/tenants/sin-cobro "{\"nombreNegocio\":\"$1\",\"nombreAdmin\":\"D\",\"telefono\":\"1155550003\",\"email\":\"$2\",\"password\":\"Humo1234!\"}" "$ADMIN" | jq -r .id); echo "$t"; }
login() { local l; l=$(req POST /api/auth/login "{\"email\":\"$1\",\"password\":\"Humo1234!\"}"); local k; k=$(echo "$l" | jq -r .token); echo "$l" | jq -r .terminosPendientes | grep -q true && req POST /api/terminos/aceptar "" "$k" >/dev/null; echo "$k"; }
xlsx() { python3 - "$@" <<'PY'
import sys, openpyxl
out=sys.argv[1]; wb=openpyxl.Workbook(); ws=wb.active
ws.append(["SKU","Nombre","Categoría","Precio","Costo","Stock inicial","Stock mínimo","Marca","Unidad"])
ws.append([f"IMP1-{sys.argv[2]}","Alimento suelto","Alimentos",4200,2900,"12,5","2","Royal Canin","kg"])
ws.append([f"IMP2-{sys.argv[2]}","Gaseosa","Bebidas",1200,800,24,6,"Coca-Cola",""])
ws.append([f"IMP3-{sys.argv[2]}","Aceite","Almacen",1500,1000,"3.25",1,"",   "L"])
ws.append([f"IMP4-{sys.argv[2]}","Raro","Otros",1,1,1,0,"Genérica","galon"])
wb.save(out)
PY
}
xlsx /tmp/imp.xlsx "$SUF"
subir() { curl -s -X POST "$BASE/api/productos/importar-excel" -H "Authorization: Bearer $1" -F "archivo=@/tmp/imp.xlsx"; }

echo "== Negocio con la función"
T=$(mk "Import Pet $SUF" "imp${SUF}@test.local"); req PUT "/api/admin/tenants/$T/features/medidas" "" "$ADMIN" >/dev/null
K=$(login "imp${SUF}@test.local")
H8=$(curl -s "$BASE/api/productos/plantilla-excel" -H "Authorization: Bearer $K" -o /tmp/plantilla8.xlsx; python3 -c "import openpyxl;print(len([c for c in openpyxl.load_workbook('/tmp/plantilla8.xlsx').active[1] if c.value]))")
check "la plantilla trae 9 columnas con la función (Marca y Unidad)" "$H8" "9"
R=$(subir "$K")
check "importa 3 filas buenas" "$(echo "$R" | jq '.creados')" "3"
check "1 fila con error (unidad inventada)" "$(echo "$R" | jq '.errores|length')" "1"
LST=$(req GET "/api/productos" "" "$K")
check "alimento suelto quedó por Kilo con 12,5 kg" "$(echo "$LST" | jq -r '[.[]|select(.sku|startswith("IMP1"))|"\(.unidad) \(.stockActual*1)"]|first')" "Kilo 12.5"
check "gaseosa sin unidad quedó por Unidad" "$(echo "$LST" | jq -r '[.[]|select(.sku|startswith("IMP2"))|.unidad]|first')" "Unidad"
check "aceite quedó por Litro con 3,25 L" "$(echo "$LST" | jq -r '[.[]|select(.sku|startswith("IMP3"))|"\(.unidad) \(.stockActual*1)"]|first')" "Litro 3.25"
R2=$(subir "$K")
check "reimportar actualiza (no cambia la unidad)" "$(echo "$R2" | jq '.actualizados')" "3"
MK=$(req GET /api/marcas "" "$K" | jq -r '[.[].nombre]|sort|join(",")')
check "las marcas del Excel se crearon solas" "$MK" "Coca-Cola,Royal Canin"
LST2=$(req GET "/api/productos" "" "$K")
check "el producto quedó con su marca" "$(echo "$LST2" | jq -r '[.[]|select(.sku|startswith("IMP1"))|.marcaNombre]|first')" "Royal Canin"
check "la celda de marca vacía deja el producto sin marca" "$(echo "$LST2" | jq -r '[.[]|select(.sku|startswith("IMP3"))|.marcaId]|first')" "null"

echo "== Filas que no se pueden redondear en silencio"
python3 - "$SUF" <<'PY'
import sys, openpyxl
wb=openpyxl.Workbook(); ws=wb.active
ws.append(["SKU","Nombre","Categoría","Precio","Costo","Stock inicial","Stock mínimo","Marca","Unidad"])
s=sys.argv[1]
ws.append([f"DEC1-{s}","Kilo con 5 decimales","Otros",100,50,"1,23456",1,"","kg"])
ws.append([f"DEC2-{s}","Unidad con medio","Otros",100,50,"10,5",1,"","u"])
ws.append([f"DEC3-{s}","Precio negativo","Otros",-5,50,3,1,"","u"])
ws.append([f"DEC4-{s}","Kilo bien cargado","Otros",100,50,"2,125",1,"","kg"])
ws.append([f"DEC5-{s}","Precio con coma","Otros","1.234,50","0,75",4,1,"","u"])
wb.save("/tmp/imp-dec.xlsx")
PY
R4=$(curl -s -X POST "$BASE/api/productos/importar-excel" -H "Authorization: Bearer $K" -F "archivo=@/tmp/imp-dec.xlsx")
check "entran solo las 2 filas bien cargadas" "$(echo "$R4" | jq '.creados')" "2"
check "más de 3 decimales en kg se rechaza" "$(echo "$R4" | jq -r '[.errores[]|select(.fila==2)|.mensaje]|first|contains("hasta 3 decimales")')" "true"
check "stock con decimales en un producto por unidad se rechaza" "$(echo "$R4" | jq -r '[.errores[]|select(.fila==3)|.mensaje]|first|contains("número entero")')" "true"
check "el error de precio no trae texto técnico" "$(echo "$R4" | jq -r '[.errores[]|select(.fila==4)|.mensaje]|first|contains("Parameter")')" "false"
check "el precio «1.234,50» se lee como 1234,50 y el costo «0,75» como 0,75" "$(req GET /api/productos "" "$K" | jq -r '[.[]|select(.sku|startswith("DEC5"))|"\(.precio*1) \(.costo*1)"]|first')" "1234.5 0.75"
echo "esto no es un excel" > /tmp/roto.xlsx
CODE=$(curl -s -o /tmp/roto.json -w "%{http_code}" -X POST "$BASE/api/productos/importar-excel" -H "Authorization: Bearer $K" -F "archivo=@/tmp/roto.xlsx")
check "un archivo dañado responde 400 con un mensaje claro" "$CODE $(jq -r .error /tmp/roto.json | cut -c1-27)" "400 No se pudo leer el archivo."

echo "== Negocio sin la función"
T2=$(mk "Import Kio $SUF" "impk${SUF}@test.local"); K2=$(login "impk${SUF}@test.local")
curl -s "$BASE/api/productos/plantilla-excel" -H "Authorization: Bearer $K2" -o /tmp/plantilla7.xlsx
H7=$(python3 -c "import openpyxl;print(len([c for c in openpyxl.load_workbook('/tmp/plantilla7.xlsx').active[1] if c.value]))")
check "la plantilla trae 8 columnas sin la función (suma solo Marca)" "$H7" "8"
R3=$(subir "$K2")
check "sin la función solo entran las filas por unidad" "$(echo "$R3" | jq '.creados')" "1"
check "las filas por kg o L se rechazan con el aviso" "$(echo "$R3" | jq '[.errores[]|select(.mensaje|contains("no está activada"))]|length')" "2"
echo; echo "RESULTADO: $PASS pasaron, $FAIL fallaron"; [ "$FAIL" -eq 0 ]
