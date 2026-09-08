import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ApproverService } from '../../../core/services/approver.service';
import { FDLanding } from '../../../core/services/fd-identification.service';

@Component({
  selector: 'app-approver-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './approver-list.component.html',
  styleUrl: './approver-list.component.css'
})
export class ApproverListComponent implements OnInit {
  fdList: FDLanding[] = [];
  filteredList: FDLanding[] = [];
  loading = false;
  hasError = false;

  statusFilter: string = '';
  searchText = '';
  currentPage = 1;
  pageSize = 10;

  title = 'All Fixed Deposits';

  constructor(
    private route: ActivatedRoute,
    private approverService: ApproverService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.statusFilter = params['status'] || '';
      this.updateTitle();
      this.loadFDs();
    });
  }

  updateTitle(): void {
    switch (this.statusFilter) {
      case 'APPROVED':
        this.title = 'Approved Fixed Deposits';
        break;
      case 'REJECTED':
      case 'FD_ADMIN_REJECTED':
      case 'CA_REJECTED':
        this.title = 'Rejected Fixed Deposits';
        break;
      case 'PENDING_FD_ADMIN':
      case 'PENDING_CA':
        this.title = 'Pending Fixed Deposits';
        break;
      case 'ACTIVE':
        this.title = 'Active Fixed Deposits';
        break;
      case 'RETURNED_TO_CREATOR':
        this.title = 'Returned Fixed Deposits';
        break;
      default:
        this.title = 'All Fixed Deposits';
    }
  }

  loadFDs(): void {
    this.loading = true;
    this.hasError = false;

    this.approverService.getFDsByStatus(this.statusFilter || undefined).subscribe({
      next: (data) => {
        this.fdList = data.sort((a, b) => (b.fdId || 0) - (a.fdId || 0));
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
    let result = [...this.fdList];

    if (this.searchText.trim()) {
      const s = this.searchText.toLowerCase();
      result = result.filter(fd =>
        (fd.fdReferenceNo || '').toLowerCase().includes(s) ||
        (fd.entityName || '').toLowerCase().includes(s) ||
        (fd.counterPartyName || '').toLowerCase().includes(s)
      );
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

  nextPage(): void { if (this.currentPage < this.totalPages) this.currentPage++; }
  prevPage(): void { if (this.currentPage > 1) this.currentPage--; }

  getStatusClass(status: string): string {
    switch (status) {
      case 'PENDING_FD_ADMIN':
      case 'PENDING_CA': return 'pending';
      case 'APPROVED': return 'approved';
      case 'ACTIVE': return 'active';
      case 'REJECTED':
      case 'FD_ADMIN_REJECTED':
      case 'CA_REJECTED': return 'rejected';
      case 'RETURNED_TO_CREATOR': return 'returned';
      default: return 'draft';
    }
  }

  formatStatus(status: string): string {
    return (status || '').replace(/_/g, ' ');
  }
}
