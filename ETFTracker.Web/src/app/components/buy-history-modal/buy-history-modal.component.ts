import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, TransactionDto } from '../../services/api.service';

@Component({
  selector: 'app-buy-history-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './buy-history-modal.component.html',
  styleUrls: ['./buy-history-modal.component.scss']
})
export class BuyHistoryModalComponent implements OnInit {
  @Input() holdingId: number = 0;
  @Output() closed = new EventEmitter<void>();

  transactions: TransactionDto[] = [];
  loading = true;
  error: string | null = null;

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadHistory();
  }

  private loadHistory(): void {
    this.loading = true;
    this.error = null;

    this.apiService.getHoldingHistory(this.holdingId).subscribe({
      next: (data) => {
        this.transactions = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading history:', err);
        this.error = 'Failed to load transaction history.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('de-IE', {
      style: 'currency',
      currency: 'EUR',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('de-IE', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).format(date);
  }

  formatQuantity(value: number): string {
    return value.toFixed(4);
  }

  formatPercent(value: number): string {
    const sign = value >= 0 ? '+' : '';
    return sign + value.toFixed(2) + '%';
  }

  onClose(): void {
    this.closed.emit();
  }
}

