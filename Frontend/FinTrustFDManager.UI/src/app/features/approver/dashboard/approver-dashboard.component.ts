import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ApprovalService, ApproverDashboardSummary } from '../../../core/services/approval.service';
import { FDLanding } from '../../../core/services/fd-identification.service';
import { EntityService, EntityDto } from '../../../core/services/entity.service';

@Component({
  selector: 'app-approver-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './approver-dashboard.component.html',
  styleUrl: './approver-dashboard.component.css'
})
export class ApproverDashboardComponent implements OnInit {

  // User info from localStorage
  userName = '';
  userRole = '';

  // Dashboard summary
  isLoadingSummary = true;
  summary: ApproverDashboardSummary | null = null;

  // Pending approvals table
  pendingApprovals: FDLanding[] = [];
  filterEntity: string | number = '';
  filterStatus: string = '';
  filterType: string = '';
  filteredApprovals: FDLanding[] = [];
  isLoadingTable = true;
  hasError = false;
  errorMessage = '';

  // Search & Filter
  searchText = '';

  // Pagination
  currentPage = 1;
  pageSize = 10;

  // Entities for filter dropdown
  entities: EntityDto[] = [];

  // Approve/Reject modals
  showApproveModal = false;
  showRejectModal = false;
  selectedFd: FDLanding | null = null;
  approveComments = '';
  rejectComments = '';
  isProcessing = false;

  // View details modal
  showDetailModal = false;
  detailFd: FDLanding | null = null;
  detailHistory: any[] = [];
  loadingHistory = false;

  // Success notification
  showSuccessToast = false;
  successMessage = '';

  constructor(
    private approvalService: ApprovalService,
    private entityService: EntityService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadDashboardData();
    this.loadEntities();
  }

  loadUserInfo(): void {
    this.userName = localStorage.getItem('userName') || 'Approver';
    this.userRole = localStorage.getItem('role') || 'Approver';
  }

  /**
   * Load dashboard data. On initial load, shows loading state.
   * On refresh (after approve/reject), does background refresh without flash.
   */
  loadDashboardData(isRefresh = false): void {
    if (!isRefresh) {
      this.isLoadingSummary = true;
      this.isLoadingTable = true;
    }

    // Load summary
    this.approvalService.getDashboardSummary().subscribe({
      next: (data) => {
        this.summary = data;
        this.isLoadingSummary = false;
      },
      error: (err) => {
        console.error('Error loading dashboard summary', err);
        this.isLoadingSummary = false;
      }
    });

    // Load pending approvals
    this.approvalService.getPendingApprovals().subscribe({
      next: (data) => {
        this.pendingApprovals = data;
        this.applyFilters();
        this.isLoadingTable = false;
      },
      error: (err) => {
        console.error('Error loading pending approvals', err);
        this.hasError = true;
        this.errorMessage = err?.message || 'Unable to load approval requests.';
        this.isLoadingTable = false;
      }
    });
  }

  loadEntities(): void {
    this.entityService.getAll().subscribe({
      next: (data) => {
        this.entities = data.filter(e => e.status === 1);
      },
      error: () => { }
    });
  }

  // ==============================
  // SEARCH & FILTER
  // ==============================

  applyFilters(): void {
    let result = [...this.pendingApprovals];

    // Search
    if (this.searchText && this.searchText.trim()) {
      const term = this.searchText.toLowerCase().trim();
      result = result.filter(fd =>
        (fd.fdReferenceNo || '').toLowerCase().includes(term) ||
        (fd.entityName || '').toLowerCase().includes(term) ||
        (fd.counterPartyName || '').toLowerCase().includes(term) ||
        (fd.currencyCode || '').toLowerCase().includes(term)
      );
    }

    // Filter by entity
    if (this.filterEntity) {
      const entityId = Number(this.filterEntity);
      result = result.filter(fd => fd.entityId === entityId);
    }

    // Filter by status
    if (this.filterStatus) {
      result = result.filter(fd => fd.status === this.filterStatus);
    }

    // Filter by type
    if (this.filterType) {
      result = result.filter(fd => fd.type === this.filterType);
    }

    this.filteredApprovals = result;
    this.currentPage = 1;
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchText = '';
    this.filterEntity = '';
    this.filterStatus = '';
    this.filterType = '';
    this.applyFilters();
  }

  // ==============================
  // PAGINATION
  // ==============================

