import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PeriodMetrics {
  gainLossEur: number;
  gainLossPercent: number;
  pricesUnavailable: boolean;
}

export interface HoldingDto {
  id: number;
  ticker: string;
  etfName: string;
  quantity: number;
  averageCost: number;
  currentPrice: number;
  totalValue: number;
  priceUnavailable: boolean;
  priceSource: string | null;
  securityType: string | null;
  /** ISO date string (YYYY-MM-DD) of the next upcoming Deemed Disposal due date, or null if N/A. */
  deemedDisposalDueDate: string | null;
  dailyMetrics: PeriodMetrics;
  weeklyMetrics: PeriodMetrics;
  monthlyMetrics: PeriodMetrics;
  ytdMetrics: PeriodMetrics;
}

export interface DashboardHeaderDto {
  totalHoldingsAmount: number;
  totalInvestedAmount: number;
  totalVariation: PeriodMetrics;
  dailyMetrics: PeriodMetrics;
  weeklyMetrics: PeriodMetrics;
  monthlyMetrics: PeriodMetrics;
  ytdMetrics: PeriodMetrics;
}

export interface DashboardDto {
  header: DashboardHeaderDto;
  holdings: HoldingDto[];
}

export interface TransactionDto {
  id: number;
  holdingId: number;
  transactionType: 'Buy' | 'Sell';
  quantity: number;
  purchasePrice: number;
  purchaseDate: string;
  createdAt: string;
  currentPrice: number;
  variationEur: number;
  variationPercent: number;
  // Sell-specific (null for buys)
  taxableProfitEur: number | null;
  exitTaxPercent: number | null;
  exitTaxDueEur: number | null;
  allocations: SellAllocationDto[] | null;
}

export interface SellAllocationDto {
  buyTransactionId: number;
  buyDate: string;
  buyPrice: number;
  allocatedQuantity: number;
  profitEur: number;
}

export interface CreateTransactionDto {
  ticker: string;
  transactionType: 'Buy' | 'Sell';
  quantity: number;
  purchasePrice: number;
  purchaseDate: string;
}

export interface UpdateTransactionDto {
  quantity: number;
  purchasePrice: number;
  purchaseDate: string; // "YYYY-MM-DD"
}

export interface AssetTaxRateDto {
  securityType: string;
  exitTaxPercent: number;
  label: string | null;
}

export interface ProjectionSettingsDto {
  yearlyReturnPercent: number;
  monthlyBuyAmount: number;
  annualBuyIncreasePercent: number;
  projectionYears: number;
  inflationPercent: number;
  cgtPercent: number;
  exitTaxPercent: number;
  excludePreExistingFromTax: boolean;
  /** SIA annual tax percentage — charged yearly on total portfolio value (alternative to CGT/Exit Tax). */
  siaAnnualPercent: number;
  /** Optional override for the starting portfolio value (0 / undefined = use live portfolio value). */
  startAmount?: number | null;
}

export interface ProjectionDataPointDto {
  year: number;
  initialBalance: number;
  totalBuys: number;
  yearProfit: number;
  totalAmount: number;
  inflationCorrectedAmount: number;
  taxPaid: number;
  exitTaxPaid: number;
  afterTaxTotalAmount: number;
  afterTaxInflationCorrectedAmount: number;
  /** SIA tax charged this year. May be 0 or missing for older saved versions. */
  siaTax?: number;
  /** After-tax balance using SIA model. May be missing for older saved versions. */
  afterTaxSia?: number;
  /** After-tax balance (SIA) corrected for inflation. May be missing for older saved versions. */
  afterTaxInflationCorrectedSia?: number;
}

export interface ProjectionResultDto {
  settings: ProjectionSettingsDto;
  dataPoints: ProjectionDataPointDto[];
}

export interface ProjectionVersionSummaryDto {
  id: number;
  versionName: string;
  isDefault: boolean;
  savedAt: string;           // ISO datetime string
  settings: ProjectionSettingsDto;
  /** Saved (frozen) yearly data points — never recalculated after save. */
  dataPoints: ProjectionDataPointDto[];
}

