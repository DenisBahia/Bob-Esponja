import { Component, EventEmitter, Input, OnInit, Output, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, TaxEventDto, TaxSummaryDto } from '../../services/api.service';
import { SharingContextService } from '../../services/sharing-context.service';

@Component({
  selector: 'app-tax-history-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tax-history-modal.component.html',
  styleUrls: ['./tax-history-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaxHistoryModalComponent implements OnInit {
  @Input() holdingId!: number;
  @Input() ticker = '';
  @Output() closed = new EventEmitter<void>();
  @Output() taxPaid = new EventEmitter<void>();

  summary: TaxSummaryDto | null = null;
  loading = true;
  error: string | null = null;
  markingId: number | null = null;

  constructor(
    private api: ApiService,
    public sharingCtx: SharingContextService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadTaxEvents();
  }

  loadTaxEvents(): void {
    this.loading = true;
    this.error = null;
    this.api.getTaxEvents(this.holdingId).subscribe({
      next: (s) => {
        this.summary = s;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'Failed to load tax history.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onMarkPaid(event: TaxEventDto): void {
    if (this.sharingCtx.isReadOnly()) return;
    this.markingId = event.id;
    this.cdr.markForCheck();
    this.api.markTaxEventPaid(event.id).subscribe({
      next: (updated) => {
        if (this.summary) {
          const idx = this.summary.events.findIndex(e => e.id === updated.id);
          if (idx >= 0) this.summary.events[idx] = updated;
          this.summary.totalPending = this.summary.events
            .filter(e => e.status === 'Pending').reduce((s, e) => s + e.taxAmount, 0);
          this.summary.totalPaid = this.summary.events
            .filter(e => e.status === 'Paid').reduce((s, e) => s + e.taxAmount, 0);
        }
        this.markingId = null;
        this.taxPaid.emit();
        this.cdr.markForCheck();
      },
      error: () => {
        this.markingId = null;
        this.cdr.markForCheck();
      }
    });
  }

  close(): void {
    this.closed.emit();
  }

  formatCurrency(v: number): string {
    const sign = v < 0 ? '-' : '';
    const abs = Math.abs(v);
    return `${sign}$${abs.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-IE', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatEventType(t: string): string {
    return t === 'DeemedDisposal' ? '8-yr Deemed Disposal' : 'Sell';
  }

  trackById(_: number, e: TaxEventDto): number { return e.id; }
}

