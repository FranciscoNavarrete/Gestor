export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiraUtc: string;
  tenantId: string;
  nombreNegocio: string;
  nombreUsuario: string;
  rol: string;
  features: string[];
  /** El dueño todavía no aceptó los términos vigentes: se le pide antes de dejarlo usar el sistema. */
  terminosPendientes?: boolean;
  /** El operador le restableció la contraseña con una temporal: tiene que elegir una nueva antes de seguir. */
  debeCambiarPassword?: boolean;
}
