import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Country {
  countryId: number;
  countryName: string;
  countryCode: string;
  description: string;
  isActive: boolean;
  createdDate?: string;
  modifiedDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CountryService {

  private apiUrl = 'http://localhost:5075/api/Country';

  constructor(private http: HttpClient) {}

  getCountries(): Observable<Country[]> {
    return this.http.get<Country[]>(this.apiUrl);
  }

  getCountryById(id: number): Observable<Country> {
    return this.http.get<Country>(`${this.apiUrl}/${id}`);
  }

  createCountry(country: Partial<Country>): Observable<Country> {
    return this.http.post<Country>(
      this.apiUrl,
      country
    );
  }

  updateCountry(
    id: number,
    country: Partial<Country>
  ): Observable<Country> {
    return this.http.put<Country>(
      `${this.apiUrl}/${id}`,
      country
    );
  }

  deleteCountry(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${id}`
    );
  }
}
