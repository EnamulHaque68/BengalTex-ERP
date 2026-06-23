import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

/**
 * Lightweight signal bus so that changing the current user's photo (e.g. on My Profile)
 * immediately refreshes the topbar avatar — instead of waiting for a full page reload.
 */
@Injectable({ providedIn: 'root' })
export class AvatarRefreshService {
  private readonly _changes = new Subject<number>();
  /** Emits a fresh timestamp whenever the current user's avatar may have changed. */
  readonly changes$ = this._changes.asObservable();

  notify(): void { this._changes.next(Date.now()); }
}
