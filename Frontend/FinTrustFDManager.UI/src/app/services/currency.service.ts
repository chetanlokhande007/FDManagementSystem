import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Currency {
  currencyId: number;
  currencyName: string;
  currencyCode: string;
  symbol: string;
  description: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CurrencyService {

  private apiUrl = `${environment.apiUrl}/Currency`;

  constructor(
    private http: HttpClient
  ) {}

  // GET ALL
  getCurrencies(): Observable<Currency[]> {
    return this.http.get<Currency[]>(this.apiUrl);
  }

  // GET BY ID
  getCurrencyById(id: number): Observable<Currency> {
    return this.http.get<Currency>(
      `${this.apiUrl}/${id}`
    );
  }

  // CREATE
  createCurrency(request: {
    currencyName: string;
    currencyCode: string;
    symbol: string;
    description: string;
    isActive: boolean;
  }): Observable<Currency> {

    return this.http.post<Currency>(
      this.apiUrl,
      request
    );
  }

  // UPDATE
  updateCurrency(
    id: number,
    request: {
      currencyName: string;
      currencyCode: string;
      symbol: string;
      description: string;
      isActive: boolean;
    }
  ): Observable<Currency> {

    return this.http.put<Currency>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  // DELETE
  deleteCurrency(id: number): Observable<void> {

    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}