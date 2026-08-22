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

  userName = 'Admin User';
  role = 'Administrator';
  
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

  constructor(private dashboardService: DashboardService) {}

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

  isMaturityNear(status: string): boolean {
    return status.toLowerCase().includes('due in');
  }

  isMaturityCritical(status: string): boolean {
    return status.toLowerCase().includes('due in') && parseInt(status.replace(/\D/g, '')) <= 7;
  }
}