export interface ProjectionVersionDetailDto {
  id: number;
  versionName: string;
  isDefault: boolean;
  savedAt: string;
  settings: ProjectionSettingsDto;
  dataPoints: ProjectionDataPointDto[];
}

export interface SaveVersionRequestDto {
  versionName: string;
  settings: ProjectionSettingsDto;
}

export interface PortfolioEvolutionDataPointDto {
  date: string;       // "yyyy-MM-dd"
  totalValue: number;
  hasBuy: boolean;
  hasSell: boolean;
}

export interface PortfolioEvolutionDto {
  dataPoints: PortfolioEvolutionDataPointDto[];
}

// ── Ticker Search ─────────────────────────────────────────────────────────────

export interface TickerSearchResult {
  symbol: string;
  shortName: string | null;
  longName: string | null;
  exchange: string | null;
  quoteType: string | null;
  typeDisp: string | null;
}

// ── Sharing ───────────────────────────────────────────────────────────────────

export interface CreateShareDto {
  guestEmail: string;
  isReadOnly: boolean;
}

export interface UpdateShareDto {
  isReadOnly?: boolean;
  status?: string;
}

export interface ShareSummaryDto {
  id: number;
  guestEmail: string;
  guestName: string | null;
  isReadOnly: boolean;
  status: string;   // "Pending" | "Active" | "Revoked"
  isLinked: boolean;
  createdAt: string;
}

export interface SharedWithMeDto {
  id: number;
  ownerUserId: number;
  ownerEmail: string;
  ownerName: string | null;
  ownerAvatarUrl: string | null;
  isReadOnly: boolean;
}

// ── FIFO Preview ──────────────────────────────────────────────────────────────

export interface FifoPreviewAllocationDto {
  buyTransactionId: number;
  buyDate: string;          // "YYYY-MM-DD"
  buyPrice: number;
  allocatedQuantity: number;
  profitEur: number;
}

export interface FifoPreviewDto {
  isFeasible: boolean;
  requestedQuantity: number;
  availableQuantity: number;
  allocations: FifoPreviewAllocationDto[];
  weightedAvgBuyPrice: number;
  totalProfit: number;
  exitTaxRate: number | null;
  exitTaxDue: number | null;
}

// ── Tax Year Summary ──────────────────────────────────────────────────────────

export interface TaxSellEntryDto {
  transactionId: number;
  ticker: string;
  etfName: string | null;
  sellDate: string;           // "yyyy-MM-dd"
  quantitySold: number;
  sellPrice: number;
  weightedBuyPrice: number;
  taxableProfit: number;
  exitTaxPercent: number | null;
  exitTaxDue: number | null;
  securityType: string | null;
}

export interface TaxYearSummaryDto {
  year: number;
  entries: TaxSellEntryDto[];
  totalTaxableProfit: number;
  totalExitTaxDue: number;
  hasMissingRates: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = `${environment.apiBase}/api`;

  constructor(private http: HttpClient) {
    console.log('ApiService instantiated');
  }

  getDashboard(): Observable<DashboardDto> {
    console.log('getDashboard called');
    return this.http.get<DashboardDto>(`${this.apiUrl}/holdings/dashboard`);
  }

  getHoldings(): Observable<HoldingDto[]> {
    return this.http.get<HoldingDto[]>(`${this.apiUrl}/holdings`);
  }

  getHoldingHistory(holdingId: number): Observable<TransactionDto[]> {
    return this.http.get<TransactionDto[]>(`${this.apiUrl}/holdings/${holdingId}/history`);
  }

