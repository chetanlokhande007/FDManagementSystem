import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ApproverService } from '../../../core/services/approver.service';
import { FDLanding } from '../../../core/services/fd-identification.service';

@Component({
  selector: 'app-approver-pending',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './approver-pending.component.html',
  styleUrl: './approver-pending.component.css'
})
export class ApproverPendingComponent implements OnInit {
  pendingList: FDLanding[] = [];
  filteredList: FDLanding[] = [];
  loading = false;
  hasError = false;

  searchText = '';
  statusFilter = '';
  currentPage = 1;
  pageSize = 10;

  constructor(private approverService: ApproverService) {}

  ngOnInit(): void {
    this.loadPending();
  }

  loadPending(): void {
    this.loading = true;
    this.hasError = false;

    this.approverService.getFDsByStatus('PENDING_FD_ADMIN').subscribe({
      next: (data) => {
        this.pendingList = data.sort((a, b) => (b.fdId || 0) - (a.fdId || 0));
        this.applyFilters();
        this.loading = false;
      },
      error: () => {
        this.hasError = true;
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let result = [...this.pendingList];

    if (this.searchText.trim()) {
      const s = this.searchText.toLowerCase();
      result = result.filter(fd =>
        (fd.fdReferenceNo || '').toLowerCase().includes(s) ||
        (fd.entityName || '').toLowerCase().includes(s) ||
        (fd.counterPartyName || '').toLowerCase().includes(s)
      );
    }

    if (this.statusFilter) {
      result = result.filter(fd => fd.status === this.statusFilter);
    }

    this.filteredList = result;
    this.currentPage = 1;
  }

  get paginatedList(): FDLanding[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredList.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredList.length / this.pageSize) || 1;
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }

  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'PENDING_FD_ADMIN':
      case 'PENDING_CA':
        return 'pending';
      case 'APPROVED':
        return 'approved';
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
    return status.replace(/_/g, ' ');
  }
}
