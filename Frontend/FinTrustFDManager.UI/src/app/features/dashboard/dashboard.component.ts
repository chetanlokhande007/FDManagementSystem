import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartConfiguration, ChartData } from 'chart.js';
import { DashboardService, DashboardSummaryDto, ChartDataDto } from '../../core/services/dashboard';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {

  userName = localStorage.getItem('userName') || 'User';
  role = localStorage.getItem('role') || '';

  isLoading = true;
  hasError = false;

  dashboard: DashboardSummaryDto | null = null;

  investmentChartData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Investment',
        fill: true,
        tension: 0.4,
        borderColor: '#0d6efd',
        backgroundColor: 'rgba(13, 110, 253, 0.1)'
      }
    ]
  };

  investmentChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      y: {
        beginAtZero: true
      }
    }
  };

  statusChartData: ChartData<'doughnut'> = {
    labels: [],
    datasets: [
      {
        data: [],
        backgroundColor: ['#0d6efd', '#198754', '#fd7e14', '#dc3545', '#ffc107']
      }
    ]
  };

  statusChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '65%',
    plugins: {
      legend: {
        display: true,
        position: 'right'
      }
    }
  };

  constructor(private dashboardService: DashboardService) { }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.hasError = false;

    this.dashboardService.getSummary().subscribe({
      next: (data) => {
        this.dashboard = data;
        this.updateCharts(data);
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading dashboard data', err);
        this.hasError = true;
        this.isLoading = false;
      }
    });
  }

  updateCharts(data: DashboardSummaryDto): void {
    // Growth Chart
    if (data.fdGrowthData && data.fdGrowthData.length > 0) {
      this.investmentChartData = {
        labels: data.fdGrowthData.map(d => d.label),
        datasets: [{
          data: data.fdGrowthData.map(d => d.value),
          label: 'FD Value (in Cr)',
          fill: true,
          tension: 0.4,
          borderColor: '#0d6efd',
          backgroundColor: 'rgba(13, 110, 253, 0.1)'
        }]
      };
    }

    // Portfolio Distribution Chart
    if (data.portfolioDistributionData && data.portfolioDistributionData.length > 0) {
      this.statusChartData = {
        labels: data.portfolioDistributionData.map(d => d.label),
        datasets: [{
          data: data.portfolioDistributionData.map(d => d.value),
          backgroundColor: ['#0d6efd', '#198754', '#fd7e14', '#dc3545', '#ffc107', '#20c997']
        }]
      };
    }
  }

  isMaturityCritical(status: string): boolean {
    return status === 'Matured' || status === 'Due Today' || (status.includes('Due in') && parseInt(status.split(' ')[2]) <= 3);
  }

  isMaturityNear(status: string): boolean {
    if (this.isMaturityCritical(status)) return false;
    return status.includes('Due in') && parseInt(status.split(' ')[2]) <= 15;
  }

  getPieChartGradient(): string {
    if (!this.dashboard || this.dashboard.portfolioDistributionData.length === 0) return 'conic-gradient(#edf2f9 0% 100%)';

    let gradient = 'conic-gradient(';
    let currentPercentage = 0;
    const colors = ['#0d6efd', '#198754', '#fd7e14', '#dc3545', '#6f42c1'];

    this.dashboard.portfolioDistributionData.forEach((item, index) => {
      const percentage = (item.value / this.dashboard!.totalPrincipal) * 100;
      const start = currentPercentage;
      const end = currentPercentage + percentage;
      const color = colors[index % colors.length];
      gradient += `${color} ${start}% ${end}%, `;
      currentPercentage = end;
    });

    gradient = gradient.slice(0, -2) + ')';
    return gradient;
  }

  getLegendColorClass(index: number): string {
    const classes = ['bg-blue', 'bg-green', 'bg-orange', 'bg-red', 'bg-purple'];
    return classes[index % classes.length];
  }

  // Helper properties and methods for dynamic chart scales
  Math = Math;

  getMaxValue(): number {
    if (!this.dashboard || this.dashboard.fdGrowthData.length === 0) return 100;
    const max = Math.max(...this.dashboard.fdGrowthData.map(d => d.value));
    return max > 0 ? max : 100;
  }

  getMaxCount(): number {
    if (!this.dashboard || this.dashboard.fdGrowthData.length === 0) return 10;
    const max = Math.max(...this.dashboard.fdGrowthData.map(d => d.count));
    return max > 0 ? max : 10;
  }
}
