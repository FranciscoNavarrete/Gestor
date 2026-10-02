import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    MercadoPago: new (publicKey: string, options?: { locale: string }) => MercadoPagoInstance;
  }
}

interface MercadoPagoInstance {
  bricks(): { create(type: 'cardPayment', containerId: string, config: CardPaymentConfig): Promise<BrickController> };
}

interface CardPaymentConfig {
  initialization: { amount: number; payer?: { email?: string } };
  customization?: {
    paymentMethods?: { maxInstallments?: number };
    visual?: { texts?: { formSubmit?: string } };
  };
  callbacks: {
    onReady: () => void;
    onSubmit: (data: CardPaymentData) => Promise<void>;
    onError: (error: BrickError) => void;
  };
}

export interface CardPaymentData {
  token: string;
}

export interface BrickError {
  type: string;
  cause: string;
}

interface BrickController {
  unmount(): void;
}

const SDK_URL = 'https://sdk.mercadopago.com/js/v2';

@Injectable({ providedIn: 'root' })
export class MpService {
  private mp: MercadoPagoInstance | null = null;
  private activeBrick: BrickController | null = null;

  async mountCardPaymentBrick(options: {
    containerId: string;
    amount: number;
    emailPagador: string;
    submitLabel: string;
    onSubmit: (data: CardPaymentData) => Promise<void>;
    onError: (error: BrickError) => void;
  }): Promise<void> {
    await this.loadSdk();
    this.unmountBrick();

    this.activeBrick = await this.mp!.bricks().create('cardPayment', options.containerId, {
      initialization: { amount: options.amount, payer: { email: options.emailPagador } },
      customization: {
        paymentMethods: { maxInstallments: 1 },
        visual: { texts: { formSubmit: options.submitLabel } },
      },
      callbacks: {
        onReady: () => {},
        onSubmit: options.onSubmit,
        onError: options.onError,
      },
    });
  }

  unmountBrick(): void {
    this.activeBrick?.unmount();
    this.activeBrick = null;
  }

  private async loadSdk(): Promise<void> {
    if (this.mp) return;
    await this.injectScript(SDK_URL);
    this.mp = new window.MercadoPago(environment.mpPublicKey, { locale: 'es-AR' });
  }

  private injectScript(src: string): Promise<void> {
    return new Promise((resolve, reject) => {
      if (document.querySelector(`script[src="${src}"]`) && window.MercadoPago) {
        resolve();
        return;
      }
      const script = document.createElement('script');
      script.src = src;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('No se pudo cargar el SDK de Mercado Pago'));
      document.head.appendChild(script);
    });
  }
}
