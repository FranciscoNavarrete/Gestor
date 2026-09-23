export interface CrearTareaRequest {
  titulo: string;
  notas: string | null;
  fecha: string;
  hora: string;
  minutosAntesAviso: number | null;
}

export interface EditarTareaRequest {
  titulo: string;
  notas: string | null;
  fecha: string;
  hora: string;
  minutosAntesAviso: number | null;
}

export interface TareaDto {
  id: string;
  titulo: string;
  notas: string | null;
  fechaHora: string;
  minutosAntesAviso: number | null;
  completada: boolean;
}
