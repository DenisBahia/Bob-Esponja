import { Component, OnInit, OnDestroy, AfterViewChecked, ChangeDetectionStrategy, ChangeDetectorRef, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, DashboardDto, HoldingDto, ProjectionResultDto, ProjectionSettingsDto, PortfolioEvolutionDto, ProjectionVersionSummaryDto, ProjectionVersionDetailDto, ProjectionDataPointDto, SaveVersionRequestDto, UserGoalDto, GoalDataPointDto, UpsertGoalRequestDto, TaxSummaryDto, TaxEventDto, TaxYearAllowanceSummaryDto, UserTaxDefaultsDto } from '../../services/api.service';
import { AuthService, CurrentUser } from '../../services/auth.service';
import { AddTransactionModalComponent } from '../../components/add-transaction-modal/add-transaction-modal.component';
import { BuyHistoryModalComponent } from '../../components/buy-history-modal/buy-history-modal.component';
import { SellModalComponent } from '../../components/sell-modal/sell-modal.component';
import { ShareProfileModalComponent } from '../../components/share-profile-modal/share-profile-modal.component';
import { TaxHistoryModalComponent } from '../../components/tax-history-modal/tax-history-modal.component';
import { UserSettingsModalComponent } from '../../components/user-settings-modal/user-settings-modal.component';
import { SharingContextService } from '../../services/sharing-context.service';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Chart, ArcElement, Tooltip, Legend, PieController,
  LineController, LineElement, PointElement, LinearScale, CategoryScale, Filler,
  type ChartData
} from 'chart.js';

Chart.register(ArcElement, Tooltip, Legend, PieController, LineController, LineElement, PointElement, LinearScale, CategoryScale, Filler);

const PIE_COLORS = [
  '#10B981', '#059669', '#06d6d0', '#10B981', '#f89b29',
  '#f05252', '#fa709a', '#a78bfa', '#34d399', '#fbbf24',
  '#60a5fa', '#c084fc', '#2dd4bf', '#86efac', '#fcd34d',
];

const EXCLUDE_PRE_EXISTING_TAX_TOGGLE_ALLOWLIST = new Set([
  'demo@sample.com',
  'denis.bahia@uol.com.br',
  'denis.bahia.1984@gmail.com',
]);

