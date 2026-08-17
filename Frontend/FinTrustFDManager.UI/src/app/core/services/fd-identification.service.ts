import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FDLanding {
  fdId: number;
  fdReferenceNo?: string;
  entityName?: string;
  counterpartyName?: string;
  transactionCurrency?: string;
  transactionAmount?: number;
  startDate?: string;
  endDate?: string;
  status?: string;
  customerName?: string;

  entityId?: number;
  counterpartyId?: number;
  counterpartyType?: string;
  currencyCode?: string;
  principalAmount?: number;
  settlementDate?: string;
  bankAccountId?: number;
  remarks?: string;
  interestRate?: number;
  totalAmount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class FDIdentificationService {

  private apiUrl =
    'http://localhost:5075/api/FDIdentification';

  constructor(private http: HttpClient) {}

  getAll(): Observable<FDLanding[]> {
    return this.http.get<FDLanding[]>(this.apiUrl);
  }

  getLandingData(): Observable<FDLanding[]> {
    return this.http.get<FDLanding[]>(`${this.apiUrl}/landing`);
  }

  getById(id: number): Observable<FDLanding> {
    return this.http.get<FDLanding>(`${this.apiUrl}/${id}`);
  }

  create(data: Partial<FDLanding>): Observable<FDLanding> {
    return this.http.post<FDLanding>(this.apiUrl, data);
  }

  update(id: number, data: Partial<FDLanding>): Observable<FDLanding> {
    return this.http.put<FDLanding>(
      `${this.apiUrl}/${id}`,
      data
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
