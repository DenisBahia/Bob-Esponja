import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, TransactionDto, UpdateTransactionDto, SellAllocationDto } from '../../services/api.service';
import { SharingContextService } from '../../services/sharing-context.service';

@Component({
  selector: 'app-buy-history-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './buy-history-modal.component.html',
  styleUrls: ['./buy-history-modal.component.scss']
})
export class BuyHistoryModalComponent implements OnInit {
  @Input() holdingId: number = 0;
  @Output() closed = new EventEmitter<void>();
  /** Fires whenever a transaction is deleted so the parent can refresh its data. */
  @Output() changed = new EventEmitter<void>();

  transactions: TransactionDto[] = [];
  loading = true;
  error: string | null = null;

  confirmDeleteId: number | null = null;
  deletingIds: Set<number> = new Set();

  // Edit state (buys only)
  editingId: number | null = null;
  editForm: UpdateTransactionDto = { quantity: 0, purchasePrice: 0, purchaseDate: '' };
  savingEdit = false;

  // FIFO breakdown expand
  expandedSellId: number | null = null;

  constructor(
    private apiService: ApiService,
    private cdr: ChangeDetectorRef,
    public sharingCtx: SharingContextService
  ) {}

  ngOnInit(): void { this.loadHistory(); }

  get buyCount(): number  { return this.transactions.filter(t => t.transactionType === 'Buy').length; }
  get sellCount(): number { return this.transactions.filter(t => t.transactionType === 'Sell').length; }

  private loadHistory(): void {
    this.loading = true;
    this.error = null;
    this.apiService.getHoldingHistory(this.holdingId).subscribe({
      next: (data) => {
        this.transactions = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Failed to load transaction history.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleSellExpand(id: number): void {
    this.expandedSellId = this.expandedSellId === id ? null : id;
    this.cdr.detectChanges();
  }

  startEdit(t: TransactionDto): void {
    this.confirmDeleteId = null;
    this.editingId = t.id;
    this.editForm = {
      quantity: t.quantity,
      purchasePrice: t.purchasePrice,
      purchaseDate: t.purchaseDate.slice(0, 10),
    };
    this.cdr.detectChanges();
  }

  cancelEdit(): void {
    this.editingId = null;
    this.cdr.detectChanges();
  }

  saveEdit(id: number): void {
    if (this.savingEdit) return;
    this.savingEdit = true;
    this.cdr.detectChanges();
    this.apiService.updateTransaction(id, this.editForm).subscribe({
      next: () => {
        this.savingEdit = false;
        this.editingId = null;
        this.loadHistory();
        this.changed.emit();
      },
      error: (err) => {
        this.error = err?.error?.message || 'Failed to update transaction.';
        this.savingEdit = false;
        this.cdr.detectChanges();
      }
    });
  }

  requestDelete(id: number): void {
    this.editingId = null;
    this.confirmDeleteId = id;
    this.cdr.detectChanges();
  }

  cancelDelete(): void {
    this.confirmDeleteId = null;
    this.cdr.detectChanges();
  }

  confirmDelete(id: number): void {
    this.confirmDeleteId = null;
    this.deletingIds = new Set(this.deletingIds).add(id);
    this.cdr.detectChanges();
    this.apiService.deleteTransaction(id).subscribe({
      next: () => {
        this.transactions = this.transactions.filter(t => t.id !== id);
        const next = new Set(this.deletingIds);
        next.delete(id);
        this.deletingIds = next;
        this.changed.emit();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = err?.error?.message || 'Failed to delete transaction.';
        const next = new Set(this.deletingIds);
        next.delete(id);
        this.deletingIds = next;
        this.cdr.detectChanges();
      }
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('de-IE', { style: 'currency', currency: 'EUR', minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
  }
  formatDate(dateString: string): string {
    const [y, m, d] = dateString.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }
  formatQuantity(value: number): string { return value.toFixed(4); }
  formatPercent(value: number): string {
    return (value >= 0 ? '+' : '') + value.toFixed(2) + '%';
  }
  onClose(): void { this.closed.emit(); }
}
