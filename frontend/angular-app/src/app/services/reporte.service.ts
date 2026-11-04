import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TipoReporte { type: string; label: string; filters: string[]; }

export interface ReporteMeta {
  reportId: string;
  type: string;
  typeLabel: string;
  generatedBy: string;
  generatedByName?: string | null;
  generatedAt: string;
  filters: Record<string, any>;
  status: 'PENDING' | 'PROCESSING' | 'COMPLETED' | 'FAILED';
  fileName?: string | null;
  downloadedAt?: string | null;
  downloadCount: number;
  error?: string | null;
  rowCount?: number | null;
}

export interface HistorialReportes { items: ReporteMeta[]; total: number; page: number; size: number; }

// Los PDF se generan en el Report Service (backend); el frontend solo los solicita y descarga.
@Injectable({ providedIn: 'root' })
export class ReporteService {
  private apiUrl = '/api/reports';

  constructor(private http: HttpClient) {}

  tipos(): Observable<TipoReporte[]> {
    return this.http.get<TipoReporte[]>(`${this.apiUrl}/types`);
  }

  generar(type: string, filters: Record<string, any>): Observable<ReporteMeta> {
    return this.http.post<ReporteMeta>(this.apiUrl, { type, filters });
  }

  historial(page = 1, size = 10): Observable<HistorialReportes> {
    const params = new HttpParams().set('page', page).set('size', size);
    return this.http.get<HistorialReportes>(this.apiUrl, { params });
  }

  descargar(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/download`, { responseType: 'blob' });
  }
}
