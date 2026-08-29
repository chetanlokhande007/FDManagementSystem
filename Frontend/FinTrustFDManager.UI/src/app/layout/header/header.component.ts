import { Component, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit {
  @Output() toggleSidebar = new EventEmitter<void>();

  userName = 'User';
  userRole = '';

  ngOnInit(): void {
    this.userName = localStorage.getItem('userName') || 'User';
    this.userRole = localStorage.getItem('role') || '';
  }
}
