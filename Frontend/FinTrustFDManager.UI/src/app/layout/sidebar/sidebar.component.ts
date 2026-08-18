import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

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

  constructor(private router: Router) {}

  ngOnInit() {
    this.checkActiveRoute(this.router.url);

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.checkActiveRoute(event.urlAfterRedirects);
    });
  }

  checkActiveRoute(url: string) {
    // Master Data
    if (
      url.includes('/entities') ||
      url.includes('/countries') ||
      url.includes('/currencies') ||
      url.includes('/counterparties')
    ) {
      this.isMasterDataOpen = true;
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
}