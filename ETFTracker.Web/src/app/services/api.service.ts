import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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

export interface PortfolioEvolutionDataPointDto {
  date: string;       // "yyyy-MM-dd"
  totalValue: number;
  hasBuy: boolean;
}

export interface PortfolioEvolutionDto {
  dataPoints: PortfolioEvolutionDataPointDto[];
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = 'http://localhost:5098/api';

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

  getPortfolioEvolution(): Observable<PortfolioEvolutionDto> {
    return this.http.get<PortfolioEvolutionDto>(`${this.apiUrl}/holdings/portfolio-evolution`);
  }
}
