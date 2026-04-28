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
  dailyMetrics: PeriodMetrics;
  weeklyMetrics: PeriodMetrics;
  monthlyMetrics: PeriodMetrics;
  ytdMetrics: PeriodMetrics;
  totalTaxPaid: number;
  totalTaxPending: number;
  totalExitTaxPending: number;
  totalCgtPending: number;
  availableQuantity: number;
  nextDeemedDisposalDate: string | null;
  assetType: string | null;
}

export interface DashboardHeaderDto {
  totalHoldingsAmount: number;
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
  quantity: number;
  purchasePrice: number;
  purchaseDate: string;
  createdAt: string;
  currentPrice: number;
  variationEur: number;
  variationPercent: number;
}

export interface CreateTransactionDto {
  ticker: string;
  quantity: number;
  purchasePrice: number;
  purchaseDate: string;
  isIrishInvestor: boolean;
  taxRate: number;
  deemedDisposalDue: boolean;
  assetType?: string | null;
}

export interface UpdateTransactionDto {
  quantity: number;
  purchasePrice: number;
  purchaseDate: string; // "YYYY-MM-DD"
}

export interface ImportTransactionRowDto {
  operation: string;  // "BUY" | "SELL"
  ticker: string;
  quantity: number;
  price: number;
  date: string;       // "YYYY-MM-DD"
}

export interface ImportTransactionsResultDto {
  imported: number;
}

export interface ProjectionSettingsDto {
  yearlyReturnPercent: number;
  monthlyBuyAmount: number;
  annualBuyIncreasePercent: number;
  projectionYears: number;
  inflationPercent: number;
  /** Exit Tax % when DD on, CGT % when DD off. */
  cgtPercent: number;
  /** Optional override for the starting portfolio value (0 / undefined = use live portfolio value). */
  startAmount?: number | null;
  /** Irish investors only: simulate 8-year deemed disposal events. */
  applyDeemedDisposal: boolean;
  /** Read-only — always resolved from user tax settings by the backend. */
  deemedDisposalPercent: number;
}

// ── User Tax Defaults ─────────────────────────────────────────────────────────

export interface UserTaxDefaultsDto {
  isConfigured: boolean;
  isIrishInvestor: boolean;
  // Irish-only
  exitTaxPercent: number;
  deemedDisposalPercent: number;
  siaAnnualPercent: number;
  // Non-Irish only
  cgtPercent: number;
  taxFreeAllowancePerYear: number;
}

