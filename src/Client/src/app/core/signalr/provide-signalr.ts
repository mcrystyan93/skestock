import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer
} from '@angular/core';
import { SignalRBridge } from './signalr-bridge';
import { SIGNALR_CONFIG, type SignalRBridgeConfig } from './signalr-config';

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
