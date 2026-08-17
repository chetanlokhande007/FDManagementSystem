import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DayCountConvention {
  dayCountConventionId: number;
  conventionCode?: string;
  conventionName?: string;
  description?: string;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class DayCountConventionService {

  private apiUrl =
    'http://localhost:5075/api/DayCountConvention';

  constructor(private http: HttpClient) {}

  getAll(): Observable<DayCountConvention[]> {
    return this.http.get<DayCountConvention[]>(this.apiUrl);
  }
}
