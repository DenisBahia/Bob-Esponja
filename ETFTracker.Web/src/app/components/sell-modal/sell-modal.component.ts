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

  // Steps: 1 = enter details, 2 = review & confirm
  step: 1 | 2 = 1;

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

  get proceeds(): number {
    return this.quantity * this.sellPrice;
  }

  get cgtDuePreview(): number {
    if (!this.preview) return 0;
    const profit = this.preview.totalProfit;
    return Math.max(0, profit) * this.editableTaxRate / 100;
  }

  get taxRateLabel(): string {
    return this.isIrishInvestor ? 'Exit Tax Rate (%)' : 'CGT Rate (%)';
  }

  get taxDueLabel(): string {
    return this.isIrishInvestor ? 'Exit Tax Due' : 'CGT Due';
  }

  get lotCostBasisLabel(): string {
    return this.isIrishInvestor ? 'Adjusted Cost' : 'Cost Basis';
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
      next: () => {
        // If tax rate changed, update projection settings
        const originalRate = this.isIrishInvestor
          ? (this.projectionSettings?.exitTaxPercent ?? 38)
          : (this.projectionSettings?.cgtPercent ?? 0);

        if (this.projectionSettings && this.editableTaxRate !== originalRate) {
          const updated = { ...this.projectionSettings };
          if (this.isIrishInvestor) {
            updated.exitTaxPercent = this.editableTaxRate;
          } else {
            updated.cgtPercent = this.editableTaxRate;
          }
          this.apiService.saveProjectionSettings(updated).subscribe();
        }

        this.confirming = false;
        this.sold.emit();
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Failed to confirm sell.';
        this.confirming = false;
        this.cdr.markForCheck();
      }
    });
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
    return new Intl.NumberFormat('de-IE', {
      style: 'currency', currency: 'EUR',
      minimumFractionDigits: 2, maximumFractionDigits: 2
    }).format(value);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('en-IE');
  }

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef) {}
}

