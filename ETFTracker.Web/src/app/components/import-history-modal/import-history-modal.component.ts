import {
  Component, EventEmitter, Output, HostListener,
  ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, Subject, debounceTime, switchMap, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import {
  ApiService, ImportTransactionRowDto, TickerSearchResult
} from '../../services/api.service';
import {
  CsvParserService, BrokerPreset, BROKER_PRESETS, ParsedImportRow, ValidationError
} from '../../services/csv-parser.service';

export type ImportStep = 'upload' | 'resolving' | 'preview' | 'importing' | 'done';

export interface PreviewRow extends ParsedImportRow {
  /** Options shown in the ticker picker dropdown (populated when isIsin && multiple results) */
  tickerOptions: TickerSearchResult[];
  /** Status: ready | pick | error */
  status: 'ready' | 'pick' | 'error';
  statusMessage?: string;
  /** Set to true if user deleted this row from the preview */
  deleted: boolean;
  // inline search state (for error rows)
  searchQuery: string;
  searchResults: TickerSearchResult[];
  searchLoading: boolean;
  showSearchDropdown: boolean;
}

export type SortCol = 'rowIndex' | 'operation' | 'date' | 'ticker' | 'quantity' | 'price' | 'status';

@Component({
  selector: 'app-import-history-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './import-history-modal.component.html',
  styleUrls: ['./import-history-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportHistoryModalComponent implements OnDestroy {
  @Output() imported = new EventEmitter<number>();
  @Output() cancelled = new EventEmitter<void>();

  readonly presets = BROKER_PRESETS;
  readonly selectedPreset: BrokerPreset = BROKER_PRESETS[0]; // always native

  step: ImportStep = 'upload';
  dragOver = false;

  // upload step
  fileName = '';
  validationErrors: ValidationError[] = [];

  // preview step
  previewRows: PreviewRow[] = [];

  sortCol: SortCol = 'date';
  sortDir: 'asc' | 'desc' = 'asc';

  // done step
  importedCount = 0;

  // error
  errorMessage: string | null = null;

  private searchSubjects = new Map<number, Subject<string>>();
  private destroy$ = new Subject<void>();

  constructor(
    private apiService: ApiService,
    private csvParser: CsvParserService,
    public cdr: ChangeDetectorRef
  ) {}

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.searchSubjects.forEach(s => s.complete());
  }

  // ── Sorting ──────────────────────────────────────────────────────────────────

  sortBy(col: SortCol): void {
    if (this.sortCol === col) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortCol = col;
      this.sortDir = 'asc';
    }
    this.cdr.markForCheck();
  }

  get sortedRows(): PreviewRow[] {
    const rows = this.previewRows.filter(r => !r.deleted);
    const dir = this.sortDir === 'asc' ? 1 : -1;
    return [...rows].sort((a, b) => {
      let av: any, bv: any;
      switch (this.sortCol) {
        case 'rowIndex':  av = a.rowIndex;  bv = b.rowIndex; break;
        case 'operation': av = a.operation; bv = b.operation; break;
        case 'date':      av = a.date;      bv = b.date; break;
        case 'ticker':
          av = (a.resolvedTicker ?? a.tickerOrIsin).toLowerCase();
          bv = (b.resolvedTicker ?? b.tickerOrIsin).toLowerCase(); break;
        case 'quantity':  av = a.quantity;  bv = b.quantity; break;
        case 'price':     av = a.price;     bv = b.price; break;
        case 'status': {
          const order: Record<string, number> = { error: 0, pick: 1, ready: 2 };
          av = order[a.status]; bv = order[b.status]; break;
        }
        default: return 0;
      }
      if (av < bv) return -dir;
      if (av > bv) return dir;
      return 0;
    });
  }

  sortIcon(col: SortCol): string {
    if (this.sortCol !== col) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  // ── Template helpers ─────────────────────────────────────────────────────────

  get activeRows(): PreviewRow[] {
    return this.previewRows.filter(r => !r.deleted);
  }

  get readyCount(): number {
    return this.activeRows.filter(r => r.status === 'ready').length;
  }

  get pendingCount(): number {
    return this.activeRows.filter(r => r.status === 'pick').length;
  }

  get errorCount(): number {
    return this.activeRows.filter(r => r.status === 'error').length;
  }

  get canConfirm(): boolean {
    return this.activeRows.length > 0
      && this.pendingCount === 0
      && this.errorCount === 0;
  }

  // ── File input / drag-drop ───────────────────────────────────────────────────

  onFileInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.handleFile(file);
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) this.handleFile(file);
    this.cdr.markForCheck();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver = true;
    this.cdr.markForCheck();
  }

  onDragLeave(): void {
    this.dragOver = false;
    this.cdr.markForCheck();
  }

  downloadTemplate(): void {
    this.csvParser.downloadTemplate(this.selectedPreset);
  }

  // ── Row actions ──────────────────────────────────────────────────────────────

  deleteRow(row: PreviewRow): void {
    row.deleted = true;
    row.showSearchDropdown = false;
    this.cdr.markForCheck();
  }

  onTickerSelected(row: PreviewRow, symbol: string): void {
    row.resolvedTicker = symbol;
    row.status = symbol ? 'ready' : 'pick';
    this.cdr.markForCheck();
  }

  // ── Inline search for error rows ─────────────────────────────────────────────

  onRowSearchChange(row: PreviewRow, query: string): void {
    row.searchQuery = query;
    row.resolvedTicker = null;
    row.status = 'error';
    this.getOrCreateSearchSubject(row).next(query);
  }

  selectRowSearchResult(row: PreviewRow, result: TickerSearchResult): void {
    row.resolvedTicker = result.symbol;
    row.searchQuery = result.symbol;
    row.status = 'ready';
    row.showSearchDropdown = false;
    row.searchResults = [];
    this.cdr.markForCheck();
  }

  closeRowDropdown(row: PreviewRow): void {
    row.showSearchDropdown = false;
    this.cdr.markForCheck();
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

  private getOrCreateSearchSubject(row: PreviewRow): Subject<string> {
    if (this.searchSubjects.has(row.rowIndex)) {
      return this.searchSubjects.get(row.rowIndex)!;
    }
    const subject = new Subject<string>();
    this.searchSubjects.set(row.rowIndex, subject);

    subject.pipe(
      debounceTime(350),
      switchMap(query => {
        if (!query.trim()) {
          row.searchResults = [];
          row.showSearchDropdown = false;
          row.searchLoading = false;
          this.cdr.markForCheck();
          return of([]);
        }
        row.searchLoading = true;
        this.cdr.markForCheck();
        return this.apiService.searchTickers(query);
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: results => {
        row.searchResults = results;
        row.showSearchDropdown = results.length > 0;
        row.searchLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        row.searchResults = [];
        row.showSearchDropdown = false;
        row.searchLoading = false;
        this.cdr.markForCheck();
      }
    });

    return subject;
  }

  // ── Confirm import ───────────────────────────────────────────────────────────

  onConfirmImport(): void {
    if (!this.canConfirm) return;

    this.step = 'importing';
    this.errorMessage = null;
    this.cdr.markForCheck();

    const rows: ImportTransactionRowDto[] = this.activeRows.map(r => ({
      operation: r.operation,
      ticker: r.resolvedTicker!,
      quantity: r.quantity,
      price: r.price,
      date: r.date,
    }));

    this.apiService.importTransactions(rows)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.importedCount = result.imported;
          this.step = 'done';
          this.cdr.markForCheck();
        },
        error: err => {
          this.errorMessage = err?.error?.message
            ?? 'Import failed. No changes were saved. Please try again.';
          this.step = 'preview';
          this.cdr.markForCheck();
        },
      });
  }

  onDone(): void { this.imported.emit(this.importedCount); }
  onCancel(): void { this.cancelled.emit(); }

  @HostListener('document:keydown.escape')
  onEscape(): void { if (this.step !== 'importing') this.onCancel(); }

  // ── File processing ──────────────────────────────────────────────────────────

  private handleFile(file: File): void {
    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.validationErrors = [{ rowIndex: 0, column: 'file', message: 'Only .csv files are supported.' }];
      this.cdr.markForCheck();
      return;
    }

    this.fileName = file.name;
    this.validationErrors = [];
    this.previewRows = [];
    this.errorMessage = null;
    this.cdr.markForCheck();

    const reader = new FileReader();
    reader.onload = e => {
      const text = e.target?.result as string;
      const result = this.csvParser.parse(text, this.selectedPreset);

      if (result.errors.length > 0) {
        this.validationErrors = result.errors;
        this.cdr.markForCheck();
        return;
      }

      // Build preview rows
      this.previewRows = result.rows.map(r => ({
        ...r,
        tickerOptions: [],
        status: r.isIsin ? 'pick' : 'ready',
        deleted: false,
        searchQuery: '',
        searchResults: [],
        searchLoading: false,
        showSearchDropdown: false,
      }));

      this.resolveIsins();
    };
    reader.readAsText(file);
  }

  private resolveIsins(): void {
    const isinRows = this.previewRows.filter(r => r.isIsin && !r.deleted);

    if (isinRows.length === 0) {
      this.step = 'preview';
      this.cdr.markForCheck();
      return;
    }

    this.step = 'resolving';
    this.cdr.markForCheck();

    // Deduplicate ISINs
    const uniqueIsins = [...new Set(isinRows.map(r => r.tickerOrIsin.toUpperCase()))];

    const searches$ = uniqueIsins.map(isin =>
      this.apiService.searchTickers(isin)
    );

    forkJoin(searches$)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: resultsPerIsin => {
          uniqueIsins.forEach((isin, i) => {
            const results = resultsPerIsin[i];
            const rowsForIsin = this.previewRows.filter(r => r.tickerOrIsin.toUpperCase() === isin);

            rowsForIsin.forEach(row => {
              if (results.length === 0) {
                row.status = 'error';
                row.statusMessage = `No tickers found for ${isin}. Search for the correct ticker below.`;
              } else if (results.length === 1) {
                row.resolvedTicker = results[0].symbol;
                row.status = 'ready';
                row.tickerOptions = results;
              } else {
                row.tickerOptions = results;
                row.status = 'pick';
              }
            });
          });

          this.step = 'preview';
          this.cdr.markForCheck();
        },
        error: () => {
          // Mark all ISIN rows as needing manual entry on error
          isinRows.forEach(r => {
            r.status = 'error';
            r.statusMessage = 'ISIN lookup failed. Search for the correct ticker below.';
          });
          this.step = 'preview';
          this.cdr.markForCheck();
        },
      });
  }
}