export interface ProjectionDataPointDto {
  year: number;
  initialBalance: number;
  totalBuys: number;
  yearProfit: number;
  totalAmount: number;
  inflationCorrectedAmount: number;
  /** Deemed Disposal tax paid this year (0 when DD is off or no DD event this year). */
  deemedDisposalPaid: number;
  taxPaid: number;
  afterTaxTotalAmount: number;
  afterTaxInflationCorrectedAmount: number;
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

// ── Goal ──────────────────────────────────────────────────────────────────────

export interface GoalDataPointDto {
  year: number;
  targetValue: number;
}

export interface UserGoalDto {
  id: number;
  sourceVersionId: number | null;
  savedAt: string;
  dataPoints: GoalDataPointDto[];
}

export interface UpsertGoalRequestDto {
  sourceVersionId?: number | null;
  dataPoints: GoalDataPointDto[];
}

export interface PortfolioEvolutionDataPointDto {
  date: string;       // "yyyy-MM-dd"
  totalValue: number;
  hasBuy: boolean;
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

// ── Sell / CGT ────────────────────────────────────────────────────────────────

export interface SellRequestDto {
  quantity: number;
  sellPrice: number;
  sellDate: string;       // "YYYY-MM-DD"
  isIrishInvestor: boolean;
  taxRate: number;
}
export interface SellLotBreakdownDto {
  buyTransactionId: number;
  buyDate: string;
  quantityConsumed: number;
  originalCostPerUnit: number;
  adjustedCostPerUnit: number;
  deemedDisposalDate: string | null;
  deemedDisposalPricePerUnit: number | null;
  profitOnLot: number;
  deemedDisposalDue: boolean;
}

export interface SellPreviewDto {
  availableQuantity: number;
  totalProfit: number;
  cgtDue: number;
  taxRateUsed: number;
  taxType: string;  // "CGT" | "ExitTax"
  hasLosses: boolean;
  lots: SellLotBreakdownDto[];
}

export interface SellRecordDto {
  id: number;
  holdingId: number;
  sellDate: string;
  sellPrice: number;
  quantity: number;
  totalProfit: number;
  taxAmountSaved: number;
  taxRateUsed: number;
  taxType: string;  // "CGT" | "ExitTax"
  createdAt: string;
  lots: SellLotBreakdownDto[];
}

export interface UpdateSellRecordDto {
  quantity: number;
  sellPrice: number;
  sellDate: string;  // "YYYY-MM-DD"
}

// ── Tax Events ────────────────────────────────────────────────────────────────

export interface TaxEventDto {
  id: number;
  holdingId: number;
  ticker: string;
  etfName: string | null;
  buyTransactionId: number | null;
  sellRecordId: number | null;
  eventType: 'Sell' | 'DeemedDisposal';
  taxSubType: string | null;  // "CGT" | "ExitTax" | "DeemedDisposal"
  eventDate: string;         // "YYYY-MM-DD"
  quantityAtEvent: number;
  costBasisPerUnit: number;
  pricePerUnitAtEvent: number;
  taxableGain: number;
  taxAmount: number;
  taxRateUsed: number;
  status: 'Pending' | 'Paid';
  paidAt: string | null;
  createdAt: string;
}

export interface TaxYearSummaryDto {
  year: number;
  totalProfits: number;
  totalLosses: number;
  netGain: number;
  taxFreeAllowance: number;
  taxableGain: number;
  taxDue: number;
  status: string;
}

export interface ExitTaxPotDto {
  holdingId: number;
  ticker: string;
  year: number;
  totalProfits: number;
  totalLosses: number;
  deemedDisposalCreditUsed: number;
  netTaxableGain: number;
  taxDue: number;
  status: string;
}

export interface RecalculateTaxYearResultDto {
  year: number;
  cgtTaxDue: number;
  exitTaxPots: ExitTaxPotDto[];
  totalTaxDue: number;
}

export interface AssetTypeDeemedDisposalDefaultDto {
  assetType: string;
  deemedDisposalDue: boolean;
}

export interface TaxSummaryDto {
  totalPending: number;
  totalPaid: number;
  isIrishInvestor: boolean;
  nextDeemedDisposalDate: string | null;
  events: TaxEventDto[];
  cgtByYear: TaxYearSummaryDto[];
  exitTaxPots: ExitTaxPotDto[];
  // Legacy
  annualTaxFreeAllowance: number;
  allowanceByYear: TaxYearAllowanceSummaryDto[];
  totalPendingAfterAllowance: number;
}

export interface TaxYearAllowanceSummaryDto {
  year: number;
  totalTaxableGain: number;
  taxBeforeAllowance: number;
  allowanceApplied: number;
  taxAfterAllowance: number;
}

export interface MarkTaxEventPaidDto {
  paidAt?: string | null;
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

