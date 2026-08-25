import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface InterestFrequency {
  id: number;
  frequencyName?: string;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class InterestFrequencyService {

  private apiUrl = `${environment.apiUrl}/InterestFrequency`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<InterestFrequency[]> {
    return this.http.get<InterestFrequency[]>(this.apiUrl);
  }
}
