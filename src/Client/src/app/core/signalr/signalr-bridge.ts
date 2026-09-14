import { inject, OnDestroy, Service } from '@angular/core';
import { Router } from '@angular/router';
import { SIGNALR_CONFIG } from './signalr-config';
import { Dispatcher, Events } from '@ngrx/signals/events';
import { HttpError, HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { EMPTY, catchError, concatMap, from, merge, Subscription, tap } from 'rxjs';
import { signalrEvents } from './services/signalr.events';

@Service()
export class SignalRBridge implements OnDestroy {
  private readonly _config = inject(SIGNALR_CONFIG);
  private readonly _events = inject(Events);
  private readonly _dispatcher = inject(Dispatcher);
  private readonly _router = inject(Router);

  private _connection?: HubConnection;
  private readonly _subscriptions: Subscription;
  private _connecting?: Promise<void>;
  private _ready?: Promise<void>;
  private _resolveReady?: () => void;
  private _rejectReady?: (error: unknown) => void;

  constructor() {
    this._subscriptions = merge(
      this._events.on(signalrEvents.send).pipe(
        concatMap(({ payload }) =>
          from(this.sendWhenConnected(payload.methodName, payload.args)).pipe(
            catchError((error: unknown) => {
              this.reportCommandError(error);
              return EMPTY;
            })
          )
        )
      ),
      this._events.on(signalrEvents.invoke).pipe(
        concatMap(({ payload }) =>
          from(this.invokeWhenConnected(payload.methodName, payload.args)).pipe(
            catchError((error: unknown) => {
              this.reportCommandError(error);
              return EMPTY;
            })
          )
        )
      ),
      this._events.on(signalrEvents.disconnect).pipe(tap(() => this.disconnect()))
    ).subscribe();
  }

  /** Idempotent — safe to call more than once (e.g. re-called after a manual disconnect). */
  public connect(): Promise<void> {
    if (this.isConnected) return Promise.resolve();
    if (this._connecting) return this._connecting;
    if (this._connection?.state === HubConnectionState.Reconnecting && this._ready) {
      return this._ready;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(this._config.url) // , { accessTokenFactory: this._config.accessTokenFactory }
      .withAutomaticReconnect()
      .build();
    this._connection = connection;

    const seen = new Set<string>();
    // method name -> event, registered per entry
    for (const [methodName, creator] of Object.entries(this._config.eventMap)) {
      connection.on(methodName, (envelope: { messageId: string, data: unknown }) => {
        if (seen.has(envelope.messageId)) return;
        seen.add(envelope.messageId);

        if (seen.size > 500)
          seen.delete(seen.values().next().value!);

        this._dispatcher.dispatch(creator(envelope.data));
      });
    }

    connection.onreconnecting((err) => {
      this.createReadyGate();
      this._dispatcher.dispatch(signalrEvents.reconnecting({ error: err?.message }));
    });
    connection.onreconnected((id) => {
      this.resolveReady();
      this._dispatcher.dispatch(signalrEvents.reconnected({ connectionId: id }));
    });
    connection.onclose((err) => {
      this.rejectReady(err ?? new Error('SignalR connection closed.'));
      this._dispatcher.dispatch(signalrEvents.disconnected({ error: err?.message }));
      if (this.isUnauthorized(err)) this.redirectToLogin();
    });

    console.log('[SignalR] starting…', this._config.url);
    const connecting = connection.start()
      .then(() => {
        this._dispatcher.dispatch(signalrEvents.connected());
        console.log('[SignalR] connected');
      })
      .catch((err) => {
        console.error('[SignalR] start failed', err);
        if (this._connection === connection) {
          this._connection = undefined;
          this._connecting = undefined;
        }
        this.rejectReady(err);
        this._dispatcher.dispatch(signalrEvents.disconnected({ error: err?.message ?? 'connect failed' }));
        if (this.isUnauthorized(err)) this.redirectToLogin();
        throw err;
      })
      .finally(() => {
        if (this._connection === connection) this._connecting = undefined;
      });
    this._connecting = connecting;

    return connecting;
  }

  public disconnect(): void {
    const connection = this._connection;
    if (!connection) return;

    this._connection = undefined;
    this._connecting = undefined;
    this.rejectReady(new Error('SignalR connection disconnected.'));
    void connection.stop().catch((error: unknown) => this.reportCommandError(error));
  }

  private invokeWhenConnected(methodName: string, args: unknown[]): Promise<unknown> {
    return this.waitForConnection().then((connection) => connection.invoke(methodName, ...args));
  }

  private sendWhenConnected(methodName: string, args: unknown[]): Promise<void> {
    return this.waitForConnection().then((connection) => connection.send(methodName, ...args));
  }

  private waitForConnection(): Promise<HubConnection> {
    const connection = this._connection;
    if (!connection) return Promise.reject(new Error('SignalR is not connected.'));
    if (connection.state === HubConnectionState.Connected) return Promise.resolve(connection);

    const ready = connection.state === HubConnectionState.Reconnecting
      ? this._ready
      : this._connecting;
    if (!ready) return Promise.reject(new Error('SignalR is not connected.'));

    return ready.then(() => {
      if (this._connection !== connection || connection.state !== HubConnectionState.Connected) {
        throw new Error('SignalR is not connected.');
      }
      return connection;
    });
  }

  private createReadyGate(): void {
    this._ready = new Promise<void>((resolve, reject) => {
      this._resolveReady = resolve;
      this._rejectReady = reject;
    });
  }

  private resolveReady(): void {
    this._resolveReady?.();
    this._resolveReady = undefined;
    this._rejectReady = undefined;
  }

  private rejectReady(error: unknown): void {
    this._rejectReady?.(error);
    this._resolveReady = undefined;
    this._rejectReady = undefined;
  }

  private reportCommandError(error: unknown): void {
    const message = error instanceof Error ? error.message : String(error);
    this._dispatcher.dispatch(signalrEvents.messageError({ error: message }));
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
    this.disconnect();
  }
}
