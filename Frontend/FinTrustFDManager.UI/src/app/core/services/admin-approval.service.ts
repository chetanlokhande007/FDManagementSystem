import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { FDLanding } from './fd-identification.service';

export interface AdminDashboardSummary {
  totalPending: number;
  totalApproved: number;
  totalRejected: number;
  totalDraft: number;
  totalSubmitted: number;
  totalActive: number;
  approvedToday: number;
  rejectedToday: number;
  criticalPending: number;
}

export interface AdminApprovalDetail {
  fdId: number;
  fdReferenceNo: string;
  entityId: number;
  entityName: string;
  counterpartyId: number;
  counterPartyName: string;
  currencyCode: string;
  principalAmount: number;
  startDate: string;
  endDate: string;
  settlementDate?: string;
  status: string;
  remarks?: string;
  createdByUserId?: number;
  createdByName: string;
  createdDate: string;
  modifiedByUserId?: number;
  modifiedByName: string;
  modifiedDate?: string;
  interest?: {
    fdInterestId: number;
    interestRateType: string;
    interestRate: number;
    benchmarkId?: number;
    benchmarkName?: string;
    benchmarkRate?: number;
    margin?: number;
    interestFrequency: string;
    compoundingFrequency?: string;
    isCompounding: boolean;
    calculationBasis: string;
    paymentConvention?: string;
    createdDate: string;
  };
  cashFlows: Array<{
    cashFlowId: number;
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
    referenceNo?: string;
    createdDate: string;
  }>;
  totalPrincipal: number;
  totalInterest: number;
  maturityAmount: number;
  totalTenorDays: number;
  approvalHistory: Array<{
    id: number;
    action: string;
    fromStatus?: string;
    toStatus?: string;
    actionByUserId: number;
    actionByName: string;
    actionDate: string;
    comments?: string;
    oldValues?: string;
    newValues?: string;
  }>;
}

@Injectable({
  providedIn: 'root'
})
export class AdminApprovalService {

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

  /**
   * Get admin dashboard summary with counts across all statuses.
   */
  getDashboardSummary(): Observable<AdminDashboardSummary> {
    return this.http.get<AdminDashboardSummary>(`${this.apiUrl}/admin/dashboard`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Get approval list for admin, optionally filtered by status.
   * @param statusFilter Optional status filter (e.g. 'PENDING_APPROVAL', 'APPROVED', etc.)
   */
  getApprovalList(statusFilter?: string): Observable<FDLanding[]> {
    let url = `${this.apiUrl}/admin/approvals`;
    if (statusFilter) {
      url += `?status=${encodeURIComponent(statusFilter)}`;
    }
    return this.http.get<FDLanding[]>(url).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Get comprehensive approval detail for a specific FD.
   * Includes FD identification, interest config, cash flows, and approval history.
   */
  getApprovalDetail(fdId: number): Observable<AdminApprovalDetail> {
    return this.http.get<AdminApprovalDetail>(`${this.apiUrl}/admin/approvals/${fdId}`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Approve a pending FD record.
   * @param fdId FD identification ID
   * @param comments Optional approval comments
   */
  approve(fdId: number, comments?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${fdId}/approve`, { comments }
    ).pipe(catchError(this.handleError));
  }

  /**
   * Reject a pending FD record.
   * @param fdId FD identification ID
   * @param comments Required rejection reason (minimum 5 characters)
   */
  reject(fdId: number, comments: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${fdId}/reject`, { comments }
    ).pipe(catchError(this.handleError));
  }
}
