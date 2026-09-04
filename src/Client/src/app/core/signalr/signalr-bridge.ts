import { inject, OnDestroy, Service } from '@angular/core';
import { Router } from '@angular/router';
import { SIGNALR_CONFIG } from './provide-signalr';
import { Dispatcher, Events } from '@ngrx/signals/events';
import { HttpError, HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { merge, Subscription, switchMap, tap } from 'rxjs';
import { signalrEvents } from './store/signalr.events';

@Service()
export class SignalRBridge implements OnDestroy {
  private readonly _config = inject(SIGNALR_CONFIG);
  private readonly _events = inject(Events);
  private readonly _dispatcher = inject(Dispatcher);
  private readonly _router = inject(Router);

  private _connection?: HubConnection;
  private readonly _subscriptions: Subscription;
  private _connecting?: Promise<void>;

  constructor() {
    // command side — mirrors ofActionDispatched(SendSignalRMessage / InvokeSignalRMessage)
    this._subscriptions = merge(
      this._events.on(signalrEvents.send).pipe(
        tap(({ payload }) => this._connection?.send(payload.methodName, ...payload.args))
      ),
      this._events.on(signalrEvents.invoke).pipe(
        switchMap(({ payload }) => this._connection!.invoke(payload.methodName, ...payload.args))
      ),
      this._events.on(signalrEvents.disconnect).pipe(tap(() => this._connection?.stop()))
    ).subscribe();
  }

  /** Idempotent — safe to call more than once (e.g. re-called after a manual disconnect). */
  public connect(): Promise<void> {
    if (this._connecting) return this._connecting;

    this._connection = new HubConnectionBuilder()
      .withUrl(this._config.url) // , { accessTokenFactory: this._config.accessTokenFactory }
      .withAutomaticReconnect()
      .build();

    const seen = new Set<string>();
    // method name -> event, registered per entry
    for (const [methodName, creator] of Object.entries(this._config.eventMap)) {
      this._connection.on(methodName, (envelope: { messageId: string, data: unknown }) => {
        if (seen.has(envelope.messageId)) return;
        seen.add(envelope.messageId);

        if (seen.size > 500)
          seen.delete(seen.values().next().value!);

        this._dispatcher.dispatch(creator(envelope.data));
      });
    }

    this._connection.onreconnecting((err) =>
      this._dispatcher.dispatch(signalrEvents.reconnecting({ error: err?.message })));
    this._connection.onreconnected((id) =>
      this._dispatcher.dispatch(signalrEvents.reconnected({ connectionId: id })));
    this._connection.onclose((err) => {
      this._dispatcher.dispatch(signalrEvents.disconnected({ error: err?.message }));
      if (this.isUnauthorized(err)) this.redirectToLogin();
    });

    console.log('[SignalR] starting…', this._config.url);
    this._connecting = this._connection.start()
      .then(() => {
        this._dispatcher.dispatch(signalrEvents.connected());
        console.log('[SignalR] connected')
      })
      .catch((err) => {
        console.error('[SignalR] start failed', err)
        this._connection = undefined;
        this._dispatcher.dispatch(signalrEvents.disconnected({ error: err?.message ?? 'connect failed' }));
        if (this.isUnauthorized(err)) this.redirectToLogin();
        throw err;
      });

    return this._connecting;
  }

  /** True when the hub rejected the handshake because the user isn't authenticated (401). */
  private isUnauthorized(err: unknown): boolean {
    return err instanceof HttpError && err.statusCode === 401;
  }

  private redirectToLogin(): void {
    if (this._router.url.startsWith('/login')) return;
    void this._router.navigate(['/login']);
  }

  public get isConnected(): boolean {
    return this._connection?.state === HubConnectionState.Connected;
  }

  public ngOnDestroy(): void {
    this._subscriptions.unsubscribe();
    this._connection?.stop();
    this._connection = undefined;
  }
}
