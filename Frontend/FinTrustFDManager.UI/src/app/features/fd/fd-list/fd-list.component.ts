import {
  Component,
  OnInit,
  HostListener,
  ElementRef,
  ChangeDetectorRef
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  Router
} from '@angular/router';

import {
  FDIdentificationService,
  FDLanding
} from '../../../core/services/fd-identification.service';

@Component({
  selector: 'app-fd-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './fd-list.component.html',
  styleUrl: './fd-list.component.css'
})
export class FDListComponent implements OnInit {

  fdList: FDLanding[] = [];
  loading = false;

  currentPage = 1;
  pageSize = 5;
  expandedFdId: number | null = null;
  openDropdownId: number | null = null;

  constructor(
    private fdService: FDIdentificationService,
    private router: Router,
    private eRef: ElementRef
  ) {}

  @HostListener('document:click', ['$event'])
  clickout(event: Event) {
    if(!this.eRef.nativeElement.contains(event.target)) {
      this.openDropdownId = null;
    }
  }

  ngOnInit(): void {
    this.loadFDs();
  }

  // ==============================
  // PAGINATION
  // ==============================

  get paginatedList(): FDLanding[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.fdList.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.fdList.length / this.pageSize) || 1;
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

  onPageSizeChange(): void {
    this.currentPage = 1;
  }

  // ==============================
  // LOAD FD LANDING DATA
  // ==============================

  loadFDs(): void {
    // 1. Instant Load from Cache (Microseconds)
    const cachedData = sessionStorage.getItem('FINTRUST_FD_LANDING_CACHE');
    if (cachedData) {
      const parsedData = JSON.parse(cachedData);
      this.fdList = parsedData.sort((a: any, b: any) => b.fdId - a.fdId);
      // Do not show loading spinner if we have cached data
    } else {
      this.loading = true;
    }

    // 2. Background Fetch to stay up-to-date (Stale-While-Revalidate)
    this.fdService
      .getLandingData()
      .subscribe({
        next: (data) => {
          // Update cache with fresh data
          sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(data));
          
          // Update UI
          this.fdList = data.sort((a, b) => b.fdId - a.fdId);
          this.loading = false;
        },
        error: (error) => {
          console.error('Error fetching FDs:', error);
          this.loading = false;
        }
      });
  }

  // ==============================
  // ACTIONS
  // ==============================

  toggleDropdown(id: number, event: Event): void {
    event.stopPropagation();
    if (this.openDropdownId === id) {
      this.openDropdownId = null;
    } else {
      this.openDropdownId = id;
    }
  }

  handleAction(action: string, fd: FDLanding): void {
    this.openDropdownId = null;

    switch (action) {
      case 'view':
        this.openFD(fd);
        break;

      case 'edit':
        this.editFD(fd.fdId);
        break;

      case 'delete':
        this.deleteFD(fd);
        break;

      case 'cashflow':
        this.cashFlow(fd);
        break;
    }
  }
  // ADD FD
  // ==============================

  addFD(): void {
    this.router.navigate(['/fd-detail']);
  }

  // ==============================
  // OPEN FD (FAST VIEW)
  // ==============================

  openFD(fd: FDLanding): void {
    if (this.expandedFdId === fd.fdId) {
      this.expandedFdId = null;
    } else {
      this.expandedFdId = fd.fdId;
    }
  }

  // ==============================
  // EDIT
  // ==============================

  editFD(fdId: number): void {
    console.log('Edit FD:', fdId);
    this.router.navigate(['/fd-detail', fdId]);
  }

  // ==============================
  // CASH FLOW
  // ==============================

  cashFlow(fd: FDLanding): void {
    this.router.navigate(['/fd-detail', fd.fdId], { queryParams: { tab: 'cashflow' } });
  }

  // ==============================
  // DELETE
  // ==============================

  deleteFD(fd: FDLanding): void {
    const confirmed = confirm(`Are you sure you want to delete ${fd.fdReferenceNo}?`);

    if (!confirmed) {
      return;
    }

    this.fdService
      .delete(fd.fdId)
      .subscribe({
        next: () => {
          alert('FD deleted successfully');
          this.loadFDs();
        },
        error: (error) => {
          console.error('Delete failed', error);
          alert('Unable to delete FD');
        }
      });
  }


}
