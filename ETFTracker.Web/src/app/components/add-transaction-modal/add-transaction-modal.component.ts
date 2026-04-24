import { Component, EventEmitter, Output, Input, HostListener, ChangeDetectionStrategy, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, AssetTypeDeemedDisposalDefaultDto, CreateTransactionDto, TickerSearchResult } from '../../services/api.service';
import { debounceTime, Subject, switchMap, of } from 'rxjs';

@Component({
  selector: 'app-add-transaction-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-transaction-modal.component.html',
  styleUrls: ['./add-transaction-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddTransactionModalComponent implements OnInit {
  @Output() transactionAdded = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();
  @Input() isIrishInvestor = false;
  @Input() taxRate = 41;

  ticker: string = '';
  quantity: number = 0;
  purchasePrice: number = 0;
  purchaseDate: string = new Date().toISOString().split('T')[0];
  deemedDisposalDue: boolean = false;

  // Asset-type defaults cache
  private assetTypeDefaults: AssetTypeDeemedDisposalDefaultDto[] = [];

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

  ngOnInit(): void {
    // Pre-load asset-type defaults if Irish investor
    // (must be in ngOnInit so @Input() isIrishInvestor is already set)
    if (this.isIrishInvestor) {
      this.apiService.getAssetTypeDefaults().subscribe({
        next: (defaults) => { this.assetTypeDefaults = defaults; this.cdr.markForCheck(); },
        error: () => {}
      });
    }
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
    // Pre-fill deemedDisposalDue from asset-type defaults
    if (this.isIrishInvestor && result.quoteType) {
      const def = this.assetTypeDefaults.find(
        d => d.assetType.toUpperCase() === result.quoteType!.toUpperCase()
      );
      this.deemedDisposalDue = def?.deemedDisposalDue ?? false;
    }
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
    if (!this.validateForm()) {
      return;
    }

    this.loading = true;
    this.error = null;

    const transaction: CreateTransactionDto = {
      ticker: this.ticker.toUpperCase(),
      quantity: this.quantity,
      purchasePrice: this.purchasePrice,
      purchaseDate: this.purchaseDate,
      isIrishInvestor: this.isIrishInvestor,
      taxRate: this.taxRate,
      deemedDisposalDue: this.isIrishInvestor ? this.deemedDisposalDue : false,
      assetType: this.selectedResult?.quoteType ?? null
    };

    this.apiService.addTransaction(transaction).subscribe({
      next: () => {
        // Persist the asset-type default preference for next time
        if (this.isIrishInvestor && this.selectedResult?.quoteType) {
          this.apiService.upsertAssetTypeDefault({
            assetType: this.selectedResult.quoteType,
            deemedDisposalDue: this.deemedDisposalDue
          }).subscribe({ error: () => {} });
        }
        this.loading = false;
        this.cdr.markForCheck();
        this.transactionAdded.emit();
      },
      error: (err) => {
        console.error('Error adding transaction:', err);
        this.error = 'Failed to add transaction. Please try again.';
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
      this.error = 'Please enter a ticker or ISIN';
      this.cdr.markForCheck();
      return false;
    }

    if (this.quantity <= 0) {
      this.error = 'Quantity must be greater than 0';
      this.cdr.markForCheck();
      return false;
    }

    if (this.purchasePrice <= 0) {
      this.error = 'Purchase price must be greater than 0';
      this.cdr.markForCheck();
      return false;
    }

    if (!this.purchaseDate) {
      this.error = 'Please select a purchase date';
      this.cdr.markForCheck();
      return false;
    }

    return true;
  }

  getQuoteTypeIcon(quoteType: string | null): string {
    switch ((quoteType ?? '').toUpperCase()) {
      case 'EQUITY':      return '📈';
      case 'ETF':         return '📊';
      case 'MUTUALFUND':  return '💼';
      case 'INDEX':       return '📉';
      case 'CURRENCY':    return '💱';
      case 'CRYPTOCURRENCY': return '₿';
      case 'FUTURE':      return '🏗️';
      case 'BOND':        return '🏦';
      default:            return '🔍';
    }
  }
}

