import { Component, signal, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { SidebarComponent } from './layout/sidebar/sidebar.component';
import { HeaderComponent } from './layout/header/header.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, HeaderComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('FinTrustFDManager.UI');
  private http = inject(HttpClient);
  apiStatus = signal<string>('Checking API connection...');
  
  isSidebarOpen = false;

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  closeSidebar() {
    this.isSidebarOpen = false;
  }

  ngOnInit() {
    this.http.get(`${environment.apiUrl}/weatherforecast`, { responseType: 'text' }).subscribe({
      next: (res) => this.apiStatus.set('Connected to API successfully!'),
      error: (err) => {
        if (err.status === 404) {
           this.apiStatus.set('Connected to API (CORS successful, but route not found).');
        } else {
           this.apiStatus.set(`Failed to connect to API: ${err.message}`);
        }
      }
    });
  }
}
