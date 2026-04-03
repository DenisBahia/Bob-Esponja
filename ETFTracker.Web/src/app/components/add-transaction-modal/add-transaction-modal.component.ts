import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, CreateTransactionDto } from '../../services/api.service';
import { debounceTime, Subject } from 'rxjs';

@Component({
  selector: 'app-add-transaction-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-transaction-modal.component.html',
  styleUrls: ['./add-transaction-modal.component.scss']
})
export class AddTransactionModalComponent {
  @Output() transactionAdded = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  ticker: string = '';
  quantity: number = 0;
  purchasePrice: number = 0;
  purchaseDate: string = new Date().toISOString().split('T')[0];
  etfDescription: string | null = null;
  loadingDescription = false;

  loading = false;
  error: string | null = null;

  private tickerSubject = new Subject<string>();

  constructor(private apiService: ApiService) {
    // Debounce ticker changes to avoid excessive API calls
    this.tickerSubject.pipe(
      debounceTime(500)
    ).subscribe(ticker => {
      this.fetchEtfDescription(ticker);
    });
  }

  onTickerChange(ticker: string): void {
    this.etfDescription = null;
    if (ticker.trim().length > 0) {
      this.tickerSubject.next(ticker.trim());
    }
  }

  private fetchEtfDescription(ticker: string): void {
    if (!ticker.trim()) {
      this.etfDescription = null;
      return;
    }

    this.loadingDescription = true;
    this.apiService.getEtfDescription(ticker.toUpperCase()).subscribe({
      next: (response) => {
        this.etfDescription = response.description && response.description !== 'ETF not found'
          ? response.description
          : null;
        this.loadingDescription = false;
      },
      error: (err) => {
        console.error('Error fetching ETF description:', err);
        this.etfDescription = null;
        this.loadingDescription = false;
      }
    });
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
      purchaseDate: this.purchaseDate
    };

    this.apiService.addTransaction(transaction).subscribe({
      next: () => {
        this.loading = false;
        this.transactionAdded.emit();
      },
      error: (err) => {
        console.error('Error adding transaction:', err);
        this.error = 'Failed to add transaction. Please try again.';
        this.loading = false;
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  private validateForm(): boolean {
    if (!this.ticker.trim()) {
      this.error = 'Please enter a ticker';
      return false;
    }

    if (this.quantity <= 0) {
      this.error = 'Quantity must be greater than 0';
      return false;
    }

    if (this.purchasePrice <= 0) {
      this.error = 'Purchase price must be greater than 0';
      return false;
    }

    if (!this.purchaseDate) {
      this.error = 'Please select a purchase date';
      return false;
    }

    return true;
  }
}

