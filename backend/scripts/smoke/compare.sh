#!/usr/bin/env bash
# Compara dos corridas del guion de humo ignorando fecha, sufijo del negocio de prueba y los campos que la
# versión nueva SUMA (unidad, presentaciones, presentacionNombre, marcaId, marcaNombre). Cualquier otra diferencia se informa.
A="$1"; B="$2"; rc=0
limpiar() {
  case "$1" in
    *.json) jq -S 'walk(if type=="object" then del(.unidad, .presentaciones, .presentacionNombre, .marcaId, .marcaNombre) else . end)' "$1" 2>/dev/null | sed -E 's/Fecha: [0-9\/ :]+/Fecha: X/; s/(base|run)[0-9]+/SUF/g' ;;
    *) sed -E 's/Fecha: [0-9\/ :]+/Fecha: X/; s/(base|run)[0-9]+/SUF/g' "$1" ;;
  esac
}
for f in "$A"/*; do
  n=$(basename "$f")
  if ! diff <(limpiar "$f") <(limpiar "$B/$n") >/dev/null; then
    echo "DIFERENTE: $n"; diff <(limpiar "$f") <(limpiar "$B/$n") | head -10; rc=1
  fi
done
[ $rc -eq 0 ] && echo "IGUAL: las dos corridas dan el mismo resultado"
exit $rc
