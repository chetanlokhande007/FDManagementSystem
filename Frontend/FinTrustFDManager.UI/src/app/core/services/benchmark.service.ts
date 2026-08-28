import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Benchmark {
  benchmarkId: number;
  benchmarkName: string;
  description?: string;
  currentRate: number;
  rateUnit?: string;
  isActive: boolean;
  createdDate?: string;
}

export interface BenchmarkRateHistory {
  benchmarkRateHistoryId: number;
  benchmarkId: number;
  rate: number;
  effectiveFrom: string;
  effectiveTo?: string;
  createdDate?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BenchmarkService {
  private apiUrl = `${environment.apiUrl}/Benchmark`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<Benchmark[]> {
    return this.http.get<Benchmark[]>(this.apiUrl);
  }

  getById(id: number): Observable<Benchmark> {
    return this.http.get<Benchmark>(`${this.apiUrl}/${id}`);
  }

  create(benchmark: Partial<Benchmark>): Observable<Benchmark> {
    return this.http.post<Benchmark>(this.apiUrl, benchmark);
  }

  update(id: number, benchmark: Partial<Benchmark>): Observable<Benchmark> {
    return this.http.put<Benchmark>(`${this.apiUrl}/${id}`, benchmark);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}

@Injectable({
  providedIn: 'root'
})
export class BenchmarkRateHistoryService {
  private apiUrl = `${environment.apiUrl}/BenchmarkRateHistory`;

  constructor(private http: HttpClient) { }

  getByBenchmarkId(benchmarkId: number): Observable<BenchmarkRateHistory[]> {
    return this.http.get<BenchmarkRateHistory[]>(`${this.apiUrl}/benchmark/${benchmarkId}`);
  }

  create(history: Partial<BenchmarkRateHistory>): Observable<BenchmarkRateHistory> {
    return this.http.post<BenchmarkRateHistory>(this.apiUrl, history);
  }

  update(id: number, history: Partial<BenchmarkRateHistory>): Observable<BenchmarkRateHistory> {
    return this.http.put<BenchmarkRateHistory>(`${this.apiUrl}/${id}`, history);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
