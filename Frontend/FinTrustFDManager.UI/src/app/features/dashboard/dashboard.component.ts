import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartConfiguration, ChartData } from 'chart.js';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {

  userName = 'Admin User';
  role = 'Administrator';

  totalInvestments = 128;
  totalAmount = '₹125.45 Cr';
  pendingApprovals = 18;
  approvedAmount = '₹98.30 Cr';

  pendingApprovalsList = [
    {
      fdNumber: 'FD2025/00018',
      entity: 'FinTrust Corp',
      amount: '₹5,00,00,000',
      submittedOn: '16 May 2025'
    },
    {
      fdNumber: 'FD2025/00017',
      entity: 'FinTrust Corp',
      amount: '₹2,50,00,000',
      submittedOn: '15 May 2025'
    },
    {
      fdNumber: 'FD2025/00016',
      entity: 'Tech Solutions Ltd',
      amount: '₹1,25,00,000',
      submittedOn: '14 May 2025'
    },
    {
      fdNumber: 'FD2025/00015',
      entity: 'Global Services Inc',
      amount: '₹3,75,00,000',
      submittedOn: '14 May 2025'
    },
    {
      fdNumber: 'FD2025/00014',
      entity: 'FinTrust Corp',
      amount: '₹2,00,00,000',
      submittedOn: '13 May 2025'
    }
  ];

  recentInvestments = [
    {
      fdNumber: 'FD2025/00013',
      entity: 'FinTrust Corp',
      amount: '₹1,00,00,000',
      status: 'Approved',
      createdOn: '12 May 2025'
    },
    {
      fdNumber: 'FD2025/00012',
      entity: 'Tech Solutions Ltd',
      amount: '₹2,30,00,000',
      status: 'Approved',
      createdOn: '10 May 2025'
    },
    {
      fdNumber: 'FD2025/00011',
      entity: 'Global Services Inc',
      amount: '₹3,10,00,000',
      status: 'Submitted',
      createdOn: '09 May 2025'
    },
    {
      fdNumber: 'FD2025/00010',
      entity: 'FinTrust Corp',
      amount: '₹1,80,00,000',
      status: 'Approved',
      createdOn: '08 May 2025'
    },
    {
      fdNumber: 'FD2025/00009',
      entity: 'FinTrust Corp',
      amount: '₹2,20,00,000',
      status: 'Rejected',
      createdOn: '07 May 2025'
    }
  ];

  investmentChartData: ChartConfiguration<'line'>['data'] = {
    labels: [
      'Dec 2024',
      'Jan 2025',
      'Feb 2025',
      'Mar 2025',
      'Apr 2025',
      'May 2025'
    ],
    datasets: [
      {
        data: [11, 18, 31, 24, 31, 27],
        label: 'Investment',
        fill: true,
        tension: 0.4
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
    labels: [
      'Draft',
      'Submitted',
      'Pending Approval',
      'Approved',
      'Rejected'
    ],
    datasets: [
      {
        data: [28, 18, 18, 52, 12]
      }
    ]
  };

  statusChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '65%',
    plugins: {
      legend: {
        display: false
      }
    }
  };
}
