import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface FDLanding {
  fdId: number;
  fdReferenceNo?: string;
  entityId?: number;
  entityName?: string;
  counterpartyId?: number;
  counterPartyName?: string;
  currencyCode?: string;
  principalAmount?: number;
  startDate?: string;
  endDate?: string;
  settlementDate?: string;
  status?: string;
  interestRate?: number;
  interestRateType?: string;
  interestFrequency?: string;
  compoundingFrequency?: string;
  calculationBasis?: string;
  totalPrincipal?: number;
  totalGrossInterest?: number;
  totalTds?: number;
  totalNetInterest?: number;
  totalAmount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class FDIdentificationService {

  private apiUrl = `${environment.apiUrl}/FDIdentification`;

  constructor(private http: HttpClient) {}

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'An error occurred';
    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else {
      errorMessage = error.error?.message || `Error Code: ${error.status}\nMessage: ${error.message}`;
    }
    console.error('API Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }

  getAll(): Observable<FDLanding[]> {
    return this.http.get<FDLanding[]>(this.apiUrl).pipe(
      catchError(this.handleError)
    );
  }

  getLandingData(): Observable<FDLanding[]> {
    return this.http.get<FDLanding[]>(`${this.apiUrl}/landing`).pipe(
      catchError(this.handleError)
    );
  }

  getById(id: number): Observable<FDLanding> {
    return this.http.get<FDLanding>(`${this.apiUrl}/${id}`).pipe(
      catchError(this.handleError)
    );
  }

  create(data: Partial<FDLanding>): Observable<FDLanding> {
    return this.http.post<FDLanding>(this.apiUrl, data).pipe(
      catchError(this.handleError)
    );
  }

  update(id: number, data: Partial<FDLanding>): Observable<FDLanding> {
    return this.http.put<FDLanding>(`${this.apiUrl}/${id}`, data).pipe(
      catchError(this.handleError)
    );
  }

  delete(id: number): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.apiUrl}/${id}`).pipe(
      catchError(this.handleError)
    );
  }

  submit(id: number): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/${id}/submit`, {}).pipe(
      catchError(this.handleError)
    );
  }

  approve(id: number, comments?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/${id}/approve`, { comments }).pipe(
      catchError(this.handleError)
    );
  }

  reject(id: number, comments: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/${id}/reject`, { comments }).pipe(
      catchError(this.handleError)
    );
  }

  getApprovalHistory(id: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${id}/approval-history`).pipe(
      catchError(this.handleError)
    );
  }

  // ── Amendment methods ──

  requestAmendment(fdId: number, data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${fdId}/amendments`, data).pipe(
      catchError(this.handleError)
    );
  }

  getAmendments(fdId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${fdId}/amendments`).pipe(
      catchError(this.handleError)
    );
  }

  approveAmendment(fdId: number, amendmentId: number, comments?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${fdId}/amendments/${amendmentId}/approve`, { comments }
    ).pipe(catchError(this.handleError));
  }

  rejectAmendment(fdId: number, amendmentId: number, comments: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${fdId}/amendments/${amendmentId}/reject`, { comments }
    ).pipe(catchError(this.handleError));
  }

  /** @deprecated Use submit/approve/reject instead */
  changeStatus(id: number, status: string): Observable<{ success: boolean; message: string }> {
    const headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    return this.http.patch<{ success: boolean; message: string }>(`${this.apiUrl}/${id}/status`, JSON.stringify(status), { headers }).pipe(
      catchError(this.handleError)
    );
  }
}
