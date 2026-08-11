import { Component, signal, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('FinTrustFDManager.UI');
  private http = inject(HttpClient);
  apiStatus = signal<string>('Checking API connection...');

  ngOnInit() {
    // Basic endpoint to test connection (adjust if a specific one exists)
    // Here we just test if a common base endpoint or a dummy one responds without CORS errors.
    this.http.get(`${environment.apiUrl}/weatherforecast`, { responseType: 'text' }).subscribe({
      next: (res) => this.apiStatus.set('Connected to API successfully!'),
      error: (err) => {
        if (err.status === 404) {
           // 404 means it reached the server but route isn't there, so CORS passed!
           this.apiStatus.set('Connected to API (CORS successful, but route not found).');
        } else {
           this.apiStatus.set(`Failed to connect to API: ${err.message}`);
        }
      }
    });
  }
}
