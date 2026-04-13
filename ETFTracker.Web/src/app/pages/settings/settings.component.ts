import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ApiService, AssetTaxRateDto } from '../../services/api.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsComponent implements OnInit {
  taxRates: AssetTaxRateDto[] = [];
  loading = true;
  error: string | null = null;
  saveError: string | null = null;
  saveSuccess: string | null = null;

  // Inline edit
  editingType: string | null = null;
  editForm: AssetTaxRateDto = { securityType: '', exitTaxPercent: 0, label: null };
  saving = false;

  // New row
  newForm: AssetTaxRateDto = { securityType: '', exitTaxPercent: 0, label: null };
  addingNew = false;
  savingNew = false;

  // Delete confirm
  confirmDeleteType: string | null = null;
  deletingTypes: Set<string> = new Set();

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.loadRates(); }

  private loadRates(): void {
    this.loading = true;
    this.error = null;
    this.apiService.getTaxRates().subscribe({
      next: (rates) => {
        this.taxRates = rates;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error = 'Failed to load tax rates.';
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  startEdit(rate: AssetTaxRateDto): void {
    this.confirmDeleteType = null;
    this.editingType = rate.securityType;
    this.editForm = { ...rate };
    this.saveError = null;
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    this.editingType = null;
    this.cdr.markForCheck();
  }

  saveEdit(): void {
    if (this.saving) return;
    this.saving = true;
    this.saveError = null;
    this.apiService.upsertTaxRate(this.editForm).subscribe({
      next: (updated) => {
        this.taxRates = this.taxRates.map(r => r.securityType === updated.securityType ? updated : r);
        this.editingType = null;
        this.saving = false;
        this.showSuccess('Saved!');
      },
      error: (err) => {
        this.saveError = err?.error?.message || 'Failed to save.';
        this.saving = false;
        this.cdr.markForCheck();
      }
    });
  }

  requestDelete(type: string): void {
    this.editingType = null;
    this.confirmDeleteType = type;
    this.cdr.markForCheck();
  }

  cancelDelete(): void {
    this.confirmDeleteType = null;
    this.cdr.markForCheck();
  }

  confirmDelete(type: string): void {
    this.confirmDeleteType = null;
    this.deletingTypes = new Set(this.deletingTypes).add(type);
    this.cdr.markForCheck();
    this.apiService.deleteTaxRate(type).subscribe({
      next: () => {
        this.taxRates = this.taxRates.filter(r => r.securityType !== type);
        const next = new Set(this.deletingTypes);
        next.delete(type);
        this.deletingTypes = next;
        this.showSuccess('Deleted.');
      },
      error: (err) => {
        this.saveError = err?.error?.message || 'Failed to delete.';
        const next = new Set(this.deletingTypes);
        next.delete(type);
        this.deletingTypes = next;
        this.cdr.markForCheck();
      }
    });
  }

  toggleAddNew(): void {
    this.addingNew = !this.addingNew;
    if (this.addingNew) {
      this.newForm = { securityType: '', exitTaxPercent: 0, label: null };
    }
    this.cdr.markForCheck();
  }

  saveNew(): void {
    if (this.savingNew) return;
    if (!this.newForm.securityType.trim()) {
      this.saveError = 'Security type is required.';
      this.cdr.markForCheck();
      return;
    }
    this.savingNew = true;
    this.saveError = null;
    this.apiService.upsertTaxRate({ ...this.newForm, securityType: this.newForm.securityType.toUpperCase().trim() }).subscribe({
      next: (created) => {
        const existing = this.taxRates.find(r => r.securityType === created.securityType);
        this.taxRates = existing
          ? this.taxRates.map(r => r.securityType === created.securityType ? created : r)
          : [...this.taxRates, created].sort((a, b) => a.securityType.localeCompare(b.securityType));
        this.addingNew = false;
        this.savingNew = false;
        this.showSuccess('Tax rate added!');
      },
      error: (err) => {
        this.saveError = err?.error?.message || 'Failed to save.';
        this.savingNew = false;
        this.cdr.markForCheck();
      }
    });
  }

  private showSuccess(msg: string): void {
    this.saveSuccess = msg;
    this.cdr.markForCheck();
    setTimeout(() => { this.saveSuccess = null; this.cdr.markForCheck(); }, 2500);
  }
}

