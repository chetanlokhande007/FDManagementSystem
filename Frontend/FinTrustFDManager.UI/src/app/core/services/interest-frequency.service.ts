import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface InterestFrequency {
  interestFrequencyId: number;
  frequencyCode?: string;
  frequencyName?: string;
  description?: string;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class InterestFrequencyService {

  private apiUrl =
    'http://localhost:5075/api/InterestFrequency';

  constructor(private http: HttpClient) {}

  getAll(): Observable<InterestFrequency[]> {
    return this.http.get<InterestFrequency[]>(this.apiUrl);
  }
}
