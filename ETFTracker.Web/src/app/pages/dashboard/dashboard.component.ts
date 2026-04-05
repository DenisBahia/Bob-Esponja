import { Component, OnInit, OnDestroy, AfterViewChecked, ChangeDetectionStrategy, ChangeDetectorRef, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, DashboardDto, HoldingDto, ProjectionResultDto, ProjectionSettingsDto, PortfolioEvolutionDto, ProjectionVersionSummaryDto, ProjectionVersionDetailDto, ProjectionDataPointDto } from '../../services/api.service';
import { AuthService, CurrentUser } from '../../services/auth.service';
import { AddTransactionModalComponent } from '../../components/add-transaction-modal/add-transaction-modal.component';
import { BuyHistoryModalComponent } from '../../components/buy-history-modal/buy-history-modal.component';
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
  imports: [CommonModule, FormsModule, AddTransactionModalComponent, BuyHistoryModalComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit, OnDestroy, AfterViewChecked {
  dashboard: DashboardDto | null = null;
  loading = true;
  error: string | null = null;

  showAddTransactionModal = false;
  showBuyHistoryModal = false;
  selectedHoldingId: number | null = null;

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
    startAmount: null,
  };
  projectionLoading = false;
  projectionSaving = false;
  activeProjectionTab: 'chart' | 'table' | 'versions' = 'chart';
  currentYear = new Date().getFullYear();

  @ViewChild('projectionChart') projectionChartRef!: ElementRef<HTMLCanvasElement>;
  private lineChart: Chart | null = null;
  private projectionChartRendered = false;

  // Projection versions
  projectionVersions: ProjectionVersionSummaryDto[] = [];
  versionsLoading = false;
  versionSaving = false;
  selectedVersionIds: Set<number> = new Set();
  versionCompareData: Map<number, ProjectionDataPointDto[]> = new Map();
  versionsCompareChartRendered = false;

  @ViewChild('versionsCompareChart') versionsCompareChartRef!: ElementRef<HTMLCanvasElement>;
  private versionsCompareChart: Chart | null = null;

  // Holdings tabs
  activeHoldingsTab: 'table' | 'evolution' = 'table';
  portfolioEvolution: PortfolioEvolutionDto | null = null;
  evolutionLoading = false;

  @ViewChild('evolutionChart') evolutionChartRef!: ElementRef<HTMLCanvasElement>;
  private evolutionChart: Chart | null = null;
  private evolutionChartRendered = false;

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

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef, public auth: AuthService) {}

  ngOnInit(): void {
    console.log('Dashboard component initialized');
    this.loadDashboard();
    this.loadProjection();
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
  }

  ngOnDestroy(): void {
    this.pieChart?.destroy();
    this.lineChart?.destroy();
    this.evolutionChart?.destroy();
    this.versionsCompareChart?.destroy();
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

    this.lineChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: points.map(p => p.year.toString()),
        datasets: [
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
        ]
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

  loadProjection(): void {
    this.projectionLoading = true;
    this.projectionChartRendered = false;
    this.lineChart?.destroy();
    this.lineChart = null;
    this.apiService.getProjection().subscribe({
      next: (data) => {
        this.projection = data;
        this.projectionSettings = { ...data.settings };
        this.projectionLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error loading projection:', err);
        this.projectionLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  saveProjectionSettings(): void {
    this.projectionSaving = true;
    this.apiService.saveProjectionSettings(this.projectionSettings).subscribe({
      next: () => {
        this.projectionSaving = false;
        // Reload projection with new settings
        this.projectionChartRendered = false;
        this.lineChart?.destroy();
        this.lineChart = null;
        this.loadProjection();
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
    if (tab === 'versions' && this.projectionVersions.length === 0 && !this.versionsLoading) {
      this.loadProjectionVersions();
    }
    this.cdr.markForCheck();
  }

  saveNewVersion(): void {
    this.versionSaving = true;
    this.apiService.saveProjectionVersion(this.projectionSettings).subscribe({
      next: (v) => {
        this.versionSaving = false;
        this.projectionVersions = [...this.projectionVersions, v];
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

  toggleVersionSelection(versionId: number): void {
    const next = new Set(this.selectedVersionIds);
    if (next.has(versionId)) {
      next.delete(versionId);
      this.selectedVersionIds = next;
      this.rebuildVersionsCompareChart();
    } else {
      next.add(versionId);
      this.selectedVersionIds = next;
      if (!this.versionCompareData.has(versionId)) {
        this.apiService.getProjectionVersionDetail(versionId).subscribe({
          next: (detail) => {
            this.versionCompareData.set(versionId, detail.dataPoints);
            this.rebuildVersionsCompareChart();
          },
          error: (err) => console.error('Error loading version detail:', err)
        });
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

  private renderVersionsCompareChart(): void {
    this.versionsCompareChart?.destroy();
    if (this.selectedVersionIds.size === 0) return;

    const ctx = this.versionsCompareChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    const selectedIds = Array.from(this.selectedVersionIds);
    const datasets = selectedIds
      .filter(id => this.versionCompareData.has(id))
      .map((id, i) => {
        const version = this.projectionVersions.find(v => v.id === id);
        const dataPoints = this.versionCompareData.get(id) ?? [];
        const color = PIE_COLORS[i % PIE_COLORS.length];
        return {
          label: `Version ${version?.versionNumber ?? id}`,
          data: dataPoints.map(p => p.totalAmount),
          borderColor: color,
          backgroundColor: color + '22',
          fill: false,
          tension: 0.35,
          pointRadius: 4,
          pointHoverRadius: 6,
          pointBackgroundColor: color,
        };
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
            title: { display: true, text: 'Projected Portfolio Value (€)', font: { weight: 'bold' }, color: '#8da0bf' },
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

  private renderEvolutionChart(): void {
    this.evolutionChart?.destroy();
    const points = this.portfolioEvolution?.dataPoints;
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

  setHoldingsTab(tab: 'table' | 'evolution'): void {
    this.activeHoldingsTab = tab;
    if (tab === 'evolution') {
      this.evolutionChartRendered = false;
      this.evolutionChart?.destroy();
      this.evolutionChart = null;
    }
    this.cdr.markForCheck();
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
      error: (err: any) => {
        console.error('Error loading dashboard:', err);
        this.error = 'Failed to load dashboard. Please ensure the API is running.';
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

  getPriceSourceLabel(source: string | null): string {
    if (!source) return 'N/A';
    const labels: { [key: string]: string } = {
      'Eodhd': 'Eodhd',
      'Yahoo': 'Yahoo',
      'Cache': 'Cached'
    };
    return labels[source] || 'Unknown';
  }

  getPriceSourceDescription(source: string | null): string {
    if (!source) return 'Price data unavailable';
    const descriptions: { [key: string]: string } = {
      'Eodhd': 'Real-time price from Eodhd API (Premium)',
      'Yahoo': 'Real-time price from Yahoo Finance',
      'Cache': 'Cached price from last update'
    };
    return descriptions[source] || 'Unknown source';
  }

  copyPortfolioTotal(): void {
    if (this.dashboard) {
      this.projectionSettings.startAmount = Math.round(this.dashboard.header.totalHoldingsAmount);
      this.cdr.markForCheck();
    }
  }

}
