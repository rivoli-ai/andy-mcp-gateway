import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="login-container">
      <div class="login-card">
        <h1>Welcome to MCP Gateway</h1>
        <p>Please sign in to continue.</p>
        <button class="login-button" (click)="login()" [disabled]="isLoading">
          {{ isLoading ? 'Signing in...' : 'Sign in with Microsoft' }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }
    .login-card {
      background: white;
      padding: 2rem;
      border-radius: 12px;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
      text-align: center;
      max-width: 400px;
      width: 100%;
    }
    h1 {
      color: #333;
      margin-bottom: 1rem;
      font-size: 1.8rem;
    }
    p {
      color: #666;
      margin-bottom: 2rem;
      line-height: 1.5;
    }
    .login-button {
      background: #0078d4;
      color: white;
      border: none;
      padding: 12px 24px;
      border-radius: 6px;
      font-size: 1rem;
      cursor: pointer;
      transition: background-color 0.2s;
      display: flex;
      align-items: center;
      gap: 8px;
      margin: 0 auto;
    }
    .login-button:hover:not(:disabled) {
      background: #106ebe;
    }
    .login-button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
  `]
})
export class LoginComponent {
  private authService = inject(AuthService);
  isLoading = false;

  login(): void {
    this.isLoading = true;
    this.authService.login();
    // Reset loading state after a short delay since redirect will happen
    setTimeout(() => {
      this.isLoading = false;
    }, 2000);
  }
}
