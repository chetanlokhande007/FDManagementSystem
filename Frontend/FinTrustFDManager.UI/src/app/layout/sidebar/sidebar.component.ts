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
  isCoreDataOpen = false;

  // ADD THIS
  isInvestmentsOpen = false;

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

    // Core Data
    if (
      url.includes('/investments') ||
      url.includes('/approvals') ||
      url.includes('/cash-flows') ||
      url.includes('/interest-frequency') ||
      url.includes('/day-count-convention')
    ) {
      this.isCoreDataOpen = true;
    }

    // ADD THIS
    // Automatically open Investments when FD page is active
    if (url.includes('/investments/fixed-deposit')) {
      this.isInvestmentsOpen = true;
    }
  }

  toggleMasterData(): void {
    this.isMasterDataOpen = !this.isMasterDataOpen;
  }

  toggleCoreData(): void {
    this.isCoreDataOpen = !this.isCoreDataOpen;
  }

  // ADD THIS
  toggleInvestments(): void {
    this.isInvestmentsOpen = !this.isInvestmentsOpen;
  }
}