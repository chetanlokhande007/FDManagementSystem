import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CounterParty {
  counterPartyId: number;
  counterPartyCode: string;
  counterPartyName: string;
  countryId: number;
  countryName: string;
  description?: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CounterPartyService {

  private apiUrl = 'http://localhost:5075/api/CounterParty';

  constructor(private http: HttpClient) {}

  getAll(): Observable<CounterParty[]> {
    return this.http.get<CounterParty[]>(this.apiUrl);
  }
}
