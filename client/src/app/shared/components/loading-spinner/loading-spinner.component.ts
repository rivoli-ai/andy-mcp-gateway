import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="loading-container" *ngIf="isLoading">
      <div class="loading-spinner">
        <div class="loading"></div>
        <p class="loading-text" *ngIf="message">{{ message }}</p>
      </div>
    </div>
  `,
  styles: [`
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: var(--space-8);
      gap: var(--space-4);
    }
    
    .loading-spinner {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-4);
    }
    
    .loading {
      width: 40px;
      height: 40px;
      border: 3px solid var(--border-light);
      border-radius: 50%;
      border-top-color: var(--primary-600);
      animation: spin 1s ease-in-out infinite;
    }
    
    .loading-text {
      margin: 0;
      color: var(--text-secondary);
      font-size: var(--text-sm);
      font-weight: 500;
    }
    
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class LoadingSpinnerComponent {
  @Input() isLoading = false;
  @Input() message = '';
}