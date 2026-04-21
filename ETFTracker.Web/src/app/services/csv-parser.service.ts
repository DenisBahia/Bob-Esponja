import { Injectable } from '@angular/core';

// ── Broker Preset Definitions ──────────────────────────────────────────────────

export interface BrokerPreset {
  id: string;
  label: string;
  operationColumn: string;
  /** Exact cell value treated as BUY (case-insensitive comparison is applied) */
  buyValues: string[];
  /** Exact cell value treated as SELL (case-insensitive comparison is applied) */
  sellValues: string[];
  dateColumn: string;
  /** dayjs / date-fns style format string — used only as a hint in UI; actual parsing is flexible */
  dateFormatHint: string;
  tickerColumn: string;
  quantityColumn: string;
  priceColumn: string;
}

export const BROKER_PRESETS: BrokerPreset[] = [
  {
    id: 'native',
    label: 'Native (this app)',
    operationColumn: 'operation',
    buyValues: ['buy'],
    sellValues: ['sell'],
    dateColumn: 'date',
    dateFormatHint: 'YYYY-MM-DD',
    tickerColumn: 'ticker_or_isin',
    quantityColumn: 'quantity',
    priceColumn: 'price',
  },
  {
    id: 'trading212',
    label: 'Trading 212',
    operationColumn: 'Action',
    buyValues: ['market buy', 'limit buy', 'buy'],
    sellValues: ['market sell', 'limit sell', 'sell'],
    dateColumn: 'Time',
    dateFormatHint: 'DD/MM/YYYY HH:mm',
    tickerColumn: 'Ticker',
    quantityColumn: 'No. of shares',
    priceColumn: 'Price / share',
  },
  {
    id: 'degiro',
    label: 'DEGIRO',
    operationColumn: 'Tipo de ordem',
    buyValues: ['compra', 'buy'],
    sellValues: ['venda', 'sell'],
    dateColumn: 'Data',
    dateFormatHint: 'DD-MM-YYYY',
    tickerColumn: 'ISIN',
    quantityColumn: 'Quantidade',
    priceColumn: 'Preço',
  },
  {
    id: 'revolut',
    label: 'Revolut',
    operationColumn: 'Type',
    buyValues: ['buy'],
    sellValues: ['sell'],
    dateColumn: 'Date',
    dateFormatHint: 'YYYY-MM-DD',
    tickerColumn: 'Ticker',
    quantityColumn: 'Quantity',
    priceColumn: 'Price per share',
  },
  {
    id: 'interactive_brokers',
    label: 'Interactive Brokers',
    operationColumn: 'Buy/Sell',
    buyValues: ['buy'],
    sellValues: ['sell'],
    dateColumn: 'TradeDate',
    dateFormatHint: 'YYYY-MM-DD',
    tickerColumn: 'Symbol',
    quantityColumn: 'Quantity',
    priceColumn: 'TradePrice',
  },
];

// ── Parsed / Validated Row types ───────────────────────────────────────────────

export type OperationType = 'BUY' | 'SELL';

export interface ParsedImportRow {
  /** Row number in the CSV (1-based, not counting header) */
  rowIndex: number;
  operation: OperationType;
  /** Raw value from the ticker/ISIN column */
  tickerOrIsin: string;
  /** True when tickerOrIsin looks like an ISIN */
  isIsin: boolean;
  /** Resolved ticker symbol — set after ISIN resolution or when tickerOrIsin is already a ticker */
  resolvedTicker: string | null;
  quantity: number;
  price: number;
  date: string;  // normalised to YYYY-MM-DD
}

export interface ValidationError {
  rowIndex: number;
  column: string;
  message: string;
}

export interface ParseResult {
  rows: ParsedImportRow[];
  errors: ValidationError[];
}

