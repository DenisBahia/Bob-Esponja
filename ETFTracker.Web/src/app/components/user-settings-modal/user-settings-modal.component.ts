import { Component, EventEmitter, OnInit, Output, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, UserTaxDefaultsDto } from '../../services/api.service';

@Component({
  selector: 'app-user-settings-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-settings-modal.component.html',
  styleUrls: ['./user-settings-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserSettingsModalComponent implements OnInit {
  @Output() closed = new EventEmitter<UserTaxDefaultsDto | null>();

  loading = true;
  saving = false;
  error: string | null = null;
  success = false;

  defaults: UserTaxDefaultsDto = {
    isIrishInvestor: true,
    exitTaxPercent: 41,
    deemedDisposalPercent: 41,
    siaAnnualPercent: 0,
    cgtPercent: 33,
    taxFreeAllowancePerYear: 0,
  };

  constructor(private api: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.api.getTaxDefaults().subscribe({
      next: (d) => {
        this.defaults = { ...d };
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.error = 'Failed to load settings.';
        this.cdr.markForCheck();
      },
    });
  }

  save(): void {
    this.saving = true;
    this.error = null;
    this.success = false;
    this.api.saveTaxDefaults(this.defaults).subscribe({
      next: (saved) => {
        this.defaults = { ...saved };
        this.saving = false;
        this.success = true;
        this.cdr.markForCheck();
        setTimeout(() => {
          this.closed.emit(saved);
        }, 600);
      },
      error: () => {
        this.saving = false;
        this.error = 'Failed to save settings. Please try again.';
        this.cdr.markForCheck();
      },
    });
  }

  cancel(): void {
    this.closed.emit(null);
  }
}

