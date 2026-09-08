import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApproverService } from '../../../core/services/approver.service';
import { FDLanding } from '../../../core/services/fd-identification.service';

@Component({
  selector: 'app-approver-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './approver-dashboard.component.html',
  styleUrl: './approver-dashboard.component.css'
})
export class ApproverDashboardComponent implements OnInit, OnDestroy {
  userName = localStorage.getItem('userName') || 'Approver';
  isLoading = true;
  hasError = false;

  pendingCount = 0;
  approvedCount = 0;
  rejectedCount = 0;
  returnedCount = 0;
  draftCount = 0;
  activeCount = 0;

  recentPending: FDLanding[] = [];

  private pollTimer: any;

  constructor(private approverService: ApproverService) {}

  ngOnInit(): void {
    this.loadDashboard();
    // Poll every 30s for near real-time visibility
    this.pollTimer = setInterval(() => this.loadDashboard(true), 30000);
  }

  ngOnDestroy(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
    }
  }

  loadDashboard(silent = false): void {
    if (!silent) {
      this.isLoading = true;
    }
    this.hasError = false;

    this.approverService.getApproverSummary().subscribe({
      next: (counts) => {
        this.pendingCount = (counts['PENDING_FD_ADMIN'] || 0) + (counts['PENDING_CA'] || 0);
        this.approvedCount = counts['APPROVED'] || 0;
        this.rejectedCount = (counts['REJECTED'] || 0) + (counts['FD_ADMIN_REJECTED'] || 0) + (counts['CA_REJECTED'] || 0);
        this.returnedCount = counts['RETURNED_TO_CREATOR'] || 0;
        this.draftCount = counts['DRAFT'] || 0;
        this.activeCount = counts['ACTIVE'] || 0;
        this.isLoading = false;
      },
      error: () => {
        if (!silent) {
          this.hasError = true;
          this.isLoading = false;
        }
      }
    });

    // Load recent pending FDs
    this.approverService.getFDsByStatus('PENDING_FD_ADMIN').subscribe({
      next: (fds) => {
        this.recentPending = fds.slice(0, 5);
      },
      error: () => {}
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'PENDING_FD_ADMIN':
      case 'PENDING_CA':
        return 'pending';
      case 'APPROVED':
        return 'approved';
      case 'ACTIVE':
        return 'active';
      case 'REJECTED':
      case 'FD_ADMIN_REJECTED':
      case 'CA_REJECTED':
        return 'rejected';
      case 'RETURNED_TO_CREATOR':
        return 'returned';
      default:
        return 'draft';
    }
  }
}
