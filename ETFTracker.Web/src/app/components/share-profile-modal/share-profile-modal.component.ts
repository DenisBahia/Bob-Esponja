import {
  Component, EventEmitter, OnInit, Output,
  ChangeDetectionStrategy, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, CreateShareDto, ShareSummaryDto, SharedWithMeDto } from '../../services/api.service';
import { SharingContextService } from '../../services/sharing-context.service';

@Component({
  selector: 'app-share-profile-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './share-profile-modal.component.html',
  styleUrls: ['./share-profile-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShareProfileModalComponent implements OnInit {
  @Output() closed = new EventEmitter<void>();
  @Output() startedViewingAs = new EventEmitter<SharedWithMeDto>();
  @Output() stoppedViewing = new EventEmitter<void>();

  // My shares tab
  myShares: ShareSummaryDto[] = [];
  mySharesLoading = false;

  // Shared with me tab
  sharedWithMe: SharedWithMeDto[] = [];
  sharedWithMeLoading = false;

  // New share form
  newShareEmail = '';
  newShareReadOnly = true;
  sharing = false;
  shareError: string | null = null;

  // In-row state
  updatingId: number | null = null;
  deletingId: number | null = null;
  confirmDeleteId: number | null = null;

  activeTab: 'my-shares' | 'shared-with-me' = 'my-shares';

  constructor(
    private api: ApiService,
    public sharingCtx: SharingContextService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadMyShares();
    this.loadSharedWithMe();
  }

  loadMyShares(): void {
    this.mySharesLoading = true;
    this.api.getMyShares().subscribe({
      next: (data) => { this.myShares = data; this.mySharesLoading = false; this.cdr.markForCheck(); },
      error: () => { this.mySharesLoading = false; this.cdr.markForCheck(); }
    });
  }

  loadSharedWithMe(): void {
    this.sharedWithMeLoading = true;
    this.api.getSharedWithMe().subscribe({
      next: (data) => { this.sharedWithMe = data; this.sharedWithMeLoading = false; this.cdr.markForCheck(); },
      error: () => { this.sharedWithMeLoading = false; this.cdr.markForCheck(); }
    });
  }

  createShare(): void {
    if (!this.newShareEmail.trim()) return;
    this.sharing = true;
    this.shareError = null;
    const dto: CreateShareDto = {
      guestEmail: this.newShareEmail.trim().toLowerCase(),
      isReadOnly: this.newShareReadOnly
    };
    this.api.createShare(dto).subscribe({
      next: (share) => {
        this.myShares = [...this.myShares, share];
        this.newShareEmail = '';
        this.sharing = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.shareError = err?.error?.message || 'Failed to create share. Check the email and try again.';
        this.sharing = false;
        this.cdr.markForCheck();
      }
    });
  }

  toggleReadOnly(share: ShareSummaryDto): void {
    this.updatingId = share.id;
    this.api.updateShare(share.id, { isReadOnly: !share.isReadOnly }).subscribe({
      next: (updated) => {
        this.myShares = this.myShares.map(s => s.id === updated.id ? updated : s);
        this.updatingId = null;
        this.cdr.markForCheck();
      },
      error: () => { this.updatingId = null; this.cdr.markForCheck(); }
    });
  }

  confirmDelete(id: number): void {
    this.confirmDeleteId = id;
    this.cdr.markForCheck();
  }

  cancelDelete(): void {
    this.confirmDeleteId = null;
    this.cdr.markForCheck();
  }

  removeShare(id: number): void {
    this.deletingId = id;
    this.confirmDeleteId = null;
    this.api.deleteShare(id).subscribe({
      next: () => {
        this.myShares = this.myShares.filter(s => s.id !== id);
        this.deletingId = null;
        this.cdr.markForCheck();
      },
      error: () => { this.deletingId = null; this.cdr.markForCheck(); }
    });
  }

  viewAs(share: SharedWithMeDto): void {
    this.sharingCtx.viewAs(share);
    this.startedViewingAs.emit(share);
    this.closed.emit();
  }

  stopViewing(): void {
    this.sharingCtx.stopViewing();
    this.stoppedViewing.emit();
    this.closed.emit();
  }

  close(): void {
    this.closed.emit();
  }
}

