import { Component, EventEmitter, Input, Output, HostListener, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, CreateTransactionDto, TickerSearchResult } from '../../services/api.service';
import { debounceTime, Subject, switchMap, of } from 'rxjs';

@Component({
  selector: 'app-add-transaction-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-transaction-modal.component.html',
  styleUrls: ['./add-transaction-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddTransactionModalComponent {
  @Output() transactionAdded = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  /** Pre-selected ticker (e.g. when opened via per-row Sell button). */
  @Input() set preselectedTicker(val: string | null) {
    if (val) {
      this.ticker = val;
      this.showDropdown = false;
    }
  }
  /** Pre-selected transaction type. */
  @Input() set initialType(val: 'Buy' | 'Sell') {
    this.transactionType = val;
  }
  /** Available quantity for the selected holding (used for sell validation hint). */
  @Input() availableQty: number | null = null;
  /** Current market price of the pre-selected holding (shown in sell mode). */
  @Input() currentMarketPrice: number | null = null;
  /** Average cost of the pre-selected holding (shown in sell mode). */
  @Input() averageCost: number | null = null;

  transactionType: 'Buy' | 'Sell' = 'Buy';
  ticker: string = '';
  quantity: number = 0;
  purchasePrice: number = 0;
  purchaseDate: string = new Date().toISOString().split('T')[0];

  // Search
  searchResults: TickerSearchResult[] = [];
  showDropdown = false;
  loadingSearch = false;
  selectedResult: TickerSearchResult | null = null;

  loading = false;
  error: string | null = null;

  private searchSubject = new Subject<string>();

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef) {
    this.searchSubject.pipe(
      debounceTime(350),
      switchMap(query => {
        if (!query.trim()) {
          this.loadingSearch = false;
          this.searchResults = [];
          this.showDropdown = false;
          this.cdr.markForCheck();
          return of([]);
        }
        this.loadingSearch = true;
        this.cdr.markForCheck();
        return this.apiService.searchTickers(query);
      })
    ).subscribe({
      next: (results) => {
        this.searchResults = results;
        this.showDropdown = results.length > 0;
        this.loadingSearch = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.searchResults = [];
        this.showDropdown = false;
        this.loadingSearch = false;
        this.cdr.markForCheck();
      }
    });
  }

  setType(type: 'Buy' | 'Sell'): void {
    this.transactionType = type;
    this.error = null;
    this.cdr.markForCheck();
  }

  /** Unrealized gain/loss per unit vs avg cost, for the sell preview panel. */
  get sellPreviewGainPerUnit(): number | null {
    if (this.currentMarketPrice === null || this.averageCost === null) return null;
    return this.currentMarketPrice - this.averageCost;
  }

  get sellPreviewGainPercent(): number | null {
    if (this.sellPreviewGainPerUnit === null || !this.averageCost) return null;
    return (this.sellPreviewGainPerUnit / this.averageCost) * 100;
  }

  onTickerChange(value: string): void {
    this.selectedResult = null;
    this.searchSubject.next(value);
  }

  selectResult(result: TickerSearchResult): void {
    this.ticker = result.symbol;
    this.selectedResult = result;
    this.showDropdown = false;
    this.searchResults = [];
    this.cdr.markForCheck();
  }

  closeDropdown(): void {
    this.showDropdown = false;
    this.cdr.markForCheck();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.showDropdown = false;
    this.cdr.markForCheck();
  }

  onSubmit(): void {
    if (!this.validateForm()) return;

    this.loading = true;
    this.error = null;

    const transaction: CreateTransactionDto = {
      ticker: this.ticker.toUpperCase(),
      transactionType: this.transactionType,
      quantity: this.quantity,
      purchasePrice: this.purchasePrice,
      purchaseDate: this.purchaseDate
    };

    this.apiService.addTransaction(transaction).subscribe({
      next: () => {
        this.loading = false;
        this.cdr.markForCheck();
        this.transactionAdded.emit();
      },
      error: (err) => {
        const msg = err?.error?.message;
        this.error = msg || 'Failed to add transaction. Please try again.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  private validateForm(): boolean {
    if (!this.ticker.trim()) {
      this.error = 'Please enter a ticker';
      this.cdr.markForCheck();
      return false;
    }
    if (this.quantity <= 0) {
      this.error = 'Quantity must be greater than 0';
      this.cdr.markForCheck();
      return false;
    }
    if (this.transactionType === 'Sell' && this.availableQty !== null && this.quantity > this.availableQty) {
      this.error = `Cannot sell ${this.quantity} — only ${this.availableQty} units available`;
      this.cdr.markForCheck();
      return false;
    }
    if (this.purchasePrice <= 0) {
      this.error = `${this.transactionType === 'Sell' ? 'Sell' : 'Purchase'} price must be greater than 0`;
      this.cdr.markForCheck();
      return false;
    }
    if (!this.purchaseDate) {
      this.error = 'Please select a date';
      this.cdr.markForCheck();
      return false;
    }
    return true;
  }

  getQuoteTypeIcon(quoteType: string | null): string {
    switch ((quoteType ?? '').toUpperCase()) {
      case 'EQUITY':         return '📈';
      case 'ETF':            return '📊';
      case 'MUTUALFUND':     return '💼';
      case 'INDEX':          return '📉';
      case 'CURRENCY':       return '💱';
      case 'CRYPTOCURRENCY': return '₿';
      case 'FUTURE':         return '🏗️';
      case 'BOND':           return '🏦';
      default:               return '🔍';
    }
  }

  formatCurrency(v: number): string {
    return new Intl.NumberFormat('de-IE', { style: 'currency', currency: 'EUR',
      minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v);
  }

  formatPercent(v: number): string {
    return (v >= 0 ? '+' : '') + v.toFixed(2) + '%';
  }
}

