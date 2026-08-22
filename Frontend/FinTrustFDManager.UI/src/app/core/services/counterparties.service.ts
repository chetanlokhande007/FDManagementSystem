import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CounterParty {
  counterPartyId: number;
  counterPartyCode: string;
  counterPartyName: string;
  countryId: number;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CounterPartyService {

  private apiUrl = `${environment.apiUrl}/CounterParty`;

  constructor(private http: HttpClient) {}

  getCounterParties(): Observable<CounterParty[]> {
    return this.http.get<CounterParty[]>(this.apiUrl);
  }

  getCounterPartyById(
    id: number
  ): Observable<CounterParty> {

    return this.http.get<CounterParty>(
      `${this.apiUrl}/${id}`
    );
  }

  createCounterParty(
    counterParty: Partial<CounterParty>
  ): Observable<CounterParty> {

    return this.http.post<CounterParty>(
      this.apiUrl,
      counterParty
    );
  }

  updateCounterParty(
    id: number,
    counterParty: Partial<CounterParty>
  ): Observable<CounterParty> {

    return this.http.put<CounterParty>(
      `${this.apiUrl}/${id}`,
      counterParty
    );
  }

  deleteCounterParty(
    id: number
  ): Observable<void> {

    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}