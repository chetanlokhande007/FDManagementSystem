import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import {
  AdminApprovalService,
  AdminDashboardSummary,
  AdminApprovalDetail
} from '../../../core/services/admin-approval.service';
import { FDLanding } from '../../../core/services/fd-identification.service';
import { EntityService, EntityDto } from '../../../core/services/entity.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {

  // User info
  userName = '';
  userRole = '';

  // Dashboard summary
  isLoadingSummary = true;
  summary: AdminDashboardSummary | null = null;

  // Approval list
  allApprovals: FDLanding[] = [];
  filteredApprovals: FDLanding[] = [];
  isLoadingTable = true;
  hasError = false;
  errorMessage = '';

  // Active status filter tab
  activeStatusFilter: string = ''; // '' = all

  // Search & Filter
  searchText = '';
  filterEntity = '';

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

  // Detail review modal
  activeDropdown: number | null = null;
  showDetailModal = false;
  detailFd: FDLanding | null = null;
  detailData: AdminApprovalDetail | null = null;
  loadingDetail = false;
  detailError = false;
  detailTab: 'overview' | 'interest' | 'cashflow' | 'history' = 'overview';

  // Success toast
  showSuccessToast = false;
  successMessage = '';

  // Status tabs config
  statusTabs = [
    { key: '', label: 'All' },
    { key: 'PENDING_APPROVAL', label: 'Pending' },
    { key: 'APPROVED', label: 'Approved' },
    { key: 'REJECTED', label: 'Rejected' },
    { key: 'DRAFT', label: 'Draft' },
    { key: 'ACTIVE', label: 'Active' }
  ];

  constructor(
    private adminService: AdminApprovalService,
    private entityService: EntityService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadUserInfo();
    this.loadDashboardData();
    this.loadEntities();
  }

  loadUserInfo(): void {
    this.userName = localStorage.getItem('userName') || 'Admin';
    this.userRole = localStorage.getItem('role') || 'Admin';
  }

  loadDashboardData(isRefresh = false): void {
    if (!isRefresh) {
      this.isLoadingSummary = true;
      this.isLoadingTable = true;
    }

    // Load summary
    this.adminService.getDashboardSummary().subscribe({
      next: (data) => {
        this.summary = data;
        this.isLoadingSummary = false;
      },
      error: (err) => {
        console.error('Error loading admin dashboard summary', err);
        this.isLoadingSummary = false;
      }
    });

    // Load all approvals
    this.adminService.getApprovalList().subscribe({
      next: (data) => {
        this.allApprovals = data;
        this.applyFilters();
        this.isLoadingTable = false;
      },
      error: (err) => {
        console.error('Error loading approval list', err);
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
      error: () => {}
    });
  }

  // ==============================
  // STATUS TAB FILTERING
  // ==============================

  setStatusFilter(status: string): void {
    this.activeStatusFilter = status;
    this.currentPage = 1;
    this.applyFilters();
  }

  // ==============================
  // SEARCH & FILTER
  // ==============================

  applyFilters(): void {
    let result = [...this.allApprovals];

    // Status tab filter
    if (this.activeStatusFilter) {
      result = result.filter(fd => fd.status === this.activeStatusFilter);
    }

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
    this.activeStatusFilter = '';
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

  // ==============================
  // VIEW DETAIL (COMPREHENSIVE REVIEW)
  // ==============================

  viewDetails(fd: FDLanding): void {
    this.detailFd = fd;
    this.detailData = null;
    this.detailError = false;
    this.detailTab = 'overview';
    this.showDetailModal = true;
    this.loadingDetail = true;

    this.adminService.getApprovalDetail(fd.fdId).subscribe({
      next: (data) => {
        this.detailData = data;
        this.loadingDetail = false;
      },
      error: (err) => {
        console.error('Error loading admin approval detail', err);
        this.detailError = true;
        this.loadingDetail = false;
      }
    });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.detailFd = null;
    this.detailData = null;
    this.detailTab = 'overview';
  }

  selectDetailTab(tab: 'overview' | 'interest' | 'cashflow' | 'history'): void {
    this.detailTab = tab;
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
    if (this.isProcessing) return;
    this.showApproveModal = false;
    this.selectedFd = null;
    this.approveComments = '';
  }

  confirmApprove(): void {
    if (!this.selectedFd || this.isProcessing) return;
    this.isProcessing = true;

    this.adminService.approve(this.selectedFd.fdId, this.approveComments || undefined).subscribe({
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
    if (this.isProcessing) return;
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

    this.adminService.reject(this.selectedFd.fdId, this.rejectComments).subscribe({
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
  // REFRESH
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
        case 400: return err.error?.message || 'Validation error. Please check your request.';
        case 401: return 'Session expired. Please log in again.';
        case 403: return 'You do not have permission to perform this action.';
        case 404: return 'FD not found. It may have been deleted.';
        case 409: return err.error?.message || 'This record has already been processed by another Admin.';
        case 500: return 'An unexpected server error occurred. Please try again.';
        default: return err.error?.message || fallback;
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
      case 'SUBMITTED': return 'status-submitted';
      case 'ACTIVE': return 'status-active';
      default: return '';
    }
  }

  getStatusCount(status: string): number {
    if (!this.allApprovals) return 0;
    if (!status) return this.allApprovals.length;
    return this.allApprovals.filter(fd => fd.status === status).length;
  }

  isCritical(fd: FDLanding): boolean {
    return (fd.principalAmount || 0) >= 10_000_000;
  }

  openFdDetail(fd: FDLanding): void {
    this.router.navigate(['/fd-detail', fd.fdId], { queryParams: { tab: 'general' } });
  }

  canApprove(fd: FDLanding): boolean {
    // Basic logic for now until full backend workflow is implemented
    return fd.status === 'PENDING_APPROVAL' || fd.status === 'PENDING_FD_ADMIN' || fd.status === 'PENDING_CA';
  }

  toggleDropdown(fdId: number, event: Event): void {
    event.stopPropagation();
    if (this.activeDropdown === fdId) {
      this.activeDropdown = null;
    } else {
      this.activeDropdown = fdId;
    }
  }

  closeDropdowns(): void {
    this.activeDropdown = null;
  }
}
