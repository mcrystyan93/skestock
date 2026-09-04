import { type } from '@ngrx/signals';
import { eventGroup } from '@ngrx/signals/events';

export const signalrEvents = eventGroup({
  source: 'SignalR',
  events: {
    connect: type<void>(),
    disconnect: type<void>(),
    send: type<{ methodName: string; args: unknown[] }>(),
    invoke: type<{ methodName: string; args: unknown[] }>(),

    connected: type<void>(),
    disconnected: type<{ error?: string }>(),
    reconnecting: type<{ error?: string }>(),
    reconnected: type<{ connectionId?: string }>(),
    messageError: type<{ error: string }>()
  }
});
