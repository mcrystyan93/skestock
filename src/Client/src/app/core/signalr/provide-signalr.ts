import {
  EnvironmentProviders,
  makeEnvironmentProviders
} from '@angular/core';
import { SIGNALR_CONFIG, type SignalRBridgeConfig } from './signalr-config';

export function provideSignalR(config: SignalRBridgeConfig): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: SIGNALR_CONFIG, useValue: config }
  ]);
}
