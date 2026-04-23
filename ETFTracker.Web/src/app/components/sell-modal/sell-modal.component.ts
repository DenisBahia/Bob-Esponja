import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ApiService, HoldingDto, ProjectionSettingsDto,
  SellPreviewDto, SellRecordDto, SellRequestDto
} from '../../services/api.service';

@Component({
  selector: 'app-sell-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sell-modal.component.html',
  styleUrls: ['./sell-modal.component.scss']
})
export class SellModalComponent implements OnInit {
  @Input() holding!: HoldingDto;
  @Input() isIrishInvestor: boolean = true;
  @Input() projectionSettings: ProjectionSettingsDto | null = null;

  @Output() sold = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  // Steps: 1 = enter details, 2 = review & confirm, 3 = confirmation
  step: 1 | 2 | 3 = 1;

  // Step 1 form
  quantity: number = 0;
  sellPrice: number = 0;
  sellDate: string = '';

  // Step 2
  preview: SellPreviewDto | null = null;
  editableTaxRate: number = 0;
  previewing = false;
  confirming = false;
  error: string | null = null;

  // Step 3 confirmation
  confirmationMessage: string = '';
  confirmedRecord: SellRecordDto | null = null;

  get proceeds(): number {
    return this.quantity * this.sellPrice;
  }

  get cgtDuePreview(): number {
    if (!this.preview) return 0;
    if (this.preview.taxType === 'CGT') return 0;
    const profit = this.preview.totalProfit;
    return Math.max(0, profit) * this.editableTaxRate / 100;
  }

  get taxRateLabel(): string {
    return this.isIrishInvestor ? 'CGT / Exit Tax %' : 'CGT Rate (%)';
  }

  get taxDueLabel(): string {
    if (!this.preview) return this.isIrishInvestor ? 'Exit Tax Due' : 'CGT Due';
    return this.preview.taxType === 'ExitTax' ? 'Exit Tax Due' : 'CGT Due';
  }

  get lotCostBasisLabel(): string {
    return this.isIrishInvestor ? 'Adjusted Cost' : 'Cost Basis';
  }

  get isCgtSell(): boolean {
    return !this.preview || this.preview.taxType !== 'ExitTax';
  }

  ngOnInit(): void {
    this.sellPrice = this.holding.currentPrice;
    this.sellDate = new Date().toISOString().slice(0, 10);
    this.editableTaxRate = this.isIrishInvestor
      ? (this.projectionSettings?.exitTaxPercent ?? 38)
      : (this.projectionSettings?.cgtPercent ?? 0);
  }

  calculateTax(): void {
    if (this.quantity <= 0 || this.sellPrice <= 0) {
      this.error = 'Please enter a valid quantity and sell price.';
      return;
    }
    if (this.quantity > this.holding.availableQuantity) {
      this.error = `Quantity exceeds available (${this.holding.availableQuantity.toFixed(4)}).`;
      return;
    }
    this.error = null;
    this.previewing = true;

    const dto: SellRequestDto = {
      quantity: this.quantity,
      sellPrice: this.sellPrice,
      sellDate: this.sellDate,
      isIrishInvestor: this.isIrishInvestor,
      taxRate: this.editableTaxRate
    };

    this.apiService.previewSell(this.holding.id, dto).subscribe({
      next: (preview) => {
        this.preview = preview;
        this.editableTaxRate = preview.taxRateUsed;
        this.previewing = false;
        this.step = 2;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Failed to calculate tax breakdown.';
        this.previewing = false;
        this.cdr.markForCheck();
      }
    });
  }

  confirmSell(): void {
    if (this.confirming) return;
    this.confirming = true;
    this.error = null;

    const dto: SellRequestDto = {
      quantity: this.quantity,
      sellPrice: this.sellPrice,
      sellDate: this.sellDate,
      isIrishInvestor: this.isIrishInvestor,
      taxRate: this.editableTaxRate
    };

    this.apiService.confirmSell(this.holding.id, dto).subscribe({
      next: (record) => {
        this.confirmedRecord = record;
        const year = new Date(this.sellDate).getFullYear();
        const ticker = this.holding.ticker;

        if (record.taxType === 'ExitTax') {
          this.confirmationMessage = `Exit Tax of ${this.formatCurrency(record.taxAmountSaved)} recorded for ${ticker} (${year} pot).`;
        } else if (record.totalProfit < 0) {
          this.confirmationMessage = `Loss of ${this.formatCurrency(Math.abs(record.totalProfit))} recorded for ${year}. It will be offset against your CGT profits within ${year}.`;
        } else {
          this.confirmationMessage = `Profit of ${this.formatCurrency(record.totalProfit)} recorded. Run the year-end recalculation in the Tax Summary to update your CGT estimate.`;
        }

        // Update tax rate if changed
        const originalRate = this.isIrishInvestor
          ? (this.projectionSettings?.exitTaxPercent ?? 38)
          : (this.projectionSettings?.cgtPercent ?? 0);
        if (this.projectionSettings && this.editableTaxRate !== originalRate) {
          const updated = { ...this.projectionSettings };
          if (this.isIrishInvestor) updated.exitTaxPercent = this.editableTaxRate;
          else updated.cgtPercent = this.editableTaxRate;
          this.apiService.saveProjectionSettings(updated).subscribe();
        }

        this.confirming = false;
        this.step = 3;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Failed to confirm sell.';
        this.confirming = false;
        this.cdr.markForCheck();
      }
    });
  }

  onConfirmationDone(): void {
    this.sold.emit();
  }

  goBack(): void {
    this.step = 1;
    this.preview = null;
    this.error = null;
    this.cdr.markForCheck();
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  formatCurrency(value: number): string {
    const sign = value < 0 ? '-' : '';
    const abs = Math.abs(value);
    return `${sign}€${abs.toLocaleString('en-IE', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('en-IE');
  }

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef) {}
}
