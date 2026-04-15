import { Component, OnInit, OnDestroy, AfterViewChecked, ChangeDetectionStrategy, ChangeDetectorRef, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, DashboardDto, HoldingDto, ProjectionResultDto, ProjectionSettingsDto, PortfolioEvolutionDto, ProjectionVersionSummaryDto, ProjectionVersionDetailDto, ProjectionDataPointDto, SaveVersionRequestDto, UserGoalDto, GoalDataPointDto, UpsertGoalRequestDto } from '../../services/api.service';
import { AuthService, CurrentUser } from '../../services/auth.service';
import { AddTransactionModalComponent } from '../../components/add-transaction-modal/add-transaction-modal.component';
import { BuyHistoryModalComponent } from '../../components/buy-history-modal/buy-history-modal.component';
import { ShareProfileModalComponent } from '../../components/share-profile-modal/share-profile-modal.component';
import { SharingContextService } from '../../services/sharing-context.service';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Chart, ArcElement, Tooltip, Legend, PieController,
  LineController, LineElement, PointElement, LinearScale, CategoryScale, Filler,
  type ChartData
} from 'chart.js';

Chart.register(ArcElement, Tooltip, Legend, PieController, LineController, LineElement, PointElement, LinearScale, CategoryScale, Filler);

const PIE_COLORS = [
  '#4f8ef7', '#7c5cfc', '#06d6d0', '#14d990', '#f89b29',
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
  imports: [CommonModule, FormsModule, AddTransactionModalComponent, BuyHistoryModalComponent, ShareProfileModalComponent],
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
    cgtPercent: 38,
    exitTaxPercent: 38,
    excludePreExistingFromTax: false,
    siaAnnualPercent: 0,
    startAmount: null,
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

  @ViewChild('goalChart') goalChartRef!: ElementRef<HTMLCanvasElement>;
  private goalChart: Chart | null = null;
  private goalChartRendered = false;

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
        case 'daily':        av = a.dailyMetrics.gainLossEur;                bv = b.dailyMetrics.gainLossEur; break;
        case 'weekly':       av = a.weeklyMetrics.gainLossEur;               bv = b.weeklyMetrics.gainLossEur; break;
        case 'monthly':      av = a.monthlyMetrics.gainLossEur;              bv = b.monthlyMetrics.gainLossEur; break;
        case 'ytd':          av = a.ytdMetrics.gainLossEur;                  bv = b.ytdMetrics.gainLossEur; break;
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
  }

  onShareModalClosed(): void {
    this.showShareModal = false;
    this.cdr.markForCheck();
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
    this.pieChart?.destroy(); this.pieChart = null;
    this.lineChart?.destroy(); this.lineChart = null;
    this.evolutionChart?.destroy(); this.evolutionChart = null;
    this.goalChart?.destroy(); this.goalChart = null;
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
    this.loading = true;
    this.projectionLoading = true;
    this.projectionVersions = [];
    this.chartRendered = false;
    this.projectionChartRendered = false;
    this.evolutionChartRendered = false;
    this.goalChartRendered = false;
    this.pieChart?.destroy(); this.pieChart = null;
    this.lineChart?.destroy(); this.lineChart = null;
    this.evolutionChart?.destroy(); this.evolutionChart = null;
    this.goalChart?.destroy(); this.goalChart = null;
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
      this.goalDataPoints.length > 0 &&
      this.goalChartRef?.nativeElement
    ) {
      this.goalChartRendered = true;
      this.renderGoalChart();
    }
  }

  ngOnDestroy(): void {
    this.pieChart?.destroy();
    this.lineChart?.destroy();
    this.evolutionChart?.destroy();
    this.versionsCompareChart?.destroy();
    this.goalChart?.destroy();
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
        borderColor: '#4f8ef7',
        backgroundColor: 'rgba(79, 142, 247, 0.10)',
        fill: true,
        tension: 0.35,
        pointRadius: 5,
        pointHoverRadius: 7,
        pointBackgroundColor: '#4f8ef7',
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
        this.projectionSettings = { ...data.settings };
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
        this.goalChart?.destroy();
        this.goalChart = null;
        this.cdr.markForCheck();
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
        this.goalSavingAsId = null;
        this.goalChartRendered = false;
        this.goalChart?.destroy();
        this.goalChart = null;
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

    this.goalChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: this.goalDataPoints.map(p => p.year.toString()),
        datasets: [
          {
            label: 'Projected Portfolio Value (Goal)',
            data: this.goalDataPoints.map(p => p.targetValue),
            borderColor: '#14d990',
            backgroundColor: 'rgba(20, 217, 144, 0.10)',
            fill: true,
            tension: 0.35,
            pointRadius: 5,
            pointHoverRadius: 7,
            pointBackgroundColor: '#14d990',
          }
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

  private renderVersionsCompareChart(): void {
    this.versionsCompareChart?.destroy();
    if (this.selectedVersionIds.size === 0) return;

    const ctx = this.versionsCompareChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    // Each metric has its own colour family; each version gets a different shade of that family
    const LINE_TYPE_CONFIG: { key: keyof ProjectionDataPointDto; label: string; dash: number[]; shades: string[] }[] = [
      { key: 'totalAmount',                       label: 'Projected Portfolio Value',             dash: [],      shades: ['#4f8ef7', '#2563d4', '#7cb5ff', '#1a47a0'] },
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
    const pointColors = points.map(p => (p.hasBuy ? '#f05252' : '#4f8ef7'));
    const pointBorderColors = points.map(p => (p.hasBuy ? '#c0392b' : '#4f8ef7'));
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
            borderColor: '#4f8ef7',
            backgroundColor: 'rgba(79, 142, 247, 0.08)',
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

  getMetricsClass(gainLossEur: number): string {
    return gainLossEur >= 0 ? 'positive' : 'negative';
  }

  onAddTransactionClick(): void {
    this.showAddTransactionModal = true;
  }

  onTransactionAdded(): void {
    this.showAddTransactionModal = false;
    this.loadDashboard();
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
    this.loadDashboard();
    this.loadProjection();
  }


  copyPortfolioTotal(): void {
    if (this.dashboard) {
      this.projectionSettings.startAmount = Math.round(this.dashboard.header.totalHoldingsAmount);
      this.cdr.markForCheck();
    }
  }

}
