import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  ApiKey,
  CreateApiKeyRequest,
  CreatedApiKey,
  RevealedApiKey
} from '../models/api-key.model';

/**
 * Thin REST client for /api/api-keys. Plaintext keys cross the wire in two paths only:
 * - POST /api/api-keys — returned exactly once at creation.
 * - GET  /api/api-keys/{id}/reveal — decrypted from the server's ciphertext on demand.
 * The list endpoint never carries plaintext, only the prefix preview.
 */
@Injectable({ providedIn: 'root' })
export class ApiKeysService {
  private readonly endpoint = '/api/api-keys';

  constructor(private readonly api: ApiService) {}

  list(): Observable<ApiKey[]> {
    return this.api.get<ApiKey[]>(this.endpoint);
  }

  create(request: CreateApiKeyRequest): Observable<CreatedApiKey> {
    return this.api.post<CreatedApiKey>(this.endpoint, request);
  }

  revoke(id: string): Observable<void> {
    return this.api.delete<void>(`${this.endpoint}/${id}`);
  }

  reveal(id: string): Observable<RevealedApiKey> {
    return this.api.get<RevealedApiKey>(`${this.endpoint}/${id}/reveal`);
  }
}
