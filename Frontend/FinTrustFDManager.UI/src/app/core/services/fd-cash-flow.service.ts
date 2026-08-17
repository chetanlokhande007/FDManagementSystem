import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FDCashFlow {
  cashFlowId: number;
  fdId: number;
  cashFlowDate: string;
  cashFlowType: string;
  direction: string;
  days: number;
  openingBalance: number;
  closingBalance: number;
  principalAmount: number;
  grossInterest: number;
  tdsAmount: number;
  netInterest: number;
  totalAmount: number;
  currencyCode: string;
  status: string;
  referenceNo: string;
  createdDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class FDCashFlowService {
  private apiUrl = 'http://localhost:5075/api/FDCashFlow';

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

  getByFdId(fdId: number): Observable<FDCashFlow[]> {
    return this.http.get<FDCashFlow[]>(`${this.apiUrl}?fdId=${fdId}`);
  }
}
