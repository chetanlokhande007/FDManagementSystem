import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApproverService, ApprovalHistoryEntry } from '../../../core/services/approver.service';
import { FDLanding } from '../../../core/services/fd-identification.service';
import { ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-approver-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './approver-detail.component.html',
  styleUrl: './approver-detail.component.css'
})
export class ApproverDetailComponent implements OnInit {
  fdId: number = 0;
  fd: FDLanding | null = null;
  loading = true;
  hasError = false;

  // Action states
  isProcessing = false;
  showApproveConfirm = false;
  showRejectModal = false;
  showReturnModal = false;

  approveComments = '';
  rejectComments = '';
  returnComments = '';

  // Approval history
  history: ApprovalHistoryEntry[] = [];
  showHistory = false;
  loadingHistory = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private approverService: ApproverService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.fdId = Number(params.get('id'));
      if (this.fdId) {
        this.loadFDDetail();
      } else {
        this.hasError = true;
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadFDDetail(): void {
    this.loading = true;
    this.hasError = false;

    this.approverService.getFDById(this.fdId).subscribe({
      next: (data: any) => {
        // Map nested backend properties to flat frontend properties
        this.fd = {
          ...data,
          entityName: data.entity?.entityName || data.entityName,
          counterPartyName: data.counterParty?.counterPartyName || data.counterPartyName,
          currencyCode: data.currencyNavigation?.currencyCode || data.currencyCode
        };
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.hasError = true;
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get isPending(): boolean {
    if (!this.fd) return false;
    return this.fd.status === 'PENDING_FD_ADMIN' || this.fd.status === 'PENDING_CA';
  }

  // ========== APPROVE ==========
  openApproveConfirm(): void {
    this.approveComments = '';
    this.showApproveConfirm = true;
  }

  closeApproveConfirm(): void {
    this.showApproveConfirm = false;
  }

  confirmApprove(): void {
    if (!this.fd || this.isProcessing) return;
    this.isProcessing = true;

    this.approverService.approveFD(this.fdId, this.approveComments).subscribe({
      next: () => {
        this.showApproveConfirm = false;
        this.isProcessing = false;
        alert('FD approved successfully.');
        this.loadFDDetail();
        this.loadHistory();
      },
      error: (err) => {
        this.isProcessing = false;
        alert(err.error?.message || err.message || 'Failed to approve FD.');
      }
    });
  }

  // ========== REJECT ==========
  openRejectModal(): void {
    this.rejectComments = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
  }

  confirmReject(): void {
    if (!this.fd || this.isProcessing) return;
    if (!this.rejectComments || this.rejectComments.length < 5) {
      alert('Rejection reason must be at least 5 characters.');
      return;
    }
    this.isProcessing = true;

    this.approverService.rejectFD(this.fdId, this.rejectComments).subscribe({
      next: () => {
        this.showRejectModal = false;
        this.isProcessing = false;
        alert('FD rejected.');
        this.loadFDDetail();
        this.loadHistory();
      },
      error: (err) => {
        this.isProcessing = false;
        alert(err.error?.message || err.message || 'Failed to reject FD.');
      }
    });
  }

  // ========== RETURN TO CREATOR / REQUEST CHANGES ==========
  openReturnModal(): void {
    this.returnComments = '';
    this.showReturnModal = true;
  }

  closeReturnModal(): void {
    this.showReturnModal = false;
  }

  confirmReturn(): void {
    if (!this.fd || this.isProcessing) return;
    if (!this.returnComments || this.returnComments.length < 5) {
      alert('Reason must be at least 5 characters.');
      return;
    }
    this.isProcessing = true;

    this.approverService.returnToCreator(this.fdId, this.returnComments).subscribe({
      next: () => {
        this.showReturnModal = false;
        this.isProcessing = false;
        alert('FD returned to creator for changes.');
        this.loadFDDetail();
        this.loadHistory();
      },
      error: (err) => {
        this.isProcessing = false;
        alert(err.error?.message || err.message || 'Failed to return FD.');
      }
    });
  }

  // ========== HISTORY ==========
  toggleHistory(): void {
    this.showHistory = !this.showHistory;
    if (this.showHistory && this.history.length === 0) {
      this.loadHistory();
    }
  }

  loadHistory(): void {
    this.loadingHistory = true;
    this.approverService.getApprovalHistory(this.fdId).subscribe({
      next: (data) => {
        this.history = data.sort((a, b) =>
          new Date(b.actionDate).getTime() - new Date(a.actionDate).getTime()
        );
        this.loadingHistory = false;
      },
      error: () => {
        this.history = [];
        this.loadingHistory = false;
      }
    });
  }

  // ========== HELPERS ==========
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

  formatStatus(status: string): string {
    return (status || '').replace(/_/g, ' ');
  }

  getActionLabel(action: string): string {
    switch (action) {
      case 'CREATE': return 'Created';
      case 'EDIT': return 'Edited';
      case 'SUBMIT': return 'Submitted';
      case 'APPROVE': return 'Approved';
      case 'REJECT': return 'Rejected';
      case 'REQUEST_CHANGES': return 'Changes Requested';
      default: return action;
    }
  }

  getActionClass(action: string): string {
    switch (action) {
      case 'APPROVE': return 'action-approved';
      case 'REJECT': return 'action-rejected';
      case 'REQUEST_CHANGES': return 'action-return';
      case 'SUBMIT': return 'action-submit';
      default: return 'action-default';
    }
  }

  goBack(): void {
    this.router.navigate(['/approver/pending']);
  }
}
