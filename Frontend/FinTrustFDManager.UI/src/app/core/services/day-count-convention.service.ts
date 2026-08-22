import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

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

  private apiUrl = `${environment.apiUrl}/DayCountConvention`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<DayCountConvention[]> {
    return this.http.get<DayCountConvention[]>(this.apiUrl);
  }
}
