import {
  EnvironmentProviders,
  inject,
  InjectionToken,
  makeEnvironmentProviders,
  provideAppInitializer
} from '@angular/core';
import { SignalRBridge } from './signalr-bridge';
import { EventInstance } from '@ngrx/signals/events';

export interface SignalRBridgeConfig {
  url: string;
  accessTokenFactory?: () => string | Promise<string>;
  // hub method name -> event creator
  eventMap: Record<string, (payload: any) => EventInstance<string, unknown>>;
}

export const SIGNALR_CONFIG = new InjectionToken<SignalRBridgeConfig>('SIGNALR_CONFIG');

export function provideSignalR(config: SignalRBridgeConfig): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: SIGNALR_CONFIG, useValue: config },
    // Fire-and-forget: the bridge itself surfaces connect failures (see handleStartFailure),
    // so the app initializer must not await/reject here — otherwise an anonymous user on a
    // public route (e.g. /login) would never get past bootstrap when the hub replies 401.
    provideAppInitializer(() => {
      void inject(SignalRBridge).connect().catch(() => {});
    }),
  ]);
}
