#!/usr/bin/env bash
# Compara dos corridas del guion de humo ignorando fecha y sufijo del negocio de prueba.
A="$1"; B="$2"; rc=0
for f in "$A"/*; do
  n=$(basename "$f")
  if ! diff <(sed -E 's/Fecha: [0-9\/ :]+/Fecha: X/; s/(base|run)[0-9]+/SUF/g' "$f") <(sed -E 's/Fecha: [0-9\/ :]+/Fecha: X/; s/(base|run)[0-9]+/SUF/g' "$B/$n") >/dev/null; then
    echo "DIFERENTE: $n"; rc=1
  fi
done
[ $rc -eq 0 ] && echo "IGUAL: las dos corridas dan el mismo resultado"
exit $rc
