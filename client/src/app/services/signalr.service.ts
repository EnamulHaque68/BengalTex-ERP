import { Injectable, OnDestroy } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { TokenStorageService } from './token-storage.service';

export interface SessionSupersededPayload {
  reason: string;
  newSessionId: string;
  occurredAt: string;
}

/**
 * Manages a single SignalR connection to /hubs/session. Starts on login, stops on
 * logout. Exposes a stream of session-lifecycle events that AuthService consumes
 * to enforce single-session-per-user from the client side.
 */
@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private hubConnection?: HubConnection;
  private readonly sessionSupersededSubject = new Subject<SessionSupersededPayload>();

  /** Fires when this client's session has been replaced by a newer login from another device. */
  readonly sessionSuperseded$ = this.sessionSupersededSubject.asObservable();

  constructor(private tokenStorage: TokenStorageService) {}

  async start(): Promise<void> {
    if (
      this.hubConnection?.state === HubConnectionState.Connected ||
      this.hubConnection?.state === HubConnectionState.Connecting ||
      this.hubConnection?.state === HubConnectionState.Reconnecting
    ) {
      return;
    }

    const hubUrl = `${environment.signalRHubUrl}/session`;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        // WebSocket can't carry an Authorization header — token goes on the query
        // string. Backend JwtBearer.OnMessageReceived pulls it out for /hubs/* paths.
        accessTokenFactory: () => this.tokenStorage.getAccessToken() ?? ''
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.hubConnection.on('SessionSuperseded', (payload: SessionSupersededPayload) => {
      this.sessionSupersededSubject.next(payload);
    });

    try {
      await this.hubConnection.start();
      console.log('[SignalR] Connected to', hubUrl);
    } catch (err) {
      console.error('[SignalR] Connection failed:', err);
    }
  }

  async stop(): Promise<void> {
    if (!this.hubConnection) return;
    if (this.hubConnection.state === HubConnectionState.Disconnected) return;

    try {
      await this.hubConnection.stop();
    } catch (err) {
      console.error('[SignalR] Stop failed:', err);
    }
  }

  ngOnDestroy(): void {
    this.stop();
  }
}
