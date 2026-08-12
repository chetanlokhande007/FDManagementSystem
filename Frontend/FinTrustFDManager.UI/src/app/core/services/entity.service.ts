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
  status: number;
}

export interface CreateEntityDto {
  entityCode: string;
  entityName: string;
  countryId: number;
}

export interface UpdateEntityDto {
  entityCode: string;
  entityName: string;
  countryId: number;
}

@Injectable({
  providedIn: 'root'
})
export class EntityService {
  private apiUrl = `${environment.apiUrl}/Entity`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<EntityDto[]> {
    return this.http.get<EntityDto[]>(
      'http://127.0.0.1:5075/api/Entity'
    );
  }

  getById(id: number): Observable<EntityDto> {
    return this.http.get<EntityDto>(`${this.apiUrl}/${id}`);
  }

  create(data: CreateEntityDto): Observable<EntityDto> {
    return this.http.post<EntityDto>(this.apiUrl, data);
  }

  update(id: number, data: UpdateEntityDto): Observable<EntityDto> {
    return this.http.put<EntityDto>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  approve(id: number): Observable<EntityDto> {
    // To be implemented: need an approve endpoint or we use update with DTO (for now we assume update or specific endpoint)
    // For Phase 1 we will just use a specific method if backend implements it, or update it
    return this.http.put<EntityDto>(`${this.apiUrl}/approve/${id}`, {});
  }
  
  reject(id: number): Observable<EntityDto> {
    return this.http.put<EntityDto>(`${this.apiUrl}/reject/${id}`, {});
  }
}
