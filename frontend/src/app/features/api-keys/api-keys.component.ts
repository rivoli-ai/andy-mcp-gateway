import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiKeysService } from '../../core/services/api-keys.service';
import { ApiKey, CreatedApiKey } from '../../core/models/api-key.model';
import { NotificationService } from '../../shared/components/notification/notification.service';

interface RevealState {
  /** The decrypted plaintext, or null when masked. Cleared on hide. */
  plaintext: string | null;
  /** True while a /reveal request is in flight. */
  loading: boolean;
}

/**
 * Application-level API keys management page. Keys are global to the gateway and
 * authenticate MCP transport routes via the X-MCP-Key header — they DO NOT replace
 * the user's login. The list endpoint never carries plaintext; reveal hits a
 * dedicated endpoint that decrypts the stored ciphertext on demand.
 */
@Component({
  selector: 'app-api-keys',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './api-keys.component.html',
  styleUrl: './api-keys.component.scss'
})
export class ApiKeysComponent implements OnInit {
  readonly keys = signal<ApiKey[]>([]);
  readonly loading = signal(false);
  readonly creating = signal(false);

  // Create-key modal state
  readonly showCreateModal = signal(false);
  newKeyName = '';

  // Display modal for the freshly created plaintext (shown exactly once)
  readonly justCreated = signal<CreatedApiKey | null>(null);

  // Per-row reveal state, keyed by api-key id
  readonly revealStates = signal<Record<string, RevealState>>({});

  // Per-row revoke confirmation flag, keyed by api-key id
  readonly confirmRevokeId = signal<string | null>(null);

  /** Slot ids that just got copied — drives the temporary "copied" tick on the icon button. */
  readonly copiedSlots = signal<Record<string, true>>({});
  private readonly copiedTimers = new Map<string, ReturnType<typeof setTimeout>>();

  readonly hasKeys = computed(() => this.keys().length > 0);
  readonly activeCount = computed(() => this.keys().filter(k => k.isActive).length);

  constructor(
    private readonly apiKeys: ApiKeysService,
    private readonly notify: NotificationService
  ) {}

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.apiKeys.list().subscribe({
      next: (rows) => {
        this.keys.set(rows);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.notify.error('Failed to load API keys', err?.message ?? 'Unknown error');
      }
    });
  }

  openCreateModal(): void {
    this.newKeyName = '';
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    this.showCreateModal.set(false);
    this.newKeyName = '';
  }

  submitCreate(): void {
    const name = this.newKeyName.trim();
    if (!name) {
      this.notify.warning('Name required', 'Give the key a recognizable name.');
      return;
    }
    this.creating.set(true);
    this.apiKeys.create({ name }).subscribe({
      next: (created) => {
        this.creating.set(false);
        this.showCreateModal.set(false);
        this.justCreated.set(created);
        this.refresh();
      },
      error: (err) => {
        this.creating.set(false);
        this.notify.error('Could not create key', err?.message ?? 'Unknown error');
      }
    });
  }

  dismissJustCreated(): void {
    this.justCreated.set(null);
  }

  toggleReveal(key: ApiKey): void {
    const current = this.revealStates()[key.id];
    if (current?.plaintext) {
      // hide
      this.revealStates.update(s => ({ ...s, [key.id]: { plaintext: null, loading: false } }));
      return;
    }

    this.revealStates.update(s => ({ ...s, [key.id]: { plaintext: null, loading: true } }));
    this.apiKeys.reveal(key.id).subscribe({
      next: (revealed) => {
        this.revealStates.update(s => ({
          ...s,
          [key.id]: { plaintext: revealed.key, loading: false }
        }));
      },
      error: (err) => {
        this.revealStates.update(s => ({ ...s, [key.id]: { plaintext: null, loading: false } }));
        this.notify.error('Could not reveal key', err?.message ?? 'Unknown error');
      }
    });
  }

  isRevealLoading(id: string): boolean {
    return !!this.revealStates()[id]?.loading;
  }

  revealedValue(id: string): string | null {
    return this.revealStates()[id]?.plaintext ?? null;
  }

  promptRevoke(id: string): void {
    this.confirmRevokeId.set(id);
  }

  cancelRevoke(): void {
    this.confirmRevokeId.set(null);
  }

  confirmRevoke(): void {
    const id = this.confirmRevokeId();
    if (!id) return;
    this.confirmRevokeId.set(null);
    this.apiKeys.revoke(id).subscribe({
      next: () => {
        this.notify.success('API key revoked', 'The key can no longer authenticate.');
        this.refresh();
      },
      error: (err) => this.notify.error('Revoke failed', err?.message ?? 'Unknown error')
    });
  }

  async copyToClipboard(value: string, slotId: string = 'default'): Promise<void> {
    if (!value) return;

    // Modern Async Clipboard API requires a secure context (https or localhost). When
    // unavailable — http or older Safari — we fall back to the legacy execCommand path.
    const hasAsync = typeof navigator !== 'undefined'
      && !!navigator.clipboard
      && window.isSecureContext;

    try {
      if (hasAsync) {
        await navigator.clipboard.writeText(value);
      } else {
        this.legacyCopy(value);
      }
      this.flagCopied(slotId);
    } catch (err) {
      console.error('[api-keys] copy failed', err);
    }
  }

  /** Marks <paramref name="slotId"/> as recently-copied for ~1.5s, then clears it. */
  private flagCopied(slotId: string): void {
    this.copiedSlots.update(s => ({ ...s, [slotId]: true }));

    // Reset any in-flight timer for this slot so successive clicks restart the highlight.
    const previous = this.copiedTimers.get(slotId);
    if (previous) clearTimeout(previous);

    const handle = setTimeout(() => {
      this.copiedSlots.update(s => {
        const next = { ...s };
        delete next[slotId];
        return next;
      });
      this.copiedTimers.delete(slotId);
    }, 1500);
    this.copiedTimers.set(slotId, handle);
  }

  isCopied(slotId: string): boolean {
    return !!this.copiedSlots()[slotId];
  }

  /** Fallback when the async Clipboard API isn't available. Returns true on success. */
  private legacyCopy(value: string): boolean {
    const ta = document.createElement('textarea');
    ta.value = value;
    ta.setAttribute('readonly', '');
    ta.style.position = 'fixed';
    ta.style.top = '-1000px';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    let ok = false;
    try {
      ok = document.execCommand('copy');
    } catch {
      ok = false;
    }
    document.body.removeChild(ta);
    return ok;
  }

  maskFor(prefix: string): string {
    return `${prefix}${'•'.repeat(24)}`;
  }

  trackById(_: number, key: ApiKey): string {
    return key.id;
  }
}
