import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Country {
  countryId: number;
  countryCode: string;
  countryName: string;
  description?: string;
  isActive: boolean;
  createdDate?: string;
  modifiedDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CountryService {

  private apiUrl = `${environment.apiUrl}/Country`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Country[]> {
    return this.http.get<Country[]>(this.apiUrl);
  }

  getCountries(): Observable<Country[]> {
    return this.getAll();
  }

  getCountryById(id: number): Observable<Country> {
    return this.http.get<Country>(`${this.apiUrl}/${id}`);
  }

  createCountry(country: Partial<Country>): Observable<Country> {
    return this.http.post<Country>(this.apiUrl, country);
  }

  updateCountry(id: number, country: Partial<Country>): Observable<Country> {
    return this.http.put<Country>(`${this.apiUrl}/${id}`, country);
  }

  deleteCountry(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
