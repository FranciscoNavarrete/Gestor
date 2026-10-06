export interface TerminosSeccion {
  titulo: string;
  texto: string;
}

export interface Terminos {
  version: string;
  resumen: string[];
  secciones: TerminosSeccion[];
}

/** Constancia de aceptación; origen 'Cliente' = el dueño los aceptó al ingresar, 'Vendedor' = quien dio el alta confirmó que se los explicó. */
export interface AceptacionTerminos {
  version: string;
  origen: 'Cliente' | 'Vendedor';
  nombre: string;
  fechaUtc: string;
  ip: string | null;
}
