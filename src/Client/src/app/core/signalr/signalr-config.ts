import { InjectionToken } from '@angular/core';
import { EventInstance } from '@ngrx/signals/events';

export interface SignalRBridgeConfig {
  url: string;
  accessTokenFactory?: () => string | Promise<string>;
  // hub method name -> event creator
  eventMap: Record<string, (payload: any) => EventInstance<string, unknown>>;
}

export const SIGNALR_CONFIG = new InjectionToken<SignalRBridgeConfig>('SIGNALR_CONFIG');
