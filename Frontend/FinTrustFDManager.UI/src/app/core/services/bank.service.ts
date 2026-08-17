import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class BankService {
  private apiUrl = 'http://localhost:5075/api/Bank';

  constructor(private http: HttpClient) {}

  getBanks(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }
}
