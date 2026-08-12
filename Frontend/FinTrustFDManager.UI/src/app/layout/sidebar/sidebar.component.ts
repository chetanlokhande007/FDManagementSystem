import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
  @Input() isOpen = false;
  @Output() closeSidebar = new EventEmitter<void>();

  isMasterDataOpen = false;
  isCoreDataOpen = false;

  toggleMasterData(): void {
    this.isMasterDataOpen = !this.isMasterDataOpen;
  }

  toggleCoreData(): void {
    this.isCoreDataOpen = !this.isCoreDataOpen;
  }
}
