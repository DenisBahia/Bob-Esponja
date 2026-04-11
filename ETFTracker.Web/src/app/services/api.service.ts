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
}

export interface PortfolioEvolutionDto {
  dataPoints: PortfolioEvolutionDataPointDto[];
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

  getEtfDescription(ticker: string): Observable<{ description: string }> {
    return this.http.get<{ description: string }>(`${this.apiUrl}/holdings/etf-description/${ticker}`);
  }

  getProjection(): Observable<ProjectionResultDto> {
    return this.http.get<ProjectionResultDto>(`${this.apiUrl}/projections`);
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
}
