import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  email?: string;
  role?: string;
  name?: string;
  userId?: number;
}

export interface RegisterDto {
  fullName: string;
  email: string;
  mobileNo: string;
  password?: string;
  roleId: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private apiUrl = `${environment.apiUrl}/Auth`;

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/login`,
      request
    );
  }

  register(data: RegisterDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, data);
  }

  setSession(res: LoginResponse) {
    if (res.token) {
      localStorage.setItem('token', res.token);
    }
    if (res.role) {
      localStorage.setItem('role', res.role);
    }
    // Store userName if available (backend returns it in LoginResponse)
    if ((res as any).name) {
      localStorage.setItem('userName', (res as any).name);
    }
  }
}