export interface ApiKey {
  id: string;
  name: string;
  keyPrefix: string;
  createdBy?: string | null;
  createdAt: string;
  lastUsedAt?: string | null;
  revokedAt?: string | null;
  isActive: boolean;
}

export interface CreateApiKeyRequest {
  name: string;
}

export interface CreatedApiKey {
  metadata: ApiKey;
  /** Plaintext key — only ever returned once, at creation. */
  key: string;
}

export interface RevealedApiKey {
  id: string;
  /** Plaintext key — decrypted from the server's ciphertext on demand. */
  key: string;
}