  addTransaction(transaction: CreateTransactionDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/holdings/transaction`, transaction);
  }

  deleteTransaction(transactionId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/holdings/transactions/${transactionId}`);
  }

  updateTransaction(transactionId: number, dto: UpdateTransactionDto): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/holdings/transactions/${transactionId}`, dto);
  }

  getEtfDescription(ticker: string): Observable<{ description: string }> {
    return this.http.get<{ description: string }>(`${this.apiUrl}/holdings/etf-description/${ticker}`);
  }

  searchTickers(query: string): Observable<TickerSearchResult[]> {
    const encoded = encodeURIComponent(query);
    return this.http.get<TickerSearchResult[]>(`${this.apiUrl}/holdings/search?q=${encoded}`);
  }

  getProjection(): Observable<ProjectionResultDto> {
    return this.http.get<ProjectionResultDto>(`${this.apiUrl}/projections`);
  }

  calculateProjection(settings: ProjectionSettingsDto): Observable<ProjectionResultDto> {
    return this.http.post<ProjectionResultDto>(`${this.apiUrl}/projections/calculate`, settings);
  }

  saveProjectionSettings(settings: ProjectionSettingsDto): Observable<ProjectionSettingsDto> {
    return this.http.put<ProjectionSettingsDto>(`${this.apiUrl}/projections/settings`, settings);
  }

  saveProjectionVersion(request: SaveVersionRequestDto): Observable<ProjectionVersionSummaryDto> {
    return this.http.post<ProjectionVersionSummaryDto>(`${this.apiUrl}/projections/versions`, request);
  }

  getProjectionVersions(): Observable<ProjectionVersionSummaryDto[]> {
    return this.http.get<ProjectionVersionSummaryDto[]>(`${this.apiUrl}/projections/versions`);
  }

  getProjectionVersionDetail(id: number): Observable<ProjectionVersionDetailDto> {
    return this.http.get<ProjectionVersionDetailDto>(`${this.apiUrl}/projections/versions/${id}`);
  }

  deleteProjectionVersion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/projections/versions/${id}`);
  }

  setDefaultProjectionVersion(id: number): Observable<ProjectionVersionSummaryDto> {
    return this.http.patch<ProjectionVersionSummaryDto>(`${this.apiUrl}/projections/versions/${id}/default`, {});
  }

  getPortfolioEvolution(): Observable<PortfolioEvolutionDto> {
    return this.http.get<PortfolioEvolutionDto>(`${this.apiUrl}/holdings/portfolio-evolution`);
  }

  // ── Asset Tax Rates ───────────────────────────────────────────────────────

  getTaxRates(): Observable<AssetTaxRateDto[]> {
    return this.http.get<AssetTaxRateDto[]>(`${this.apiUrl}/holdings/tax-rates`);
  }

  upsertTaxRate(dto: AssetTaxRateDto): Observable<AssetTaxRateDto> {
    return this.http.put<AssetTaxRateDto>(`${this.apiUrl}/holdings/tax-rates`, dto);
  }

  deleteTaxRate(securityType: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/holdings/tax-rates/${encodeURIComponent(securityType)}`);
  }

  // ── Sharing ──────────────────────────────────────────────────────────────────

  createShare(dto: CreateShareDto): Observable<ShareSummaryDto> {
    return this.http.post<ShareSummaryDto>(`${this.apiUrl}/sharing`, dto);
  }

  getMyShares(): Observable<ShareSummaryDto[]> {
    return this.http.get<ShareSummaryDto[]>(`${this.apiUrl}/sharing/my-shares`);
  }

  updateShare(id: number, dto: UpdateShareDto): Observable<ShareSummaryDto> {
    return this.http.put<ShareSummaryDto>(`${this.apiUrl}/sharing/${id}`, dto);
  }

  deleteShare(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/sharing/${id}`);
  }

  getSharedWithMe(): Observable<SharedWithMeDto[]> {
    return this.http.get<SharedWithMeDto[]>(`${this.apiUrl}/sharing/shared-with-me`);
  }

  // ── Tax Year Summary ──────────────────────────────────────────────────────

  getTaxYears(): Observable<number[]> {
    return this.http.get<number[]>(`${this.apiUrl}/holdings/tax-years`);
  }

  getTaxSummary(year: number): Observable<TaxYearSummaryDto> {
    return this.http.get<TaxYearSummaryDto>(`${this.apiUrl}/holdings/tax-summary?year=${year}`);
  }

  // ── FIFO Preview ──────────────────────────────────────────────────────────

  getFifoPreview(holdingId: number, quantity: number, sellPrice: number): Observable<FifoPreviewDto> {
    return this.http.get<FifoPreviewDto>(
      `${this.apiUrl}/holdings/${holdingId}/fifo-preview?quantity=${quantity}&sellPrice=${sellPrice}`
    );
  }
}
