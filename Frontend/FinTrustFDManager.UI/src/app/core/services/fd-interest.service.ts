import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FDInterest {
  fdInterestId: number;
  fdId: number;
  interestRateType: string;
  interestRate: number;
  benchmarkName: string;
  benchmarkRate: number;
  margin: number;
  interestFrequency: string;
  compoundingFrequency: string;
  isCompounding: boolean;
  calculationBasis: string;
  calendarCode: string;
  paymentConvention: string;
  firstInterestDate: string;
  firstCompoundingDate: string;
  tdsApplicable: boolean;
  tdsRate: number;
  createdDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class FDInterestService {
  private apiUrl = 'http://localhost:5075/api/FDInterest';

  constructor(private http: HttpClient) {}

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