// Shared dark-theme Chart.js defaults
const DARK_SCALE_DEFAULTS = {
  grid:  { color: 'rgba(31, 46, 74, 0.8)' },
  ticks: { color: '#8da0bf' },
  title: { color: '#8da0bf' },
  border: { color: 'rgba(31,46,74,0.5)' },
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, AddTransactionModalComponent, BuyHistoryModalComponent, SellModalComponent, ShareProfileModalComponent, TaxHistoryModalComponent, UserSettingsModalComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit, OnDestroy, AfterViewChecked {
  activeMainSection: 'portfolio' | 'goal' | 'projections' = 'portfolio';
  dashboard: DashboardDto | null = null;
  loading = true;
  error: string | null = null;

  showAddTransactionModal = false;
  showBuyHistoryModal = false;
  selectedHoldingId: number | null = null;
  showShareModal = false;
  showUserSettingsModal = false;

  // User tax defaults (loaded on init, drives pre-fill across the app)
  userTaxDefaults: UserTaxDefaultsDto | null = null;

  // Sell modal
  showSellModal = false;
  selectedSellHolding: HoldingDto | null = null;

  // Tax history modal
  showTaxHistoryModal = false;
  selectedTaxHolding: HoldingDto | null = null;

  // Tax summary (consolidated section)
  taxSummary: TaxSummaryDto | null = null;
  taxSummaryLoading = false;
  taxMarkingAllYear: number | null = null;

  @ViewChild('allocationChart') allocationChartRef!: ElementRef<HTMLCanvasElement>;
  private pieChart: Chart | null = null;
  private chartRendered = false;

  // Projection
  projection: ProjectionResultDto | null = null;
  projectionSettings: ProjectionSettingsDto = {
    yearlyReturnPercent: 7,
    monthlyBuyAmount: 500,
    annualBuyIncreasePercent: 3,
    projectionYears: 10,
    inflationPercent: 2,
    cgtPercent: 33,
    exitTaxPercent: 41,
    excludePreExistingFromTax: false,
    siaAnnualPercent: 0,
    startAmount: null,
    isIrishInvestor: true,
    taxFreeAllowancePerYear: 0,
    deemedDisposalPercent: 41,
  };
  projectionLoading = false;
  projectionSaving = false;
  showSiaNumbers = false;
  activeProjectionTab: 'chart' | 'table' | 'versions' = 'chart';
  currentYear = new Date().getFullYear();

  @ViewChild('projectionChart') projectionChartRef!: ElementRef<HTMLCanvasElement>;
  private lineChart: Chart | null = null;
  private projectionChartRendered = false;

  // Projection versions
  projectionVersions: ProjectionVersionSummaryDto[] = [];
  versionsLoading = false;
  versionSaving = false;
  newVersionName = '';
  versionDeleting: Set<number> = new Set();
  versionSettingDefault: Set<number> = new Set();
  confirmDeleteId: number | null = null;
  selectedVersionIds: Set<number> = new Set();
  versionCompareData: Map<number, ProjectionDataPointDto[]> = new Map();
  versionsCompareChartRendered = false;
  /** Name of the default version currently driving the params/graph/table (null = saved settings). */
  defaultVersionName: string | null = null;

  // Versions table sort
  versionsSortCol = 'savedAt';
  versionsSortDir: 'asc' | 'desc' = 'desc';

  get sortedVersions(): ProjectionVersionSummaryDto[] {
    const list = [...this.projectionVersions];
    const dir = this.versionsSortDir === 'asc' ? 1 : -1;
    return list.sort((a, b) => {
      let av: any, bv: any;
      switch (this.versionsSortCol) {
        case 'name':        av = a.versionName?.toLowerCase();              bv = b.versionName?.toLowerCase(); break;
        case 'savedAt':     av = a.savedAt;                                 bv = b.savedAt; break;
        case 'startAmount': av = a.settings.startAmount ?? 0;               bv = b.settings.startAmount ?? 0; break;
        case 'return':      av = a.settings.yearlyReturnPercent;             bv = b.settings.yearlyReturnPercent; break;
        case 'monthly':     av = a.settings.monthlyBuyAmount;               bv = b.settings.monthlyBuyAmount; break;
        case 'buyInc':      av = a.settings.annualBuyIncreasePercent;        bv = b.settings.annualBuyIncreasePercent; break;
        case 'years':       av = a.settings.projectionYears;                bv = b.settings.projectionYears; break;
        case 'inflation':   av = a.settings.inflationPercent;               bv = b.settings.inflationPercent; break;
        case 'cgt':         av = a.settings.cgtPercent;                     bv = b.settings.cgtPercent; break;
        case 'exitTax':     av = a.settings.exitTaxPercent;                 bv = b.settings.exitTaxPercent; break;
        case 'sia':         av = a.settings.siaAnnualPercent;               bv = b.settings.siaAnnualPercent; break;
        default: return 0;
      }
      if (av < bv) return -dir;
      if (av > bv) return dir;
      return 0;
    });
  }

  sortVersionsBy(col: string): void {
    if (this.versionsSortCol === col) {
      this.versionsSortDir = this.versionsSortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.versionsSortCol = col;
      this.versionsSortDir = 'asc';
    }
    this.cdr.markForCheck();
  }

  @ViewChild('versionsCompareChart') versionsCompareChartRef!: ElementRef<HTMLCanvasElement>;
  private versionsCompareChart: Chart | null = null;

  // ── My Goal ─────────────────────────────────────────────────────────────────
  userGoal: UserGoalDto | null = null;
  goalDataPoints: GoalDataPointDto[] = [];
  goalLoading = false;
  goalSaving = false;
  goalSavingAsId: number | null = null;   // version id currently being saved as goal
  goalSubTab: 'yearly' | 'monthly' = 'yearly';
  goalVersionSettings: ProjectionSettingsDto | null = null;

  @ViewChild('goalChart') goalChartRef!: ElementRef<HTMLCanvasElement>;
  private goalChart: Chart | null = null;
  private goalChartRendered = false;

  @ViewChild('goalMonthlyChart') goalMonthlyChartRef!: ElementRef<HTMLCanvasElement>;
  private goalMonthlyChart: Chart | null = null;
  private goalMonthlyChartRendered = false;

  portfolioEvolution: PortfolioEvolutionDto | null = null;
  evolutionLoading = false;
  evolutionPeriod: 'all' | 'year' | 'month' | 'week' = 'all';

  // Holdings table sort
  holdingsSortCol = '';
  holdingsSortDir: 'asc' | 'desc' = 'asc';

  get sortedHoldings(): HoldingDto[] {
    if (!this.dashboard?.holdings) return [];
    if (!this.holdingsSortCol) return this.dashboard.holdings;
    const list = [...this.dashboard.holdings];
    const dir = this.holdingsSortDir === 'asc' ? 1 : -1;
    return list.sort((a, b) => {
      let av: any, bv: any;
      switch (this.holdingsSortCol) {
        case 'ticker':       av = a.ticker?.toLowerCase();                   bv = b.ticker?.toLowerCase(); break;
        case 'etfName':      av = a.etfName?.toLowerCase();                  bv = b.etfName?.toLowerCase(); break;
        case 'quantity':     av = a.quantity;                                bv = b.quantity; break;
        case 'avgCost':      av = a.averageCost;                             bv = b.averageCost; break;
        case 'currentPrice': av = a.currentPrice;                            bv = b.currentPrice; break;
        case 'totalValue':   av = a.totalValue;                              bv = b.totalValue; break;
        case 'totalGainLoss': av = this.getHoldingTotalGainLoss(a);          bv = this.getHoldingTotalGainLoss(b); break;
        case 'daily':        av = a.dailyMetrics.gainLossEur;                bv = b.dailyMetrics.gainLossEur; break;
        case 'weekly':       av = a.weeklyMetrics.gainLossEur;               bv = b.weeklyMetrics.gainLossEur; break;
        case 'monthly':      av = a.monthlyMetrics.gainLossEur;              bv = b.monthlyMetrics.gainLossEur; break;
        case 'ytd':          av = a.ytdMetrics.gainLossEur;                  bv = b.ytdMetrics.gainLossEur; break;
        case 'taxPaid':      av = a.totalTaxPaid;                            bv = b.totalTaxPaid; break;
        default: return 0;
      }
      if (av < bv) return -dir;
      if (av > bv) return dir;
      return 0;
    });
  }

  sortHoldingsBy(col: string): void {
    if (this.holdingsSortCol === col) {
      this.holdingsSortDir = this.holdingsSortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.holdingsSortCol = col;
      this.holdingsSortDir = 'asc';
    }
    this.cdr.markForCheck();
  }

  @ViewChild('evolutionChart') evolutionChartRef!: ElementRef<HTMLCanvasElement>;
  private evolutionChart: Chart | null = null;
  private evolutionChartRendered = false;

  get filteredEvolutionPoints() {
    const points = this.portfolioEvolution?.dataPoints;
    if (!points?.length) return [];
    if (this.evolutionPeriod === 'all') return points;

    const now = new Date();
    let cutoff: Date;

    if (this.evolutionPeriod === 'year') {
      cutoff = new Date(now.getFullYear(), 0, 1);
    } else if (this.evolutionPeriod === 'month') {
      cutoff = new Date(now.getFullYear(), now.getMonth(), 1);
    } else {
      // week: last 7 days (Mon–Sun of current week)
      const day = now.getDay(); // 0=Sun
      const diff = (day === 0 ? -6 : 1 - day);
      cutoff = new Date(now.getFullYear(), now.getMonth(), now.getDate() + diff);
    }

    const cutoffStr = cutoff.toISOString().slice(0, 10);
    return points.filter(p => p.date >= cutoffStr);
  }

  get allocationSlices(): { ticker: string; etfName: string; value: number; percent: number; color: string }[] {
    if (!this.dashboard?.holdings?.length) return [];
    const total = this.dashboard.holdings.reduce((sum, h) => sum + h.totalValue, 0);
    return this.dashboard.holdings.map((h, i) => ({
      ticker: h.ticker,
      etfName: h.etfName,
      value: h.totalValue,
      percent: total > 0 ? (h.totalValue / total) * 100 : 0,
      color: PIE_COLORS[i % PIE_COLORS.length],
    }));
  }

  // ── Irish Investor toggle ────────────────────────────────────────────────────
  isIrishInvestor = true;

  /** Returns settings with Irish-specific taxes zeroed out when not an Irish investor. */
  get effectiveProjectionSettings(): ProjectionSettingsDto {
    if (this.isIrishInvestor) return this.projectionSettings;
    return { ...this.projectionSettings, cgtPercent: 0, siaAnnualPercent: 0 };
  }

  get canManageExcludePreExistingTax(): boolean {
    const email = this.auth.user()?.email?.trim().toLowerCase();
    return !!email && EXCLUDE_PRE_EXISTING_TAX_TOGGLE_ALLOWLIST.has(email);
  }

  toggleIrishInvestor(): void {
    this.isIrishInvestor = !this.isIrishInvestor;
    localStorage.setItem('isIrishInvestor', String(this.isIrishInvestor));
    this.projectionSettings.isIrishInvestor = this.isIrishInvestor;
    // Recalculate projection with updated effective settings
    this.projectionChartRendered = false;
    this.lineChart?.destroy();
    this.lineChart = null;
    this.projectionLoading = true;
    this.cdr.markForCheck();
    this.apiService.calculateProjection(this.effectiveProjectionSettings).subscribe({
      next: (result) => {
        this.projection = result;
        this.projectionLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.projectionLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  toggleExcludePreExistingFromTax(): void {
    this.projectionSettings.excludePreExistingFromTax = !this.projectionSettings.excludePreExistingFromTax;
    this.cdr.markForCheck();
  }

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef, public auth: AuthService, public sharingCtx: SharingContextService) {
    const saved = localStorage.getItem('isIrishInvestor');
    this.isIrishInvestor = saved === null ? true : saved === 'true';
  }

  ngOnInit(): void {
    console.log('Dashboard component initialized');
    this.loadDashboard();
    this.initProjection();
    this.loadPortfolioEvolution();
    this.loadTaxSummary();
    this.loadTaxDefaults();
  }

  onShareModalClosed(): void {
    this.showShareModal = false;
    this.cdr.markForCheck();
  }

  // ── User Settings Modal ──────────────────────────────────────────────────────

  openUserSettings(): void {
    this.showUserSettingsModal = true;
    this.cdr.markForCheck();
  }

  onUserSettingsClosed(saved: UserTaxDefaultsDto | null): void {
    this.showUserSettingsModal = false;
    if (saved) {
      this.userTaxDefaults = saved;
      // Sync isIrishInvestor toggle and relevant projection settings with new defaults
      this.isIrishInvestor = saved.isIrishInvestor;
      localStorage.setItem('isIrishInvestor', String(this.isIrishInvestor));
      this.projectionSettings = {
        ...this.projectionSettings,
        isIrishInvestor: saved.isIrishInvestor,
        exitTaxPercent: saved.exitTaxPercent,
        deemedDisposalPercent: saved.deemedDisposalPercent,
        siaAnnualPercent: saved.siaAnnualPercent,
        cgtPercent: saved.cgtPercent,
        taxFreeAllowancePerYear: saved.taxFreeAllowancePerYear,
      };
      // Recalculate projection and refresh tax summary with the updated defaults
      this.projectionChartRendered = false;
      this.lineChart?.destroy();
      this.lineChart = null;
      this.projectionLoading = true;
      this.apiService.calculateProjection(this.effectiveProjectionSettings).subscribe({
        next: (result) => { this.projection = result; this.projectionLoading = false; this.cdr.markForCheck(); },
        error: () => { this.projectionLoading = false; this.cdr.markForCheck(); }
      });
      this.loadTaxSummary();
    }
    this.cdr.markForCheck();
  }

  private loadTaxDefaults(): void {
    this.apiService.getTaxDefaults().subscribe({
      next: (defaults) => {
        this.userTaxDefaults = defaults;
        // Sync isIrishInvestor from server (authoritative)
        this.isIrishInvestor = defaults.isIrishInvestor;
        localStorage.setItem('isIrishInvestor', String(defaults.isIrishInvestor));
        this.cdr.markForCheck();
      },
      error: () => { /* non-fatal — fall back to localStorage value */ }
    });
  }

  onStartedViewingAs(): void {
    // Immediately clear stale data so the user sees loading state right away
    this.dashboard = null;
    this.projection = null;
    this.portfolioEvolution = null;
    this.defaultVersionName = null;
    this.userGoal = null;
    this.goalDataPoints = [];
    this.loading = true;
    this.projectionLoading = true;
    this.projectionVersions = [];
    this.chartRendered = false;
    this.projectionChartRendered = false;
    this.evolutionChartRendered = false;
    this.goalChartRendered = false;
    this.goalMonthlyChartRendered = false;
    this.goalVersionSettings = null;
    this.pieChart?.destroy(); this.pieChart = null;
    this.lineChart?.destroy(); this.lineChart = null;
    this.evolutionChart?.destroy(); this.evolutionChart = null;
    this.goalChart?.destroy(); this.goalChart = null;
    this.goalMonthlyChart?.destroy(); this.goalMonthlyChart = null;
    this.cdr.detectChanges();
    this.loadDashboard();
    this.initProjection();
    this.loadPortfolioEvolution();
  }

  stopViewingAs(): void {
    this.sharingCtx.stopViewing();
    // Immediately clear stale data so the user sees loading state right away
    this.dashboard = null;
    this.projection = null;
    this.portfolioEvolution = null;
    this.defaultVersionName = null;
    this.userGoal = null;
    this.goalDataPoints = [];
    this.goalVersionSettings = null;
    this.loading = true;
    this.projectionLoading = true;
    this.projectionVersions = [];
    this.chartRendered = false;
    this.projectionChartRendered = false;
    this.evolutionChartRendered = false;
    this.goalChartRendered = false;
    this.goalMonthlyChartRendered = false;
    this.pieChart?.destroy(); this.pieChart = null;
    this.lineChart?.destroy(); this.lineChart = null;
    this.evolutionChart?.destroy(); this.evolutionChart = null;
    this.goalChart?.destroy(); this.goalChart = null;
    this.goalMonthlyChart?.destroy(); this.goalMonthlyChart = null;
    this.cdr.detectChanges();
    this.loadDashboard();
    this.initProjection();
    this.loadPortfolioEvolution();
  }

  ngAfterViewChecked(): void {
    if (this.dashboard && !this.loading && !this.chartRendered && this.allocationChartRef?.nativeElement) {
      this.chartRendered = true;
      this.renderPieChart();
    }
    if (this.projection && !this.projectionLoading && !this.projectionChartRendered && this.projectionChartRef?.nativeElement) {
      this.projectionChartRendered = true;
      this.renderLineChart();
    }
    if (this.portfolioEvolution && !this.evolutionLoading && !this.evolutionChartRendered && this.evolutionChartRef?.nativeElement) {
      this.evolutionChartRendered = true;
      this.renderEvolutionChart();
    }
    if (
      this.activeProjectionTab === 'versions' &&
      !this.versionsCompareChartRendered &&
      this.selectedVersionIds.size > 0 &&
      this.versionsCompareChartRef?.nativeElement
    ) {
      this.versionsCompareChartRendered = true;
      this.renderVersionsCompareChart();
    }
    if (
      this.activeMainSection === 'goal' &&
      !this.goalChartRendered &&
      this.goalSubTab === 'yearly' &&
      this.goalDataPoints.length > 0 &&
      this.goalChartRef?.nativeElement
    ) {
      this.goalChartRendered = true;
      this.renderGoalChart();
    }
    if (
      this.activeMainSection === 'goal' &&
      this.goalSubTab === 'monthly' &&
      !this.goalMonthlyChartRendered &&
      this.goalVersionSettings !== null &&
      this.goalDataPoints.length > 0 &&
      this.goalMonthlyChartRef?.nativeElement
    ) {
      this.goalMonthlyChartRendered = true;
      this.renderMonthlyGoalChart();
    }
  }

  ngOnDestroy(): void {
    this.pieChart?.destroy();
    this.lineChart?.destroy();
    this.evolutionChart?.destroy();
    this.versionsCompareChart?.destroy();
    this.goalChart?.destroy();
    this.goalMonthlyChart?.destroy();
  }

  private renderPieChart(): void {
    this.pieChart?.destroy();
    const slices = this.allocationSlices;
    if (!slices.length) return;

    const ctx = this.allocationChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    this.pieChart = new Chart(ctx, {
      type: 'pie',
      data: {
        labels: slices.map(s => s.ticker),
        datasets: [{
          data: slices.map(s => s.value),
          backgroundColor: slices.map(s => s.color),
          borderWidth: 2,
          borderColor: '#111d35',
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#172040',
            borderColor: '#1f2e4a',
            borderWidth: 1,
            titleColor: '#e4eaf5',
            bodyColor: '#8da0bf',
            callbacks: {
              label: (ctx) => {
                const slice = slices[ctx.dataIndex];
                return ` ${slice.ticker}: ${this.formatCurrency(slice.value)} (${slice.percent.toFixed(1)}%)`;
              }
            }
          }
        }
      }
    });
  }

  private renderLineChart(): void {
    this.lineChart?.destroy();
    const points = this.projection?.dataPoints;
    if (!points?.length) return;

    const ctx = this.projectionChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const datasets: any[] = [
      {
        label: 'Projected Portfolio Value',
        data: points.map(p => p.totalAmount),
        borderColor: '#10B981',
        backgroundColor: 'rgba(16, 185, 129, 0.10)',
        fill: true,
        tension: 0.35,
        pointRadius: 5,
        pointHoverRadius: 7,
        pointBackgroundColor: '#10B981',
      },
      {
        label: 'Amount corrected by inflation',
        data: points.map(p => p.inflationCorrectedAmount),
        borderColor: '#f05252',
        backgroundColor: 'rgba(240, 82, 82, 0.07)',
        fill: true,
        tension: 0.35,
        pointRadius: 5,
        pointHoverRadius: 7,
        pointBackgroundColor: '#f05252',
        borderDash: [6, 3],
      },
      {
        label: 'Projected after taxes',
        data: points.map(p => p.afterTaxTotalAmount),
        borderColor: '#4d6080',
        backgroundColor: 'rgba(77, 96, 128, 0.06)',
        fill: false,
        tension: 0.35,
        pointRadius: 5,
        pointHoverRadius: 7,
        pointBackgroundColor: '#4d6080',
        borderDash: [4, 4],
      },
      {
        label: 'After Tax Balance Inflation-Corrected',
        data: points.map(p => p.afterTaxInflationCorrectedAmount),
        borderColor: '#f89b29',
        backgroundColor: 'rgba(248, 155, 41, 0.07)',
        fill: false,
        tension: 0.35,
        pointRadius: 5,
        pointHoverRadius: 7,
        pointBackgroundColor: '#f89b29',
        borderDash: [8, 3],
      }
    ];

    // Add SIA datasets only when Irish investor mode is on and SIA % is configured
    if (this.isIrishInvestor && this.projectionSettings.siaAnnualPercent > 0) {
      datasets.push(
        {
          label: 'Projected after tax (SIA)',
          data: points.map(p => p.afterTaxSia ?? 0),
          borderColor: '#2ec4b6',
          backgroundColor: 'rgba(46, 196, 182, 0.07)',
          fill: false,
          tension: 0.35,
          pointRadius: 5,
          pointHoverRadius: 7,
          pointBackgroundColor: '#2ec4b6',
          borderDash: [6, 4],
        },
        {
          label: 'After tax inflation-corrected (SIA)',
          data: points.map(p => p.afterTaxInflationCorrectedSia ?? 0),
          borderColor: '#9b5de5',
          backgroundColor: 'rgba(155, 93, 229, 0.07)',
          fill: false,
          tension: 0.35,
          pointRadius: 5,
          pointHoverRadius: 7,
          pointBackgroundColor: '#9b5de5',
          borderDash: [10, 4],
        }
      );
    }

    this.lineChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: points.map(p => p.year.toString()),
        datasets,
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Year', font: { weight: 'bold' }, color: '#8da0bf' }
          },
          y: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Total Value (€)', font: { weight: 'bold' }, color: '#8da0bf' },
            ticks: {
              color: '#8da0bf',
              callback: (value) => `€${Number(value).toLocaleString('de-IE', { maximumFractionDigits: 0 })}`
            }
          }
        },
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: { usePointStyle: true, padding: 20, color: '#8da0bf' }
          },
          tooltip: {
            backgroundColor: '#172040',
            borderColor: '#1f2e4a',
            borderWidth: 1,
            titleColor: '#e4eaf5',
            bodyColor: '#8da0bf',
            callbacks: {
              label: (tooltipCtx) => ` ${tooltipCtx.dataset.label}: ${this.formatCurrency(tooltipCtx.parsed.y as number)}`
            }
          }
        }
      }
    });
  }

  /** Called on init / view-switch. Loads versions first; if a default is set, calculates projection from it. */
  private initProjection(): void {
    this.projectionLoading = true;
    this.projectionChartRendered = false;
    this.lineChart?.destroy();
    this.lineChart = null;
    this.cdr.markForCheck();

    this.apiService.getProjectionVersions().subscribe({
      next: (versions) => {
        this.projectionVersions = versions;
        const defaultVersion = versions.find(v => v.isDefault) ?? null;

        if (defaultVersion) {
          this.defaultVersionName = defaultVersion.versionName;
          this.projectionSettings = { ...defaultVersion.settings };
          // Calculate a fresh projection using effective settings (zeroes Irish taxes if not Irish investor)
          this.apiService.calculateProjection(this.effectiveProjectionSettings).subscribe({
            next: (result) => {
              this.projection = result;
              this.projectionLoading = false;
              this.cdr.markForCheck();
            },
            error: () => {
              // Fallback: load from saved settings
              this.defaultVersionName = null;
              this.loadProjection();
            }
          });
        } else {
          this.defaultVersionName = null;
          this.loadProjection();
        }
      },
      error: () => {
        // If versions fail to load, fall back to normal projection load
        this.loadProjection();
      }
    });
  }

  loadProjection(): void {
    this.projectionLoading = true;
    this.projectionChartRendered = false;
    this.lineChart?.destroy();
    this.lineChart = null;
    this.apiService.getProjection().subscribe({
      next: (data) => {
        this.projectionSettings = { ...data.settings, deemedDisposalPercent: data.settings.deemedDisposalPercent ?? 41 };
        // Sync the toggle from the persisted server value
        this.isIrishInvestor = data.settings.isIrishInvestor;
        localStorage.setItem('isIrishInvestor', String(this.isIrishInvestor));
        if (!this.isIrishInvestor) {
          // Server data was calculated with Irish taxes; recalculate without them
          this.apiService.calculateProjection(this.effectiveProjectionSettings).subscribe({
            next: (recalc) => {
              this.projection = recalc;
              this.projectionLoading = false;
              this.cdr.markForCheck();
            },
            error: () => {
              this.projection = data;
              this.projectionLoading = false;
              this.cdr.markForCheck();
            }
          });
        } else {
          this.projection = data;
          this.projectionLoading = false;
          this.cdr.markForCheck();
        }
      },
      error: (err: HttpErrorResponse) => {
        console.error('Error loading projection:', err);
        if (err.status === 403 && this.sharingCtx.isViewingAsOther()) {
          this.sharingCtx.stopViewing();
        }
        this.projectionLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  saveProjectionSettings(): void {
    this.projectionSaving = true;
    // Always save the real settings (preserves Irish investor values even when toggle is off)
    this.apiService.saveProjectionSettings(this.projectionSettings).subscribe({
      next: () => {
        this.projectionSaving = false;
        // User has manually applied custom settings — clear the default version badge
        this.defaultVersionName = null;
        this.projectionChartRendered = false;
        this.lineChart?.destroy();
        this.lineChart = null;
        this.projectionLoading = true;
        this.cdr.markForCheck();
        // Calculate using effective settings (zeroes Irish taxes if not Irish investor)
        this.apiService.calculateProjection(this.effectiveProjectionSettings).subscribe({
          next: (result) => {
            this.projection = result;
            this.projectionLoading = false;
            this.cdr.markForCheck();
          },
          error: (err) => {
            console.error('Error calculating projection after save:', err);
            this.projectionLoading = false;
            this.cdr.markForCheck();
          }
        });
      },
      error: (err) => {
        console.error('Error saving projection settings:', err);
        this.projectionSaving = false;
        this.cdr.markForCheck();
      }
    });
  }

  setProjectionTab(tab: 'chart' | 'table' | 'versions'): void {
    this.activeProjectionTab = tab;
    if (tab === 'chart') {
      this.projectionChartRendered = false;
      this.lineChart?.destroy();
      this.lineChart = null;
    }
    // Versions are already eagerly loaded in initProjection(); only reload if empty (e.g. after error)
    if (tab === 'versions' && this.projectionVersions.length === 0 && !this.versionsLoading) {
      this.loadProjectionVersions();
    }
    this.cdr.markForCheck();
  }

  setMainSection(section: 'portfolio' | 'goal' | 'projections'): void {
    if (this.activeMainSection === section) return;

    this.activeMainSection = section;

    // Recreate charts when canvases are re-mounted after section switches.
    this.chartRendered = false;
    this.projectionChartRendered = false;
    this.evolutionChartRendered = false;
    this.versionsCompareChartRendered = false;
    this.goalChartRendered = false;

    this.pieChart?.destroy();
    this.pieChart = null;
    this.lineChart?.destroy();
    this.lineChart = null;
    this.evolutionChart?.destroy();
    this.evolutionChart = null;
    this.versionsCompareChart?.destroy();
    this.versionsCompareChart = null;
    this.goalChart?.destroy();
    this.goalChart = null;
    this.goalMonthlyChart?.destroy();
    this.goalMonthlyChart = null;

    if (section === 'goal' && !this.userGoal && !this.goalLoading) {
      this.loadGoal();
    }

    this.cdr.markForCheck();
  }

  saveNewVersion(): void {
    if (!this.newVersionName.trim()) return;
    this.versionSaving = true;
    const request: SaveVersionRequestDto = {
      versionName: this.newVersionName.trim(),
      settings: this.projectionSettings,
    };
    this.apiService.saveProjectionVersion(request).subscribe({
      next: (v) => {
        this.versionSaving = false;
        this.newVersionName = '';
        this.projectionVersions = [...this.projectionVersions, v];
        // Cache the frozen data points so comparison works immediately without an extra fetch
        if (v.dataPoints?.length) {
          this.versionCompareData.set(v.id, v.dataPoints);
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error saving projection version:', err);
        this.versionSaving = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadProjectionVersions(): void {
    this.versionsLoading = true;
    this.apiService.getProjectionVersions().subscribe({
      next: (data) => {
        this.projectionVersions = data;
        this.versionsLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading projection versions:', err);
        this.versionsLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  deleteVersion(versionId: number): void {
    if (this.confirmDeleteId !== versionId) {
      this.confirmDeleteId = versionId;
      this.cdr.markForCheck();
      return;
    }
    this.confirmDeleteId = null;
    this.versionDeleting = new Set(this.versionDeleting).add(versionId);
    this.cdr.markForCheck();
    this.apiService.deleteProjectionVersion(versionId).subscribe({
      next: () => {
        this.projectionVersions = this.projectionVersions.filter(v => v.id !== versionId);
        const nextDeleting = new Set(this.versionDeleting);
        nextDeleting.delete(versionId);
        this.versionDeleting = nextDeleting;
        if (this.selectedVersionIds.has(versionId)) {
          const nextSelected = new Set(this.selectedVersionIds);
          nextSelected.delete(versionId);
          this.selectedVersionIds = nextSelected;
          this.versionCompareData.delete(versionId);
          this.rebuildVersionsCompareChart();
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error deleting projection version:', err);
        const nextDeleting = new Set(this.versionDeleting);
        nextDeleting.delete(versionId);
        this.versionDeleting = nextDeleting;
        this.cdr.markForCheck();
      }
    });
  }

  cancelDelete(): void {
    this.confirmDeleteId = null;
    this.cdr.markForCheck();
  }

  setDefaultVersion(versionId: number): void {
    this.versionSettingDefault = new Set(this.versionSettingDefault).add(versionId);
    this.cdr.markForCheck();
    this.apiService.setDefaultProjectionVersion(versionId).subscribe({
      next: () => {
        // Update local list: unset all defaults, then set the target
        this.projectionVersions = this.projectionVersions.map(v => ({
          ...v,
          isDefault: v.id === versionId,
        }));
        const next = new Set(this.versionSettingDefault);
        next.delete(versionId);
        this.versionSettingDefault = next;
        // Apply the newly-set default version to params + graph
        const newDefault = this.projectionVersions.find(v => v.id === versionId);
        if (newDefault) {
          this.defaultVersionName = newDefault.versionName;
          this.projectionSettings = { ...newDefault.settings };
          this.projectionChartRendered = false;
          this.lineChart?.destroy();
          this.lineChart = null;
          this.apiService.calculateProjection(this.effectiveProjectionSettings).subscribe({
            next: (result) => { this.projection = result; this.cdr.markForCheck(); },
            error: () => this.cdr.markForCheck()
          });
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error setting default version:', err);
        const next = new Set(this.versionSettingDefault);
        next.delete(versionId);
        this.versionSettingDefault = next;
        this.cdr.markForCheck();
      }
    });
  }

  toggleVersionSelection(versionId: number): void {
    const next = new Set(this.selectedVersionIds);
    if (next.has(versionId)) {
      next.delete(versionId);
      this.selectedVersionIds = next;
      this.rebuildVersionsCompareChart();
    } else {
      next.add(versionId);
      this.selectedVersionIds = next;
      // Use the pre-loaded (frozen) data points from the version summary — no extra API call needed.
      if (!this.versionCompareData.has(versionId)) {
        const version = this.projectionVersions.find(v => v.id === versionId);
        if (version?.dataPoints?.length) {
          this.versionCompareData.set(versionId, version.dataPoints);
          this.rebuildVersionsCompareChart();
        } else {
          // Fallback: fetch detail if data points weren't included (e.g. older saved version)
          this.apiService.getProjectionVersionDetail(versionId).subscribe({
            next: (detail) => {
              this.versionCompareData.set(versionId, detail.dataPoints);
              this.rebuildVersionsCompareChart();
            },
            error: (err) => console.error('Error loading version detail:', err)
          });
        }
      } else {
        this.rebuildVersionsCompareChart();
      }
    }
  }

  private rebuildVersionsCompareChart(): void {
    this.versionsCompareChartRendered = false;
    this.versionsCompareChart?.destroy();
    this.versionsCompareChart = null;
    this.cdr.markForCheck();
  }

  // ── My Goal methods ─────────────────────────────────────────────────────────

  trackGoalPoint(_index: number, point: GoalDataPointDto): number {
    return point.year;
  }

  loadGoal(): void {
    this.goalLoading = true;
    this.cdr.markForCheck();
    this.apiService.getGoal().subscribe({
      next: (goal) => {
        this.userGoal = goal;
        this.goalDataPoints = goal.dataPoints.map(p => ({ ...p }));
        this.goalLoading = false;
        this.goalChartRendered = false;
        this.goalMonthlyChartRendered = false;
        this.goalChart?.destroy();
        this.goalChart = null;
        this.goalMonthlyChart?.destroy();
        this.goalMonthlyChart = null;
        this.cdr.markForCheck();
        // Fetch source version settings for the monthly breakdown chart
        if (goal.sourceVersionId) {
          this.apiService.getProjectionVersionDetail(goal.sourceVersionId).subscribe({
            next: (detail) => {
              this.goalVersionSettings = detail.settings;
              this.goalMonthlyChartRendered = false;
              this.cdr.markForCheck();
            },
            error: () => {
              this.goalVersionSettings = null;
              this.cdr.markForCheck();
            }
          });
        } else {
          this.goalVersionSettings = null;
        }
      },
      error: (err) => {
        // 404 = no goal set yet; any other error is logged
        if (err.status !== 404) {
          console.error('Error loading goal:', err);
        }
        this.goalLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  saveAsGoal(version: ProjectionVersionSummaryDto): void {
    this.goalSavingAsId = version.id;
    this.cdr.markForCheck();

    const dataPoints: GoalDataPointDto[] = (version.dataPoints ?? []).map(p => ({
      year: p.year,
      targetValue: p.totalAmount,
    }));

    const request: UpsertGoalRequestDto = {
      sourceVersionId: version.id,
      dataPoints,
    };

    this.apiService.upsertGoal(request).subscribe({
      next: (goal) => {
        this.userGoal = goal;
        this.goalDataPoints = goal.dataPoints.map(p => ({ ...p }));
        this.goalVersionSettings = version.settings;
        this.goalSavingAsId = null;
        this.goalChartRendered = false;
        this.goalMonthlyChartRendered = false;
        this.goalChart?.destroy();
        this.goalChart = null;
        this.goalMonthlyChart?.destroy();
        this.goalMonthlyChart = null;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error saving goal:', err);
        this.goalSavingAsId = null;
        this.cdr.markForCheck();
      }
    });
  }

  onGoalValueChange(index: number, rawValue: string): void {
    const parsed = parseFloat(rawValue);
    if (!isNaN(parsed) && parsed >= 0) {
      // Mutate in place — avoids object-reference change that makes *ngFor
      // destroy + recreate the input element (and lose focus).
      this.goalDataPoints[index].targetValue = parsed;
    }
  }

  saveGoalEdits(): void {
    if (!this.userGoal) return;
    this.goalSaving = true;
    this.cdr.markForCheck();

    const request: UpsertGoalRequestDto = {
      sourceVersionId: this.userGoal.sourceVersionId,
      dataPoints: this.goalDataPoints,
    };

    this.apiService.upsertGoal(request).subscribe({
      next: (goal) => {
        this.userGoal = goal;
        this.goalDataPoints = goal.dataPoints.map(p => ({ ...p }));
        this.goalSaving = false;
        // Refresh chart with the newly saved values
        this.goalChartRendered = false;
        this.goalChart?.destroy();
        this.goalChart = null;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error saving goal edits:', err);
        this.goalSaving = false;
        this.cdr.markForCheck();
      }
    });
  }

  private renderGoalChart(): void {
    this.goalChart?.destroy();
    if (!this.goalDataPoints.length) return;

    const ctx = this.goalChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const currentYear     = new Date().getFullYear();
    const actualValues    = this.computeActualYearlyPositions();
    // Solid orange: past years (actual end-of-year) + current year (projected year-end)
    const actualSolidData: (number | null)[] = this.goalDataPoints.map((p, i) =>
      p.year <= currentYear ? actualValues[i] : null
    );
    // Dashed orange: bridges current year into future forecast
    const actualDashedData: (number | null)[] = this.goalDataPoints.map((p, i) =>
      p.year >= currentYear ? actualValues[i] : null
    );

    this.goalChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: this.goalDataPoints.map(p => p.year.toString()),
        datasets: [
          {
            label: 'Projected Portfolio Value (Goal)',
            data: this.goalDataPoints.map(p => p.targetValue),
            borderColor: '#10B981',
            backgroundColor: 'rgba(16, 185, 129, 0.10)',
            fill: true,
            tension: 0.35,
            pointRadius: 5,
            pointHoverRadius: 7,
            pointBackgroundColor: '#10B981',
          },
          {
            label: 'Actual / Estimated Position',
            data: actualSolidData,
            borderColor: '#f89b29',
            backgroundColor: 'rgba(248, 155, 41, 0.07)',
            fill: false,
            tension: 0.35,
            pointRadius: 5,
            pointHoverRadius: 7,
            pointBackgroundColor: '#f89b29',
            spanGaps: false,
          } as any,
          {
            label: 'Position Forecast',
            data: actualDashedData,
            borderColor: 'rgba(248, 155, 41, 0.55)',
            backgroundColor: 'rgba(248, 155, 41, 0.03)',
            fill: false,
            tension: 0.35,
            pointRadius: 4,
            pointHoverRadius: 6,
            pointBackgroundColor: 'rgba(248, 155, 41, 0.55)',
            borderDash: [5, 4],
            spanGaps: false,
          } as any,
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Year', font: { weight: 'bold' }, color: '#8da0bf' }
          },
          y: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Target Value (€)', font: { weight: 'bold' }, color: '#8da0bf' },
            ticks: {
              color: '#8da0bf',
              callback: (value) => `€${Number(value).toLocaleString('de-IE', { maximumFractionDigits: 0 })}`
            }
          }
        },
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: { usePointStyle: true, padding: 20, color: '#8da0bf' }
          },
          tooltip: {
            backgroundColor: '#172040',
            borderColor: '#1f2e4a',
            borderWidth: 1,
            titleColor: '#e4eaf5',
            bodyColor: '#8da0bf',
            callbacks: {
              label: (tooltipCtx) =>
                ` ${tooltipCtx.dataset.label}: ${this.formatCurrency(tooltipCtx.parsed.y as number)}`
            }
          }
        }
      }
    });
  }

  setGoalSubTab(tab: 'yearly' | 'monthly'): void {
    if (this.goalSubTab === tab) return;
    this.goalSubTab = tab;
    if (tab === 'yearly') {
      this.goalChartRendered = false;
      this.goalChart?.destroy();
      this.goalChart = null;
    } else {
      this.goalMonthlyChartRendered = false;
      this.goalMonthlyChart?.destroy();
      this.goalMonthlyChart = null;
    }
    this.cdr.markForCheck();
  }

  private computeMonthlyGoalTargets(): { month: number; label: string; value: number; isFuture: boolean }[] | null {
    if (!this.goalDataPoints.length || !this.goalVersionSettings) return null;

    const today = new Date();
    const currentYear = today.getFullYear();
    const currentMonth = today.getMonth() + 1; // 1-based

    const MONTH_LABELS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const sortedPoints = [...this.goalDataPoints].sort((a, b) => a.year - b.year);
    const firstGoalYear = sortedPoints[0].year;

    const currentYearPoint = sortedPoints.find(p => p.year === currentYear);
    if (!currentYearPoint) return null;

    const settings = this.goalVersionSettings;
    const mg = Math.pow(1 + settings.yearlyReturnPercent / 100, 1 / 12); // monthly growth factor

    // Year index in the goal array (0 = first goal year) drives the annual buy increase
    const yearIndex = sortedPoints.findIndex(p => p.year === currentYear);
    const annualIncreaseFactor = Math.pow(1 + settings.annualBuyIncreasePercent / 100, yearIndex);
    const monthlyBuy = settings.monthlyBuyAmount * annualIncreaseFactor;

    const decTarget = currentYearPoint.targetValue;
    const values: number[] = new Array(12);

    if (currentYear === firstGoalYear) {
      // Case A: BACKWARD from Dec anchor
      values[11] = decTarget;
      for (let m = 10; m >= 0; m--) {
        values[m] = values[m + 1] / mg - monthlyBuy;
      }
    } else {
      // Case B: FORWARD from previous year's Dec target
      const prevYearPoint = sortedPoints.find(p => p.year === currentYear - 1);
      if (!prevYearPoint) return null;
      values[0] = (prevYearPoint.targetValue + monthlyBuy) * mg;
      for (let m = 1; m < 11; m++) {
        values[m] = (values[m - 1] + monthlyBuy) * mg;
      }
      // Snap December to exact goal target
      values[11] = decTarget;
    }

    return values.map((v, i) => ({
      month: i + 1,
      label: MONTH_LABELS[i],
      value: Math.round(v),
      isFuture: (i + 1) > currentMonth,
    }));
  }

  // ── Actual position helpers ──────────────────────────────────────────────────

  /**
   * Maps each goal-year to its actual end-of-year portfolio value (past years)
   * or a projected year-end estimate (current / future years).
   * Past  → last `totalValue` data point found in portfolioEvolution for that year.
   * Now   → projects current live holdings forward to December using goal settings.
   * Future→ continues compounding month-by-month from the current-year estimate.
   */
  private computeActualYearlyPositions(): (number | null)[] {
    const today = new Date();
    const currentYear  = today.getFullYear();
    const currentMonth = today.getMonth() + 1; // 1-based

    const sortedPoints = [...this.goalDataPoints].sort((a, b) => a.year - b.year);
    const valueByYear  = new Map<number, number | null>();
    let prevEstimate: number | null = null;

    for (let i = 0; i < sortedPoints.length; i++) {
      const year = sortedPoints[i].year;

      if (year < currentYear) {
        // Use last data point of that calendar year from portfolio evolution
        const pts = (this.portfolioEvolution?.dataPoints ?? [])
          .filter(p => p.date.startsWith(`${year}-`));
        if (pts.length > 0) {
          prevEstimate = pts[pts.length - 1].totalValue;
          valueByYear.set(year, prevEstimate);
        } else {
          valueByYear.set(year, null);
        }

      } else if (year === currentYear) {
        if (!this.goalVersionSettings || !this.dashboard) {
          valueByYear.set(year, null);
          prevEstimate = null;
        } else {
          const s  = this.goalVersionSettings;
          const mg = Math.pow(1 + s.yearlyReturnPercent / 100, 1 / 12);
          const mb = s.monthlyBuyAmount * Math.pow(1 + s.annualBuyIncreasePercent / 100, i);
          const mm = String(currentMonth).padStart(2, '0');
          const buyThisMonth = (this.portfolioEvolution?.dataPoints ?? [])
            .some(p => p.date.startsWith(`${currentYear}-${mm}-`) && p.hasBuy);

          // Start from live holdings; add planned buy if it hasn't happened yet
          let value = this.dashboard.header.totalHoldingsAmount + (buyThisMonth ? 0 : mb);
          // Project remaining full months to December
          for (let m = currentMonth + 1; m <= 12; m++) {
            value = (value + mb) * mg;
          }
          prevEstimate = value;
          valueByYear.set(year, Math.round(value));
        }

      } else {
        // Future year — compound forward 12 months from previous year-end estimate
        if (!this.goalVersionSettings || prevEstimate === null) {
          valueByYear.set(year, null);
          prevEstimate = null;
        } else {
          const s  = this.goalVersionSettings;
          const mg = Math.pow(1 + s.yearlyReturnPercent / 100, 1 / 12);
          const mb = s.monthlyBuyAmount * Math.pow(1 + s.annualBuyIncreasePercent / 100, i);
          let value: number = prevEstimate;
          for (let m = 0; m < 12; m++) {
            value = (value + mb) * mg;
          }
          prevEstimate = value;
          valueByYear.set(year, Math.round(value));
        }
      }
    }

    // Return values in the original goalDataPoints order (same order as chart labels)
    return this.goalDataPoints.map(p => valueByYear.get(p.year) ?? null);
  }

  /**
   * Returns actual (past) and forecast (current + future) monthly portfolio values
   * for the current calendar year.
   * Past months   → last daily snapshot from portfolio evolution for that month.
   * Current month → live holdings total, plus the planned monthly buy if it hasn't happened yet.
   * Future months → iterative (prev + monthlyBuy) × monthlyGrowthFactor.
   */
  private computeActualMonthlyPositions(): { value: number | null; type: 'actual' | 'current' | 'forecast'; pendingBuyAmount?: number }[] {
    const today        = new Date();
    const currentYear  = today.getFullYear();
    const currentMonth = today.getMonth() + 1; // 1-based

    const s = this.goalVersionSettings;
    const sortedGoalPoints = [...this.goalDataPoints].sort((a, b) => a.year - b.year);
    const yearIndex = sortedGoalPoints.findIndex(p => p.year === currentYear);
    const mg = s ? Math.pow(1 + s.yearlyReturnPercent / 100, 1 / 12) : 1;
    const mb = s
      ? s.monthlyBuyAmount * Math.pow(1 + s.annualBuyIncreasePercent / 100, yearIndex >= 0 ? yearIndex : 0)
      : 0;

    const result: { value: number | null; type: 'actual' | 'current' | 'forecast'; pendingBuyAmount?: number }[] = [];
    let prevValue: number | null = null;

    for (let m = 1; m <= 12; m++) {
      const mm = String(m).padStart(2, '0');

      if (m < currentMonth) {
        // Actual past month — take last data point of that month
        const pts = (this.portfolioEvolution?.dataPoints ?? [])
          .filter(p => p.date.startsWith(`${currentYear}-${mm}-`));
        if (pts.length > 0) {
          prevValue = pts[pts.length - 1].totalValue;
          result.push({ value: prevValue, type: 'actual' });
        } else {
          result.push({ value: null, type: 'actual' });
        }

      } else if (m === currentMonth) {
        if (!this.dashboard) {
          result.push({ value: null, type: 'current' });
          prevValue = null;
        } else {
          const buyThisMonth = (this.portfolioEvolution?.dataPoints ?? [])
            .some(p => p.date.startsWith(`${currentYear}-${mm}-`) && p.hasBuy);
          const value = this.dashboard.header.totalHoldingsAmount + (buyThisMonth ? 0 : mb);
          prevValue = value;
          result.push({ value, type: 'current', pendingBuyAmount: (!buyThisMonth && mb > 0) ? mb : undefined });
        }

      } else {
        // Future month — extrapolate from previous month
        if (!s || prevValue === null) {
          result.push({ value: null, type: 'forecast' });
        } else {
          const value: number = (prevValue + mb) * mg;
          prevValue   = value;
          result.push({ value: Math.round(value), type: 'forecast' });
        }
      }
    }

    return result;
  }

  private renderMonthlyGoalChart(): void {
    this.goalMonthlyChart?.destroy();

    const monthlyData = this.computeMonthlyGoalTargets();
    if (!monthlyData) return;

    const ctx = this.goalMonthlyChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    const currentMonth = new Date().getMonth() + 1;

    // Past/current months — solid line; current month bridged into both datasets
    const pastValues: (number | null)[] = monthlyData.map((p, i) =>
      (!p.isFuture) ? p.value : null
    );
    // Future months — dashed; bridge starts at current month point
    const futureValues: (number | null)[] = monthlyData.map((p, i) =>
      (i + 1 === currentMonth || p.isFuture) ? p.value : null
    );

    const pointRadii = monthlyData.map((p, i) =>
      (!p.isFuture) ? (i + 1 === currentMonth ? 9 : 5) : 0
    );
    const pointColors = monthlyData.map((_, i) =>
      i + 1 === currentMonth ? '#f89b29' : '#10B981'
    );

    // ── Actual position data ─────────────────────────────────────────────────
    const actualMonthly    = this.computeActualMonthlyPositions();
    const actualSolidData: (number | null)[] = actualMonthly.map(p =>
      (p.type === 'actual' || p.type === 'current') ? p.value : null
    );
    const actualDashedData: (number | null)[] = actualMonthly.map(p =>
      (p.type === 'current' || p.type === 'forecast') ? p.value : null
    );
    const actualPointRadii = actualMonthly.map(p =>
      p.type === 'current' ? 9 : p.type === 'actual' ? 5 : 0
    );
    const actualPointColors = actualMonthly.map(p =>
      p.type === 'current' ? '#f05252' : '#f89b29'
    );
    // Detect whether the current month's value includes a planned buy not yet executed
    const pendingBuy = actualMonthly[currentMonth - 1]?.pendingBuyAmount;

    this.goalMonthlyChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: monthlyData.map(p => p.label),
        datasets: [
          {
            label: `${new Date().getFullYear()} Monthly Target`,
            data: pastValues,
            borderColor: '#10B981',
            backgroundColor: 'rgba(16, 185, 129, 0.10)',
            fill: true,
            tension: 0.3,
            pointRadius: pointRadii,
            pointHoverRadius: 7,
            pointBackgroundColor: pointColors,
            spanGaps: false,
          } as any,
          {
            label: 'Forecast (future months)',
            data: futureValues,
            borderColor: 'rgba(16, 185, 129, 0.4)',
            backgroundColor: 'rgba(16, 185, 129, 0.03)',
            fill: false,
            tension: 0.3,
            pointRadius: 4,
            pointHoverRadius: 6,
            pointBackgroundColor: 'rgba(16, 185, 129, 0.4)',
            borderDash: [5, 4],
            spanGaps: false,
          } as any,
          {
            label: pendingBuy ? `Actual Position (+ ${this.formatCurrency(pendingBuy)} planned buy)` : 'Actual Position',
            data: actualSolidData,
            borderColor: '#f89b29',
            backgroundColor: 'rgba(248, 155, 41, 0.07)',
            fill: false,
            tension: 0.3,
            pointRadius: actualPointRadii,
            pointHoverRadius: 7,
            pointBackgroundColor: actualPointColors,
            spanGaps: false,
          } as any,
          {
            label: 'Position Forecast',
            data: actualDashedData,
            borderColor: 'rgba(248, 155, 41, 0.45)',
            backgroundColor: 'rgba(248, 155, 41, 0.03)',
            fill: false,
            tension: 0.3,
            pointRadius: 4,
            pointHoverRadius: 6,
            pointBackgroundColor: 'rgba(248, 155, 41, 0.45)',
            borderDash: [5, 4],
            spanGaps: false,
          } as any,
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Month', font: { weight: 'bold' }, color: '#8da0bf' }
          },
          y: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Target Value (€)', font: { weight: 'bold' }, color: '#8da0bf' },
            ticks: {
              color: '#8da0bf',
              callback: (value) => `€${Number(value).toLocaleString('de-IE', { maximumFractionDigits: 0 })}`
            }
          }
        },
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: { usePointStyle: true, padding: 20, color: '#8da0bf' }
          },
          tooltip: {
            backgroundColor: '#172040',
            borderColor: '#1f2e4a',
            borderWidth: 1,
            titleColor: '#e4eaf5',
            bodyColor: '#8da0bf',
            callbacks: {
              label: (tooltipCtx) => {
                const base = ` ${tooltipCtx.dataset.label}: ${this.formatCurrency(tooltipCtx.parsed.y as number)}`;
                // Dataset index 2 = "Actual Position"; warn when the planned buy is included
                if (pendingBuy && tooltipCtx.datasetIndex === 2 && tooltipCtx.dataIndex === currentMonth - 1) {
                  return [base, `   ⚠ includes ${this.formatCurrency(pendingBuy)} planned buy (not yet executed)`];
                }
                return base;
              }
            }
          }
        }
      }
    });
  }

  private renderVersionsCompareChart(): void {
    if (this.selectedVersionIds.size === 0) return;

    const ctx = this.versionsCompareChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    // Each metric has its own colour family; each version gets a different shade of that family
    const LINE_TYPE_CONFIG: { key: keyof ProjectionDataPointDto; label: string; dash: number[]; shades: string[] }[] = [
      { key: 'totalAmount',                       label: 'Projected Portfolio Value',             dash: [],      shades: ['#10B981', '#2563d4', '#7cb5ff', '#1a47a0'] },
      { key: 'inflationCorrectedAmount',          label: 'Inflation Corrected',                   dash: [6, 3],  shades: ['#f05252', '#b91c1c', '#f87171', '#7f1d1d'] },
      { key: 'afterTaxTotalAmount',               label: 'After Taxes',                           dash: [4, 4],  shades: ['#4d6080', '#2d4565', '#6d80a0', '#1d2f45'] },
      { key: 'afterTaxInflationCorrectedAmount',  label: 'After Tax + Inflation Corrected',       dash: [8, 3],  shades: ['#f89b29', '#c27416', '#fbba60', '#7c4a0a'] },
      { key: 'afterTaxSia',                       label: 'After Tax (SIA)',                       dash: [6, 4],  shades: ['#2ec4b6', '#178077', '#5de0d4', '#0e5550'] },
      { key: 'afterTaxInflationCorrectedSia',     label: 'After Tax + Inflation Corrected (SIA)', dash: [10, 4], shades: ['#9b5de5', '#6d28d9', '#bf8af7', '#4c1d95'] },
    ];

    const selectedIds = Array.from(this.selectedVersionIds);
    const datasets: any[] = [];

    selectedIds.forEach((id, versionIndex) => {
      if (!this.versionCompareData.has(id)) return;
      const version = this.projectionVersions.find(v => v.id === id);
      const dataPoints = this.versionCompareData.get(id) ?? [];

      LINE_TYPE_CONFIG.forEach((lineType, lineIndex) => {
        // Skip SIA lines when this version has no meaningful SIA data
        if (lineType.key === 'afterTaxSia' || lineType.key === 'afterTaxInflationCorrectedSia') {
          const hasSia = dataPoints.some(p => ((p as any)[lineType.key] ?? 0) > 0);
          if (!hasSia) return;
        }

        const data = dataPoints.map(p => (p as any)[lineType.key] ?? 0);
        // Colour = metric family hue, shade = version index
        const color = lineType.shades[versionIndex % lineType.shades.length];
        datasets.push({
          label: `${version?.versionName ?? `#${id}`} — ${lineType.label}`,
          data,
          borderColor: color,
          backgroundColor: color + '18',
          fill: false,
          tension: 0.35,
          pointRadius: 4,
          pointHoverRadius: 6,
          pointBackgroundColor: color,
          borderDash: lineType.dash,
          borderWidth: lineIndex === 0 ? 2.5 : 1.5,
          hidden: lineIndex > 0, // only "Projected Portfolio Value" visible by default
        });
      });
    });

    if (datasets.length === 0) return;

    const firstId = selectedIds.find(id => this.versionCompareData.has(id))!;
    const labels = (this.versionCompareData.get(firstId) ?? []).map(p => p.year.toString());

    this.versionsCompareChart = new Chart(ctx, {
      type: 'line',
      data: { labels, datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Year', font: { weight: 'bold' }, color: '#8da0bf' }
          },
          y: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Value (€)', font: { weight: 'bold' }, color: '#8da0bf' },
            ticks: {
              color: '#8da0bf',
              callback: (value) => `€${Number(value).toLocaleString('de-IE', { maximumFractionDigits: 0 })}`
            }
          }
        },
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: { usePointStyle: true, padding: 14, color: '#8da0bf', font: { size: 11 } }
          },
          tooltip: {
            backgroundColor: '#172040',
            borderColor: '#1f2e4a',
            borderWidth: 1,
            titleColor: '#e4eaf5',
            bodyColor: '#8da0bf',
            callbacks: {
              label: (tooltipCtx) =>
                ` ${tooltipCtx.dataset.label}: ${this.formatCurrency(tooltipCtx.parsed.y as number)}`
            }
          }
        }
      }
    });
  }

  setEvolutionPeriod(period: 'all' | 'year' | 'month' | 'week'): void {
    this.evolutionPeriod = period;
    this.evolutionChartRendered = false;
    this.evolutionChart?.destroy();
    this.evolutionChart = null;
    this.cdr.markForCheck();
  }

  private renderEvolutionChart(): void {
    this.evolutionChart?.destroy();
    const points = this.filteredEvolutionPoints;
    if (!points?.length) return;

    const ctx = this.evolutionChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    // Build per-point arrays for dynamic point styling
    const pointRadii = points.map(p => (p.hasBuy ? 8 : 3));
    const pointColors = points.map(p => (p.hasBuy ? '#f05252' : '#10B981'));
    const pointBorderColors = points.map(p => (p.hasBuy ? '#c0392b' : '#10B981'));
    const pointBorderWidths = points.map(p => (p.hasBuy ? 2 : 1));
    const pointHoverRadii = points.map(p => (p.hasBuy ? 10 : 5));

    this.evolutionChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: points.map(p => p.date),
        datasets: [
          {
            label: 'Portfolio Value',
            data: points.map(p => p.totalValue),
            borderColor: '#10B981',
            backgroundColor: 'rgba(16, 185, 129, 0.08)',
            fill: true,
            tension: 0.25,
            pointRadius: pointRadii,
            pointHoverRadius: pointHoverRadii,
            pointBackgroundColor: pointColors,
            pointBorderColor: pointBorderColors,
            pointBorderWidth: pointBorderWidths,
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        scales: {
          x: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Date', font: { weight: 'bold' }, color: '#8da0bf' },
            ticks: {
              color: '#8da0bf',
              maxTicksLimit: 20,
              maxRotation: 45,
            }
          },
          y: {
            ...DARK_SCALE_DEFAULTS,
            title: { display: true, text: 'Total Value (€)', font: { weight: 'bold' }, color: '#8da0bf' },
            ticks: {
              color: '#8da0bf',
              callback: (value) => `€${Number(value).toLocaleString('de-IE', { maximumFractionDigits: 0 })}`
            }
          }
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: '#172040',
            borderColor: '#1f2e4a',
            borderWidth: 1,
            titleColor: '#e4eaf5',
            bodyColor: '#8da0bf',
            callbacks: {
              title: (items) => {
                const idx = items[0].dataIndex;
                const pt = points[idx];
                return pt.hasBuy ? `${pt.date}  🛒 Buy recorded` : pt.date;
              },
              label: (tooltipCtx) =>
                ` Portfolio: ${this.formatCurrency(tooltipCtx.parsed.y as number)}`
            }
          }
        }
      }
    });
  }

  loadPortfolioEvolution(): void {
    this.evolutionLoading = true;
    this.evolutionChartRendered = false;
    this.evolutionChart?.destroy();
    this.evolutionChart = null;
    this.apiService.getPortfolioEvolution().subscribe({
      next: (data) => {
        this.portfolioEvolution = data;
        this.evolutionLoading = false;
        // If the goal section is already visible, force the charts to re-render
        // now that portfolio evolution data (needed for the actual position overlay) is ready.
        if (this.activeMainSection === 'goal' && this.goalDataPoints.length > 0) {
          this.goalChartRendered = false;
          this.goalChart?.destroy();
          this.goalChart = null;
          this.goalMonthlyChartRendered = false;
          this.goalMonthlyChart?.destroy();
          this.goalMonthlyChart = null;
        }
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading portfolio evolution:', err);
        this.evolutionLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadDashboard(): void {
    console.log('Loading dashboard...');
    this.loading = true;
    this.error = null;
    this.chartRendered = false;
    this.pieChart?.destroy();
    this.pieChart = null;
    this.apiService.getDashboard().subscribe({
      next: (data: DashboardDto) => {
        console.log('Dashboard data received:', data);
        this.dashboard = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        console.error('Error loading dashboard:', err);
        if (err.status === 403 && this.sharingCtx.isViewingAsOther()) {
          // Share was revoked or invalid – return to own portfolio
          this.sharingCtx.stopViewing();
          this.error = 'Access to this shared portfolio is no longer available.';
          this.loadDashboard();
        } else {
          this.error = 'Failed to load dashboard. Please ensure the API is running.';
        }
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('de-IE', {
      style: 'currency',
      currency: 'EUR',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  }

  formatPercent(value: number): string {
    return value >= 0 ? `+${value.toFixed(2)}%` : `${value.toFixed(2)}%`;
  }

  formatQuantity(value: number): string {
    return value.toFixed(4);
  }

  getHoldingTotalCost(holding: HoldingDto): number {
    return holding.quantity * holding.averageCost;
  }

  getHoldingTotalGainLoss(holding: HoldingDto): number {
    return holding.totalValue - this.getHoldingTotalCost(holding);
  }

  getHoldingTotalGainLossPercent(holding: HoldingDto): number | null {
    const totalCost = this.getHoldingTotalCost(holding);
    if (totalCost <= 0) return null;

    return (this.getHoldingTotalGainLoss(holding) / totalCost) * 100;
  }

  getMetricsClass(gainLossEur: number): string {
    return gainLossEur >= 0 ? 'positive' : 'negative';
  }

  onAddTransactionClick(): void {
    this.showAddTransactionModal = true;
  }

  private refreshPortfolioData(): void {
    this.loadDashboard();
    this.loadPortfolioEvolution();
    this.loadProjection();
    this.loadTaxSummary();
  }

  onTransactionAdded(): void {
    this.showAddTransactionModal = false;
    this.refreshPortfolioData();
  }

  onTransactionCancelled(): void {
    this.showAddTransactionModal = false;
  }

  onBuyHistoryClick(holdingId: number): void {
    this.selectedHoldingId = holdingId;
    this.showBuyHistoryModal = true;
  }

  onHistoryModalClosed(): void {
    this.showBuyHistoryModal = false;
    this.selectedHoldingId = null;
  }

  onHistoryChanged(): void {
    // A transaction was deleted — reload portfolio data so holdings & metrics stay in sync
    this.refreshPortfolioData();
  }

  onSellClick(holding: HoldingDto): void {
    this.selectedSellHolding = holding;
    this.showSellModal = true;
    this.cdr.markForCheck();
  }

  onSellModalClosed(): void {
    this.showSellModal = false;
    this.selectedSellHolding = null;
    this.cdr.markForCheck();
  }

  onSellConfirmed(): void {
    this.showSellModal = false;
    this.selectedSellHolding = null;
    this.refreshPortfolioData();
    this.loadTaxSummary();
    this.cdr.markForCheck();
  }

  // ── Tax History Modal ──────────────────────────────────────────────────────

  onTaxHistoryClick(holding: HoldingDto): void {
    this.selectedTaxHolding = holding;
    this.showTaxHistoryModal = true;
    this.cdr.markForCheck();
  }

  onTaxHistoryModalClosed(): void {
    this.showTaxHistoryModal = false;
    this.selectedTaxHolding = null;
    this.cdr.markForCheck();
  }

  onTaxPaid(): void {
    this.loadTaxSummary();
    this.cdr.markForCheck();
  }

  // ── Tax Summary Section ────────────────────────────────────────────────────

  loadTaxSummary(): void {
    this.taxSummaryLoading = true;
    this.cdr.markForCheck();
    this.apiService.getTaxEvents().subscribe({
      next: (s) => {
        this.taxSummary = s;
        this.taxSummaryLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.taxSummaryLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onMarkAllPaid(year?: number): void {
    if (this.sharingCtx.isReadOnly()) return;
    this.taxMarkingAllYear = year ?? 0;
    this.cdr.markForCheck();
    this.apiService.markAllTaxEventsPaid(year).subscribe({
      next: () => {
        this.taxMarkingAllYear = null;
        this.loadTaxSummary();
      },
      error: () => {
        this.taxMarkingAllYear = null;
        this.cdr.markForCheck();
      }
    });
  }

  onMarkSinglePaid(event: TaxEventDto): void {
    if (this.sharingCtx.isReadOnly()) return;
    this.apiService.markTaxEventPaid(event.id).subscribe({
      next: () => this.loadTaxSummary(),
      error: () => {}
    });
  }

  getTaxCurrentYear(): number { return new Date().getFullYear(); }

  getTaxPendingCurrentYear(): number {
    if (!this.taxSummary) return 0;
    const y = this.getTaxCurrentYear();
    return this.taxSummary.events
      .filter(e => e.status === 'Pending' && new Date(e.eventDate).getFullYear() === y)
      .reduce((s, e) => s + e.taxAmount, 0);
  }

  getTaxEventsByHolding(): { holdingId: number; ticker: string; etfName: string | null; events: TaxEventDto[] }[] {
    if (!this.taxSummary) return [];
    const map = new Map<number, { holdingId: number; ticker: string; etfName: string | null; events: TaxEventDto[] }>();
    for (const e of this.taxSummary.events) {
      if (!map.has(e.holdingId)) map.set(e.holdingId, { holdingId: e.holdingId, ticker: e.ticker, etfName: e.etfName, events: [] });
      map.get(e.holdingId)!.events.push(e);
    }
    return Array.from(map.values());
  }

  formatTaxEventType(t: string): string {
    return t === 'DeemedDisposal' ? '8-yr Deemed Disposal' : 'Sell';
  }

  getGroupPending(events: TaxEventDto[]): number {
    return events.filter(e => e.status === 'Pending').reduce((s, e) => s + e.taxAmount, 0);
  }


  copyPortfolioTotal(): void {
    if (this.dashboard) {
      this.projectionSettings.startAmount = Math.round(this.dashboard.header.totalHoldingsAmount);
      this.cdr.markForCheck();
    }
  }

}