  get paginatedList(): FDLanding[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.filteredApprovals.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredApprovals.length / this.pageSize) || 1;
  }

  get totalItems(): number {
    return this.filteredApprovals.length;
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  onPageSizeChange(): void {
    this.currentPage = 1;
  }

  // ==============================
  // VIEW DETAILS
  // ==============================

  viewDetails(fd: FDLanding): void {
    this.detailFd = fd;
    this.showDetailModal = true;
    this.detailHistory = [];
    this.loadingHistory = true;

    this.approvalService.getApprovalHistory(fd.fdId).subscribe({
      next: (data) => {
        this.detailHistory = data.sort((a: any, b: any) =>
          new Date(b.actionDate).getTime() - new Date(a.actionDate).getTime()
        );
        this.loadingHistory = false;
      },
      error: () => {
        this.detailHistory = [];
        this.loadingHistory = false;
      }
    });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.detailFd = null;
    this.detailHistory = [];
  }

  // ==============================
  // APPROVE
  // ==============================

  openApproveModal(fd: FDLanding): void {
    this.selectedFd = fd;
    this.approveComments = '';
    this.showApproveModal = true;
  }

  closeApproveModal(): void {
    if (this.isProcessing) return; // Prevent close while processing
    this.showApproveModal = false;
    this.selectedFd = null;
    this.approveComments = '';
  }

  confirmApprove(): void {
    if (!this.selectedFd || this.isProcessing) return;
    this.isProcessing = true;

    this.approvalService.approve(this.selectedFd.fdId, this.approveComments || undefined).subscribe({
      next: (res) => {
        this.closeApproveModal();
        this.isProcessing = false;
        this.showSuccess(res.message || 'FD approved successfully.');
        this.refreshData();
      },
      error: (err) => {
        this.isProcessing = false;
        const msg = this.getErrorMessage(err, 'Unable to approve FD.');
        alert(`Approve Error: ${msg}`);
      }
    });
  }

  // ==============================
  // REJECT
  // ==============================

  openRejectModal(fd: FDLanding): void {
    this.selectedFd = fd;
    this.rejectComments = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    if (this.isProcessing) return; // Prevent close while processing
    this.showRejectModal = false;
    this.selectedFd = null;
    this.rejectComments = '';
  }

  confirmReject(): void {
    if (!this.selectedFd || this.isProcessing) return;
    if (!this.rejectComments || this.rejectComments.length < 5) {
      alert('Rejection reason must be at least 5 characters.');
      return;
    }
    this.isProcessing = true;

    this.approvalService.reject(this.selectedFd.fdId, this.rejectComments).subscribe({
      next: (res) => {
        this.closeRejectModal();
        this.isProcessing = false;
        this.showSuccess(res.message || 'FD rejected.');
        this.refreshData();
      },
      error: (err) => {
        this.isProcessing = false;
        const msg = this.getErrorMessage(err, 'Unable to reject FD.');
        alert(`Reject Error: ${msg}`);
      }
    });
  }

  // ==============================
  // REFRESH (background, no loading flash)
  // ==============================

  refreshData(): void {
    this.loadDashboardData(true);
  }

  // ==============================
  // SUCCESS TOAST
  // ==============================

  showSuccess(message: string): void {
    this.successMessage = message;
    this.showSuccessToast = true;
    setTimeout(() => {
      this.showSuccessToast = false;
      this.successMessage = '';
    }, 3000);
  }

  // ==============================
  // ERROR HANDLING
  // ==============================

  private getErrorMessage(err: any, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      switch (err.status) {
        case 400:
          return err.error?.message || 'Validation error. Please check your request.';
        case 401:
          return 'Session expired. Please log in again.';
        case 403:
          return 'You do not have permission to perform this action.';
        case 404:
          return 'FD not found. It may have been deleted.';
        case 409:
          return err.error?.message || 'This approval request has already been processed.';
        case 500:
          return 'An unexpected server error occurred. Please try again.';
        default:
          return err.error?.message || fallback;
      }
    }
    return err?.message || fallback;
  }

  // ==============================
  // HELPERS
  // ==============================

  getStatusClass(status: string): string {
    switch (status) {
      case 'PENDING_APPROVAL': return 'status-pending';
      case 'APPROVED': return 'status-approved';
      case 'REJECTED': return 'status-rejected';
      case 'DRAFT': return 'status-draft';
      default: return '';
    }
  }

  isCritical(fd: FDLanding): boolean {
    return (fd.principalAmount || 0) >= 10_000_000;
  }

  openFdDetail(fd: FDLanding): void {
    this.router.navigate(['/fd-detail', fd.fdId], { queryParams: { tab: 'general' } });
  }

  getPages(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;
    let start = Math.max(1, this.currentPage - Math.floor(maxVisible / 2));
    let end = Math.min(this.totalPages, start + maxVisible - 1);
    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }
}
