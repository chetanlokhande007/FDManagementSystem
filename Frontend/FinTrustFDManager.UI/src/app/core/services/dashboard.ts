import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ChartDataDto {
  label: string;
  value: number;
  count: number;
}

export interface FDUpcomingMaturityDto {
  fdId: number;
  fdReferenceNo: string;
  counterpartyName: string;
  principalAmount: number;
  maturityDate: string;
  maturityAmount: number;
  status: string;
}

export interface FDRecentDto {
  fdId: number;
  fdReferenceNo: string;
  startDate: string;
  principalAmount: number;
  interestRate: number;
  interestType: string;
}

export interface DashboardSummaryDto {
  activeFDCount: number;
  totalPrincipal: number;
  totalAccruedInterest: number;
  maturingThisMonthCount: number;
  maturingThisMonthValue: number;
  fdGrowthData: ChartDataDto[];
  portfolioDistributionData: ChartDataDto[];
  upcomingMaturities: FDUpcomingMaturityDto[];
  recentlyAddedFDs: FDRecentDto[];
}

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}/Dashboard`;

  constructor(private http: HttpClient) {}

  getSummary(): Observable<DashboardSummaryDto> {
    return this.http.get<DashboardSummaryDto>(`${this.apiUrl}/Summary`);
  }
}
