import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { FDLanding } from './fd-identification.service';

export interface ApproverDashboardSummary {
  totalPending: number;
  criticalPending: number;
  approvedToday: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApprovalService {

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

  getPendingApprovals(): Observable<FDLanding[]> {
    return this.http.get<FDLanding[]>(`${this.apiUrl}/pending-approvals`).pipe(
      catchError(this.handleError)
    );
  }

  getDashboardSummary(): Observable<ApproverDashboardSummary> {
    return this.http.get<ApproverDashboardSummary>(`${this.apiUrl}/approver-dashboard`).pipe(
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
}
