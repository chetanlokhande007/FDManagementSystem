import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent implements OnInit {
  @Input() isOpen = false;
  @Output() closeSidebar = new EventEmitter<void>();

  isMasterDataOpen = false;
  isInvestmentsOpen = true; // Open by default
  isAdminMasterDataOpen = false;

  userRole = '';
  isApprover = false;
  isAdmin = false;
  isCA = false;

  constructor(private router: Router, private authService: AuthService) {}

  ngOnInit() {
    this.userRole = localStorage.getItem('role') || '';
    this.isApprover = this.userRole === 'Approver';
    this.isAdmin = this.userRole === 'Admin';
    this.isCA = this.userRole === 'CA';

    this.checkActiveRoute(this.router.url);

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.checkActiveRoute(event.urlAfterRedirects);
    });
  }

  checkActiveRoute(url: string) {
    // Master Data (CA & Admin)
    if (
      url.includes('/entities') ||
      url.includes('/countries') ||
      url.includes('/currencies') ||
      url.includes('/counterparties') ||
      url.includes('/benchmarks')
    ) {
      this.isMasterDataOpen = true;
      this.isAdminMasterDataOpen = true;
    }

    // Investments
    if (
      url.includes('/fd') ||
      url.includes('/investments') ||
      url.includes('/fixed-deposit')
    ) {
      this.isInvestmentsOpen = true;
    }
  }

  toggleMasterData(): void {
    this.isMasterDataOpen = !this.isMasterDataOpen;
  }

  toggleAdminMasterData(): void {
    this.isAdminMasterDataOpen = !this.isAdminMasterDataOpen;
  }

  toggleInvestments(): void {
    this.isInvestmentsOpen = !this.isInvestmentsOpen;
  }

  isCoreDataOpen = false;
  isOperationsOpen = false;
  isReportingOpen = false;

  toggleCoreData(): void {
    this.isCoreDataOpen = !this.isCoreDataOpen;
  }

  toggleOperations(): void {
    this.isOperationsOpen = !this.isOperationsOpen;
  }

  toggleReporting(): void {
    this.isReportingOpen = !this.isReportingOpen;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}