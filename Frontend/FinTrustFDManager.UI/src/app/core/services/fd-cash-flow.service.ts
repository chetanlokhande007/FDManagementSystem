import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FDCashFlow {
  cashFlowId: number;
  fdId: number;
  event: string;
  startDate: string;
  endDate: string;
  days: number;
  interestRate: number;
  openingBalance: number;
  interestAmount: number;
  closingBalance: number;
  cashFlowAmount: number;
  direction: string;
  currencyCode: string;
  status: string;
  referenceNo: string;
  createdDate?: string;
}

export interface FDCashFlowSummary {
  fdId: number;
  principalAmount: number;
  totalInterest: number;
  maturityAmount: number;
  cashFlows: FDCashFlow[];
}

@Injectable({
  providedIn: 'root'
})
export class FDCashFlowService {
  private apiUrl = `${environment.apiUrl}/FDCashFlow`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<FDCashFlow[]> {
    return this.http.get<FDCashFlow[]>(this.apiUrl);
  }

  create(data: FDCashFlow): Observable<FDCashFlow> {
    return this.http.post<FDCashFlow>(this.apiUrl, data);
  }

  getById(id: number): Observable<FDCashFlow> {
    return this.http.get<FDCashFlow>(`${this.apiUrl}/${id}`);
  }

  update(id: number, data: FDCashFlow): Observable<FDCashFlow> {
    return this.http.put<FDCashFlow>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getByFdId(fdId: number): Observable<FDCashFlowSummary> {
    return this.http.get<FDCashFlowSummary>(`${this.apiUrl}/fd/${fdId}`);
  }
}