// ── CsvParserService ───────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class CsvParserService {

  /** Parse raw CSV text using the given broker preset, returning validated rows + errors. */
  parse(csvText: string, preset: BrokerPreset): ParseResult {
    const lines = this.splitLines(csvText);
    if (lines.length < 2) {
      return { rows: [], errors: [{ rowIndex: 0, column: 'file', message: 'File is empty or has no data rows.' }] };
    }

    const headers = this.splitCsv(lines[0]).map(h => h.trim());
    const errors: ValidationError[] = [];

    // Validate required headers
    const required = [
      preset.operationColumn,
      preset.dateColumn,
      preset.tickerColumn,
      preset.quantityColumn,
      preset.priceColumn,
    ];
    for (const col of required) {
      if (!headers.some(h => h.toLowerCase() === col.toLowerCase())) {
        errors.push({ rowIndex: 0, column: col, message: `Required column "${col}" not found in file headers.` });
      }
    }
    if (errors.length) return { rows: [], errors };

    const idx = (col: string): number =>
      headers.findIndex(h => h.toLowerCase() === col.toLowerCase());

    const opIdx  = idx(preset.operationColumn);
    const dateIdx = idx(preset.dateColumn);
    const tickerIdx = idx(preset.tickerColumn);
    const qtyIdx = idx(preset.quantityColumn);
    const priceIdx = idx(preset.priceColumn);

    const rows: ParsedImportRow[] = [];
    const today = new Date().toISOString().slice(0, 10);

    for (let i = 1; i < lines.length; i++) {
      const line = lines[i].trim();
      if (!line || line.startsWith('#')) continue;

      const cells = this.splitCsv(line);
      const rowIndex = i;

      const rawOp    = (cells[opIdx]    ?? '').trim();
      const rawDate  = (cells[dateIdx]  ?? '').trim();
      const rawTicker = (cells[tickerIdx] ?? '').trim();
      const rawQty   = (cells[qtyIdx]   ?? '').trim();
      const rawPrice = (cells[priceIdx] ?? '').trim();

      // Validate operation
      const opLower = rawOp.toLowerCase();
      let operation: OperationType | null = null;
      if (preset.buyValues.some(v => v.toLowerCase() === opLower)) operation = 'BUY';
      else if (preset.sellValues.some(v => v.toLowerCase() === opLower)) operation = 'SELL';
      else errors.push({ rowIndex, column: preset.operationColumn, message: `Unknown operation "${rawOp}". Expected: ${preset.buyValues[0]} / ${preset.sellValues[0]}` });

      // Validate date
      const normDate = this.parseDate(rawDate);
      if (!normDate) {
        errors.push({ rowIndex, column: preset.dateColumn, message: `Cannot parse date "${rawDate}". Use YYYY-MM-DD or DD/MM/YYYY.` });
      } else if (normDate > today) {
        errors.push({ rowIndex, column: preset.dateColumn, message: `Date "${rawDate}" is in the future.` });
      }

      // Validate ticker
      if (!rawTicker) {
        errors.push({ rowIndex, column: preset.tickerColumn, message: 'Ticker / ISIN cannot be empty.' });
      }

      // Validate quantity
      const qty = parseFloat(rawQty.replace(',', '.'));
      if (isNaN(qty) || qty <= 0) {
        errors.push({ rowIndex, column: preset.quantityColumn, message: `Quantity "${rawQty}" must be a positive number.` });
      }

      // Validate price
      const price = parseFloat(rawPrice.replace(',', '.'));
      if (isNaN(price) || price <= 0) {
        errors.push({ rowIndex, column: preset.priceColumn, message: `Price "${rawPrice}" must be a positive number.` });
      }

      if (operation && normDate && rawTicker && !isNaN(qty) && qty > 0 && !isNaN(price) && price > 0) {
        const isIsin = this.looksLikeIsin(rawTicker);
        rows.push({
          rowIndex,
          operation,
          tickerOrIsin: rawTicker,
          isIsin,
          resolvedTicker: isIsin ? null : rawTicker.toUpperCase(),
          quantity: qty,
          price,
          date: normDate,
        });
      }
    }

    return { rows, errors };
  }

  /** Generate a downloadable CSV template for a given preset. */
  downloadTemplate(preset: BrokerPreset): void {
    const header = [
      preset.operationColumn,
      preset.dateColumn,
      preset.tickerColumn,
      preset.quantityColumn,
      preset.priceColumn,
    ].join(',');

    const buyOp  = preset.buyValues[0];
    const sellOp = preset.sellValues[0];

    const rows = [
      [buyOp,  '2024-01-15', 'VWRL',         '10', '98.50' ].join(','),
      [buyOp,  '2024-03-10', 'IE00B3RBWM25', '5',  '45.00' ].join(','),
      [sellOp, '2025-06-01', 'AAPL',          '2', '210.00'].join(','),
    ].join('\n');

    const csv = `${header}\n${rows}`;
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `import_template_${preset.id}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  // ── Private helpers ──────────────────────────────────────────────────────────

  /** Normalise a date string to YYYY-MM-DD. Accepts: YYYY-MM-DD, DD/MM/YYYY, DD-MM-YYYY, DD/MM/YYYY HH:mm */
  private parseDate(raw: string): string | null {
    if (!raw) return null;

    // Strip time component if present
    const datePart = raw.split(' ')[0].split('T')[0];

    // YYYY-MM-DD
    if (/^\d{4}-\d{2}-\d{2}$/.test(datePart)) {
      const d = new Date(datePart + 'T00:00:00');
      return isNaN(d.getTime()) ? null : datePart;
    }

    // DD/MM/YYYY or DD-MM-YYYY
    const m = datePart.match(/^(\d{2})[\/\-](\d{2})[\/\-](\d{4})$/);
    if (m) {
      const iso = `${m[3]}-${m[2]}-${m[1]}`;
      const d = new Date(iso + 'T00:00:00');
      return isNaN(d.getTime()) ? null : iso;
    }

    return null;
  }

  private looksLikeIsin(value: string): boolean {
    return /^[A-Z]{2}[A-Z0-9]{10}$/.test(value.toUpperCase());
  }

  /** Split a line respecting quoted fields */
  private splitCsv(line: string): string[] {
    const result: string[] = [];
    let current = '';
    let inQuotes = false;
    for (let i = 0; i < line.length; i++) {
      const ch = line[i];
      if (ch === '"') {
        inQuotes = !inQuotes;
      } else if (ch === ',' && !inQuotes) {
        result.push(current.trim().replace(/^"|"$/g, ''));
        current = '';
      } else {
        current += ch;
      }
    }
    result.push(current.trim().replace(/^"|"$/g, ''));
    return result;
  }

  private splitLines(text: string): string[] {
    return text.split(/\r?\n/).filter(l => l.trim().length > 0);
  }
}

