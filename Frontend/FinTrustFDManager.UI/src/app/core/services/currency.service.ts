import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Currency {
  currencyId: number;
  currencyCode: string;
  currencyName: string;
  symbol?: string;
  description?: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CurrencyService {

  private apiUrl = `${environment.apiUrl}/Currency`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Currency[]> {
    return this.http.get<Currency[]>(this.apiUrl);
  }
}