  importTransactions(rows: ImportTransactionRowDto[]): Observable<ImportTransactionsResultDto> {
    return this.http.post<ImportTransactionsResultDto>(`${this.apiUrl}/holdings/import`, { rows });
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

  // ── Goal ──────────────────────────────────────────────────────────────────

  getGoal(): Observable<UserGoalDto> {
    return this.http.get<UserGoalDto>(`${this.apiUrl}/goal`);
  }

  upsertGoal(dto: UpsertGoalRequestDto): Observable<UserGoalDto> {
    return this.http.put<UserGoalDto>(`${this.apiUrl}/goal`, dto);
  }

  // ── Sell / CGT ─────────────────────────────────────────────────────────────

  previewSell(holdingId: number, dto: SellRequestDto): Observable<SellPreviewDto> {
    return this.http.post<SellPreviewDto>(`${this.apiUrl}/holdings/${holdingId}/sell/preview`, dto);
  }

  confirmSell(holdingId: number, dto: SellRequestDto): Observable<SellRecordDto> {
    return this.http.post<SellRecordDto>(`${this.apiUrl}/holdings/${holdingId}/sell/confirm`, dto);
  }

  getSellHistory(holdingId: number): Observable<SellRecordDto[]> {
    return this.http.get<SellRecordDto[]>(`${this.apiUrl}/holdings/${holdingId}/sell-history`);
  }

  deleteSellRecord(sellRecordId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/holdings/sell-records/${sellRecordId}`);
  }

  updateSellRecord(sellRecordId: number, dto: UpdateSellRecordDto): Observable<SellRecordDto> {
    return this.http.patch<SellRecordDto>(`${this.apiUrl}/holdings/sell-records/${sellRecordId}`, dto);
  }

  // ── Tax Events ────────────────────────────────────────────────────────────

  getTaxEvents(holdingId?: number): Observable<TaxSummaryDto> {
    const url = holdingId
      ? `${this.apiUrl}/tax-events?holdingId=${holdingId}`
      : `${this.apiUrl}/tax-events`;
    return this.http.get<TaxSummaryDto>(url);
  }

  markTaxEventPaid(id: number, dto?: MarkTaxEventPaidDto): Observable<TaxEventDto> {
    return this.http.put<TaxEventDto>(`${this.apiUrl}/tax-events/${id}/mark-paid`, dto ?? {});
  }

  markAllTaxEventsPaid(year?: number): Observable<{ marked: number }> {
    const url = year
      ? `${this.apiUrl}/tax-events/mark-all-paid?year=${year}`
      : `${this.apiUrl}/tax-events/mark-all-paid`;
    return this.http.put<{ marked: number }>(url, {});
  }

  markYearPaid(year: number): Observable<{ year: number; status: string }> {
    return this.http.put<{ year: number; status: string }>(`${this.apiUrl}/tax-events/mark-year-paid/${year}`, {});
  }

  markExitTaxYearPaid(year: number): Observable<{ year: number; status: string }> {
    return this.http.put<{ year: number; status: string }>(`${this.apiUrl}/tax-events/mark-exit-tax-year-paid/${year}`, {});
  }

  recalculateTaxYear(year: number): Observable<RecalculateTaxYearResultDto> {
    return this.http.post<RecalculateTaxYearResultDto>(`${this.apiUrl}/tax-events/recalculate-year?year=${year}`, {});
  }

  // ── Asset-Type Defaults ─────────────────────────────────────────────────────

  getAssetTypeDefaults(): Observable<AssetTypeDeemedDisposalDefaultDto[]> {
    return this.http.get<AssetTypeDeemedDisposalDefaultDto[]>(`${this.apiUrl}/asset-type-defaults`);
  }

  upsertAssetTypeDefault(dto: AssetTypeDeemedDisposalDefaultDto): Observable<AssetTypeDeemedDisposalDefaultDto> {
    return this.http.post<AssetTypeDeemedDisposalDefaultDto>(`${this.apiUrl}/asset-type-defaults`, dto);
  }

  // ── User Settings ──────────────────────────────────────────────────────────

  getTaxDefaults(): Observable<UserTaxDefaultsDto> {
    return this.http.get<UserTaxDefaultsDto>(`${this.apiUrl}/user-settings/tax-defaults`);
  }

  saveTaxDefaults(dto: UserTaxDefaultsDto): Observable<UserTaxDefaultsDto> {
    return this.http.put<UserTaxDefaultsDto>(`${this.apiUrl}/user-settings/tax-defaults`, dto);
  }
}
