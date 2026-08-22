import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EntityDto {
  entityId: number;
  entityCode: string;
  entityName: string;
  countryId: number;
  countryName?: string;
  status?: number;
  isActive?: boolean;
}

export interface Entity {
  entityId: number;
  entityCode: string;
  entityName: string;
  countryId: number;
  countryName?: string;
  status?: number;
  isActive?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class EntityService {

  private apiUrl = `${environment.apiUrl}/Entity`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<EntityDto[]> {
    return this.http.get<EntityDto[]>(this.apiUrl);
  }

  getById(id: number): Observable<EntityDto> {
    return this.http.get<EntityDto>(`${this.apiUrl}/${id}`);
  }

  create(data: Partial<EntityDto>): Observable<EntityDto> {
    return this.http.post<EntityDto>(this.apiUrl, data);
  }

  update(id: number, data: Partial<EntityDto>): Observable<EntityDto> {
    return this.http.put<EntityDto>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
