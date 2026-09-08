import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { FDLanding } from './fd-identification.service';

export interface ApprovalHistoryEntry {
  id: number;
  fdId: number;
  action: string;
  fromStatus: string;
  toStatus: string;
  actionBy: number;
  actionDate: string;
  comments: string;
  oldValues?: string;
  newValues?: string;
}

@Injectable({ providedIn: 'root' })
export class ApproverService {
  private apiUrl = `${environment.apiUrl}/FDIdentification`;

  constructor(private http: HttpClient) {}

  /** Get approver dashboard summary: status counts */
  getApproverSummary(): Observable<Record<string, number>> {
    return this.http.get<Record<string, number>>(`${this.apiUrl}/approver/summary`);
  }

  /** Get FDs filtered by status */
  getFDsByStatus(status?: string): Observable<FDLanding[]> {
    const url = status
      ? `${this.apiUrl}/filtered?status=${encodeURIComponent(status)}`
      : `${this.apiUrl}/filtered`;
    return this.http.get<FDLanding[]>(url);
  }

  /** Approve an FD */
  approveFD(fdId: number, comments?: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${fdId}/approve`,
      { comments: comments || '' }
    );
  }

  /** Reject an FD */
  rejectFD(fdId: number, comments: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${fdId}/reject`,
      { comments }
    );
  }

  /** Return FD to creator / request changes */
  returnToCreator(fdId: number, comments: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/${fdId}/return-to-creator`,
      { comments }
    );
  }

  /** Get approval history for an FD */
  getApprovalHistory(fdId: number): Observable<ApprovalHistoryEntry[]> {
    return this.http.get<ApprovalHistoryEntry[]>(`${this.apiUrl}/${fdId}/approval-history`);
  }

  /** Get single FD by ID */
  getFDById(fdId: number): Observable<FDLanding> {
    return this.http.get<FDLanding>(`${this.apiUrl}/${fdId}`);
  }
}
