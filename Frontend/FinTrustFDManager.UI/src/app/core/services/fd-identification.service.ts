import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FDLanding {

  fdId: number;

  fdReferenceNo: string;

  entityId: number;

  counterpartyId: number;

  currencyCode: string;

  principalAmount: number;

  startDate: string;

  endDate: string;

  settlementDate: string;

  status: string;

  interestRate: number;

  interestRateType: string;

  interestFrequency: string;

  compoundingFrequency: string;

  calculationBasis: string;

  totalPrincipal: number;

  totalGrossInterest: number;

  totalTds: number;

  totalNetInterest: number;

  totalAmount: number;
}


@Injectable({
  providedIn: 'root'
})
export class FDIdentificationService {

  private apiUrl =
    'http://localhost:5075/api/FDIdentification';


  constructor(
    private http: HttpClient
  ) {}


  // ==============================
  // LANDING DATA
  // ==============================

  getLandingData(): Observable<FDLanding[]> {

    return this.http.get<FDLanding[]>(
      `${this.apiUrl}/landing`
    );

  }


  // ==============================
  // GET BY ID
  // ==============================

  getById(id: number): Observable<any> {

    return this.http.get<any>(
      `${this.apiUrl}/${id}`
    );

  }


  // ==============================
  // CREATE
  // ==============================

  create(data: any): Observable<any> {

    return this.http.post<any>(
      this.apiUrl,
      data
    );

  }


  // ==============================
  // UPDATE
  // ==============================

  update(
    id: number,
    data: any
  ): Observable<any> {

    return this.http.put<any>(
      `${this.apiUrl}/${id}`,
      data
    );

  }


  // ==============================
  // DELETE
  // ==============================

  delete(id: number): Observable<any> {

    return this.http.delete(
      `${this.apiUrl}/${id}`
    );

  }

}
