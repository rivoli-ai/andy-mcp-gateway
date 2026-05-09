import { Inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { IApiService } from '../interfaces/api.interface';
import { APP_CONFIG, AppConfig } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class ApiService implements IApiService {
  private readonly baseUrl: string;

  constructor(
    private http: HttpClient,
    @Inject(APP_CONFIG) config: AppConfig
  ) {
    this.baseUrl = config.apiUrl;
  }

  get<T>(endpoint: string, params?: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}${endpoint}`, { params })
      .pipe(
        catchError(this.handleError)
      );
  }

  post<T>(endpoint: string, data?: any, headers?: HttpHeaders): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${endpoint}`, data, { headers })
      .pipe(
        catchError(this.handleError)
      );
  }

  put<T>(endpoint: string, data?: any, headers?: HttpHeaders): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${endpoint}`, data, { headers })
      .pipe(
        catchError(this.handleError)
      );
  }

  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${endpoint}`)
      .pipe(
        catchError(this.handleError)
      );
  }

  getBlob(endpoint: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}${endpoint}`, { 
      responseType: 'blob',
      observe: 'response'
    }).pipe(
      map(response => response.body as Blob),
      catchError(this.handleError)
    );
  }

  postFormData<T>(endpoint: string, formData: FormData): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${endpoint}`, formData)
      .pipe(
        catchError(this.handleError)
      );
  }

  private handleError(error: any): Observable<never> {
    let errorMessage = 'An error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else {
      // Server-side error
      errorMessage = error.error?.error || error.message || 'Server error';
    }
    
    console.error('API Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}






