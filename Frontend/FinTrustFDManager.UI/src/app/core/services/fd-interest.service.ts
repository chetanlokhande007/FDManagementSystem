import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FDInterest {
  fdInterestId: number;
  fdId: number;
  interestRateType: string;
  interestRate: number;
  benchmarkId?: number;
  benchmarkName?: string;
  benchmarkRate?: number;
  margin?: number;
  interestFrequencyId: number;
  compoundingFrequencyId?: number;
  isCompounding: boolean;
  dayCountConventionId: number;
  paymentConvention?: string;
  createdDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class FDInterestService {
  private apiUrl = `${environment.apiUrl}/FDInterest`;

  constructor(private http: HttpClient) { }

  getByFdId(fdId: number): Observable<FDInterest> {
    // Assuming backend has this endpoint as requested
    return this.http.get<FDInterest>(`${this.apiUrl}/fd/${fdId}`);
  }

  create(data: FDInterest): Observable<FDInterest> {
    return this.http.post<FDInterest>(this.apiUrl, data);
  }

  update(id: number, data: FDInterest): Observable<FDInterest> {
    return this.http.put<FDInterest>(`${this.apiUrl}/${id}`, data);
  }
}
