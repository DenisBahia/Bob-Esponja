import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, TransactionDto, UpdateTransactionDto, SellRecordDto, UpdateSellRecordDto } from '../../services/api.service';
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

  // ── Sells tab ─────────────────────────────────────────────────────────────────
  activeTab: 'buys' | 'sells' = 'buys';
  sellsLoaded = false;
  sellsLoading = false;
  sellRecords: SellRecordDto[] = [];
  expandedSellId: number | null = null;

  // Sell edit/delete
  editingSellId: number | null = null;
  editSellForm: UpdateSellRecordDto = { quantity: 0, sellPrice: 0, sellDate: '' };
  savingSellEdit = false;
  confirmDeleteSellId: number | null = null;
  deletingSellIds: Set<number> = new Set();

  // ── Buys tab ─────────────────────────────────────────────────────────────────
  transactions: TransactionDto[] = [];
  loading = true;
  error: string | null = null;

  confirmDeleteId: number | null = null;
  deletingIds: Set<number> = new Set();

  editingId: number | null = null;
  editForm: UpdateTransactionDto = { quantity: 0, purchasePrice: 0, purchaseDate: '' };
  savingEdit = false;

  constructor(
    private apiService: ApiService,
    private cdr: ChangeDetectorRef,
    public sharingCtx: SharingContextService
  ) {}

  ngOnInit(): void {
    this.loadHistory();
  }

  setTab(tab: 'buys' | 'sells'): void {
    this.activeTab = tab;
    if (tab === 'sells' && !this.sellsLoaded) {
      this.loadSells();
    }
    this.cdr.markForCheck();
  }

  private loadSells(): void {
    this.sellsLoading = true;
    this.apiService.getSellHistory(this.holdingId).subscribe({
      next: (data) => {
        this.sellRecords = data;
        this.sellsLoaded = true;
        this.sellsLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading sell history:', err);
        this.sellsLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleSellExpand(id: number): void {
    this.expandedSellId = this.expandedSellId === id ? null : id;
    this.cdr.markForCheck();
  }

  getSellTaxPaidLabel(record?: SellRecordDto): string {
    if (!record) return 'Tax Paid';
    return record.taxType === 'ExitTax' ? 'Exit Tax Paid' : 'CGT Paid';
  }

  getLotCostBasisLabel(record: SellRecordDto): string {
    return record.taxType === 'ExitTax' ? 'Adjusted Cost/Unit' : 'Cost Basis/Unit';
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
        console.error('Error updating transaction:', err);
        this.error = 'Failed to update transaction. Please try again.';
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
        console.error('Error deleting transaction:', err);
        this.error = err?.error?.message ?? 'Failed to delete transaction. Please try again.';
        const next = new Set(this.deletingIds);
        next.delete(id);
        this.deletingIds = next;
        this.cdr.detectChanges();
      }
    });
  }

  formatCurrency(value: number): string {
    const sign = value < 0 ? '-' : '';
    const abs = Math.abs(value);
    return `${sign}$${abs.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('de-IE', {
      year: 'numeric', month: '2-digit', day: '2-digit'
    }).format(date);
  }

  formatQuantity(value: number): string {
    return value.toFixed(4);
  }

  formatPercent(value: number): string {
    const sign = value >= 0 ? '+' : '';
    return sign + value.toFixed(2) + '%';
  }

  // ── Sell edit / delete ────────────────────────────────────────────────────────

  startEditSell(record: SellRecordDto): void {
    this.confirmDeleteSellId = null;
    this.expandedSellId = null;
    this.editingSellId = record.id;
    this.editSellForm = {
      quantity: record.quantity,
      sellPrice: record.sellPrice,
      sellDate: record.sellDate.slice(0, 10),
    };
    this.cdr.detectChanges();
  }

  cancelEditSell(): void {
    this.editingSellId = null;
    this.cdr.detectChanges();
  }

  saveEditSell(id: number): void {
    if (this.savingSellEdit) return;
    this.savingSellEdit = true;
    this.cdr.detectChanges();

    this.apiService.updateSellRecord(id, this.editSellForm).subscribe({
      next: () => {
        this.savingSellEdit = false;
        this.editingSellId = null;
        this.sellsLoaded = false;
        this.loadSells();
        this.changed.emit();
      },
      error: (err) => {
        console.error('Error updating sell record:', err);
        this.error = err?.error?.message ?? 'Failed to update sell record. Please try again.';
        this.savingSellEdit = false;
        this.cdr.detectChanges();
      }
    });
  }

  requestDeleteSell(id: number): void {
    this.editingSellId = null;
    this.expandedSellId = null;
    this.confirmDeleteSellId = id;
    this.cdr.detectChanges();
  }

  cancelDeleteSell(): void {
    this.confirmDeleteSellId = null;
    this.cdr.detectChanges();
  }

  confirmDeleteSell(id: number): void {
    this.confirmDeleteSellId = null;
    this.deletingSellIds = new Set(this.deletingSellIds).add(id);
    this.cdr.detectChanges();

    this.apiService.deleteSellRecord(id).subscribe({
      next: () => {
        this.sellRecords = this.sellRecords.filter(r => r.id !== id);
        const next = new Set(this.deletingSellIds);
        next.delete(id);
        this.deletingSellIds = next;
        this.changed.emit();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error deleting sell record:', err);
        this.error = err?.error?.message ?? 'Failed to delete sell record. Please try again.';
        const next = new Set(this.deletingSellIds);
        next.delete(id);
        this.deletingSellIds = next;
        this.cdr.detectChanges();
      }
    });
  }

  onClose(): void {
    this.closed.emit();
  }
}
