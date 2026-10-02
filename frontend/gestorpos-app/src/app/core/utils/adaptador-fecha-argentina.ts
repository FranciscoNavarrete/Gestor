import { Injectable } from '@angular/core';
import { NativeDateAdapter } from '@angular/material/core';

/**
 * El adaptador nativo de Angular Material interpreta lo que se escribe a mano como mes/día (formato
 * de EE.UU.): "3/10/2026" se leería como el 10 de marzo. Acá se entiende día/mes/año, que es como
 * escribe fechas quien usa la app.
 */
@Injectable()
export class AdaptadorFechaArgentina extends NativeDateAdapter {
  override parse(valor: unknown): Date | null {
    if (typeof valor === 'string') {
      const coincidencia = valor.trim().match(/^(\d{1,2})[/.-](\d{1,2})[/.-](\d{2}|\d{4})$/);
      if (coincidencia) {
        const dia = Number(coincidencia[1]);
        const mes = Number(coincidencia[2]);
        let anio = Number(coincidencia[3]);
        if (anio < 100) anio += 2000;

        const fecha = new Date(anio, mes - 1, dia);
        const existe = fecha.getFullYear() === anio && fecha.getMonth() === mes - 1 && fecha.getDate() === dia;
        return existe ? fecha : this.invalid();
      }
    }
    return super.parse(valor);
  }
}
