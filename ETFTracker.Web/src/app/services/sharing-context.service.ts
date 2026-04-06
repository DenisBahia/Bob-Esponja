import { Injectable, signal, computed } from '@angular/core';
import { SharedWithMeDto } from './api.service';

@Injectable({ providedIn: 'root' })
export class SharingContextService {
  private _viewingAs = signal<SharedWithMeDto | null>(null);

  /** The profile currently being viewed, or null if viewing own profile. */
  readonly viewingAs = this._viewingAs.asReadonly();

  /** True when viewing another user's portfolio. */
  readonly isViewingAsOther = computed(() => this._viewingAs() !== null);

  /** The owner user-id header value to attach to requests (or null). */
  readonly viewAsUserId = computed(() => this._viewingAs()?.ownerUserId ?? null);

  /** Whether the shared portfolio is read-only for the current viewer. */
  readonly isReadOnly = computed(() => this._viewingAs()?.isReadOnly ?? false);

  /** Switch to viewing another user's portfolio. */
  viewAs(share: SharedWithMeDto): void {
    this._viewingAs.set(share);
  }

  /** Return to viewing your own portfolio. */
  stopViewing(): void {
    this._viewingAs.set(null);
  }
}

