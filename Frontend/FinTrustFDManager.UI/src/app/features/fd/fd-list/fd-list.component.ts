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
  ) { }

  @HostListener('document:click', ['$event'])
  clickout(event: Event) {
    if (!this.eRef.nativeElement.contains(event.target)) {
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
        next: (data: FDLanding[]) => {
          // Update cache with fresh data
          sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(data));

          // Update UI
          this.fdList = data.sort((a, b) => b.fdId - a.fdId);
          this.loading = false;
        },
        error: (error: any) => {
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

      case 'submit':
        this.submitFD(fd);
        break;

      case 'approve':
        this.approveFD(fd);
        break;

      case 'reject':
        this.openRejectModal(fd);
        break;
    }
  }
  // ADD FD
  // ==============================

  addFD(): void {
    this.router.navigate(['/fd-detail']);
  }

  // ==============================
  // OPEN FD (FULL DETAIL)
  // ==============================

  openFD(fd: FDLanding): void {
    this.router.navigate(['/fd-detail', fd.fdId], { queryParams: { tab: 'general' } });
  }

  // ==============================
  // EDIT
  // ==============================

  editFD(fdId: number): void {
    console.log('Edit FD:', fdId);
    this.router.navigate(['/fd-detail', fdId], { queryParams: { tab: 'general', mode: 'edit' } });
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

    // Optimistic UI update: instantly remove from list
    const previousList = [...this.fdList];
    this.fdList = this.fdList.filter(f => f.fdId !== fd.fdId);
    sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(this.fdList));
    this.openDropdownId = null;

    this.fdService
      .delete(fd.fdId)
      .subscribe({
        next: () => {
          // Clear the FD detail cache for this FD
          sessionStorage.removeItem(`FINTRUST_FD_DETAIL_CACHE_${fd.fdId}`);
          // Silent success. Refresh data in background just to be safe.
          this.fdService.getLandingData().subscribe(data => {
            sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(data));
            this.fdList = data.sort((a: any, b: any) => b.fdId - a.fdId);
          });
        },
        error: (error: any) => {
          // Revert optimistic delete on error
          this.fdList = previousList;
          sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(this.fdList));

          console.error('Delete failed:', error);
          const errorMsg = error?.error?.message || error?.message || 'Unable to delete FD.';
          alert(`Delete Error: ${errorMsg}`);
        }
      });
  }

  // ==============================
  // SUBMIT FD FOR APPROVAL
  // ==============================

  submitFD(fd: FDLanding): void {
    const confirmed = confirm(`Submit ${fd.fdReferenceNo} for approval?`);
    if (!confirmed) return;

    const oldStatus = fd.status;
    fd.status = 'PENDING_APPROVAL';
    sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(this.fdList));

    this.fdService.submit(fd.fdId).subscribe({
      next: () => {
        this.fdService.getLandingData().subscribe(data => {
          sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(data));
          this.fdList = data.sort((a: any, b: any) => b.fdId - a.fdId);
        });
      },
      error: (error: any) => {
        fd.status = oldStatus;
        sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(this.fdList));
        console.error('Submit failed:', error);
        alert(`Submit Error: ${error?.error?.message || error?.message || 'Unable to submit FD.'}`);
      }
    });
  }

  // ==============================
  // APPROVE FD
  // ==============================

  approveFD(fd: FDLanding): void {
    const confirmed = confirm(`Approve ${fd.fdReferenceNo}?`);
    if (!confirmed) return;

    const oldStatus = fd.status;
    fd.status = 'APPROVED';
    sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(this.fdList));

    this.fdService.approve(fd.fdId).subscribe({
      next: () => {
        this.fdService.getLandingData().subscribe(data => {
          sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(data));
          this.fdList = data.sort((a: any, b: any) => b.fdId - a.fdId);
        });
      },
      error: (error: any) => {
        fd.status = oldStatus;
        sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(this.fdList));
        console.error('Approve failed:', error);
        alert(`Approve Error: ${error?.error?.message || error?.message || 'Unable to approve FD.'}`);
      }
    });
  }

  // ==============================
  // REJECT FD
  // ==============================

  showRejectModal = false;
  rejectFd: FDLanding | null = null;
  rejectComments = '';

  openRejectModal(fd: FDLanding): void {
    this.rejectFd = fd;
    this.rejectComments = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.rejectFd = null;
    this.rejectComments = '';
  }

  confirmReject(): void {
    if (!this.rejectFd) return;
    if (!this.rejectComments || this.rejectComments.length < 5) {
      alert('Rejection reason must be at least 5 characters.');
      return;
    }

    const fd = this.rejectFd;
    const oldStatus = fd.status;
    fd.status = 'REJECTED';
    sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(this.fdList));
    this.closeRejectModal();

    this.fdService.reject(fd.fdId, this.rejectComments).subscribe({
      next: () => {
        this.fdService.getLandingData().subscribe(data => {
          sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(data));
          this.fdList = data.sort((a: any, b: any) => b.fdId - a.fdId);
        });
      },
      error: (error: any) => {
        fd.status = oldStatus;
        sessionStorage.setItem('FINTRUST_FD_LANDING_CACHE', JSON.stringify(this.fdList));
        console.error('Reject failed:', error);
        alert(`Reject Error: ${error?.error?.message || error?.message || 'Unable to reject FD.'}`);
      }
    });
  }

}
