export interface ClaveVapida {
  clavePublica: string;
}

export interface SuscribirsePushRequest {
  endpoint: string;
  p256dh: string;
  auth: string;
}

export interface DesuscribirsePushRequest {
  endpoint: string;
}
