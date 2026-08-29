import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { FDIdentificationService, FDLanding } from '../../../core/services/fd-identification.service';

@Component({
  selector: 'app-cash-flow-generation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cash-flow-generation.component.html',
  styleUrl: './cash-flow-generation.component.css'
})
export class CashFlowGenerationComponent implements OnInit {

  fdList: FDLanding[] = [];
  filteredList: FDLanding[] = [];
  loading = true;
  searchText = '';

  currentPage = 1;
  pageSize = 10;

  constructor(
    private fdService: FDIdentificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadFDs();
  }

  loadFDs(): void {
    this.loading = true;
    this.fdService.getLandingData().subscribe({
      next: (data) => {
        this.fdList = data.filter(fd =>
          fd.status === 'APPROVED' || fd.status === 'ACTIVE'
        ).sort((a, b) => b.fdId - a.fdId);
        this.applyFilter();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading FDs for cash flow generation', err);
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    if (this.searchText && this.searchText.trim()) {
      const term = this.searchText.toLowerCase().trim();
      this.filteredList = this.fdList.filter(fd =>
        (fd.fdReferenceNo || '').toLowerCase().includes(term) ||
        (fd.entityName || '').toLowerCase().includes(term) ||
        (fd.counterPartyName || '').toLowerCase().includes(term)
      );
    } else {
      this.filteredList = [...this.fdList];
    }
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

  goToCashFlow(fd: FDLanding): void {
    this.router.navigate(['/fd-detail', fd.fdId], { queryParams: { tab: 'cashflow' } });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'APPROVED': return 'status-approved';
      case 'ACTIVE': return 'status-active';
      default: return '';
    }
  }
}
