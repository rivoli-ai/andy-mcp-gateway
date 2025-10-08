import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterModule } from '@angular/router';
import { ThemeService } from './core/services/theme.service';
import { LoadingService } from './core/services/loading.service';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterModule],
  template: `
    <!-- Login Page -->
    <div *ngIf="!isAuthenticated" class="login-wrapper">
      <router-outlet></router-outlet>
    </div>
    
    <!-- Main App -->
    <div *ngIf="isAuthenticated" class="app-container">
      <!-- Sidebar -->
      <aside class="sidebar" [class.sidebar-collapsed]="sidebarCollapsed">
        <div class="sidebar-header">
          <div class="logo">
            <div class="logo-icon">
              <svg width="32" height="32" viewBox="0 0 32 32" fill="none">
                <rect width="32" height="32" rx="8" fill="url(#gradient)"/>
                <path d="M8 12h16v2H8v-2zm0 4h16v2H8v-2zm0 4h12v2H8v-2z" fill="white"/>
                <defs>
                  <linearGradient id="gradient" x1="0%" y1="0%" x2="100%" y2="100%">
                    <stop offset="0%" style="stop-color:#3b82f6"/>
                    <stop offset="100%" style="stop-color:#1d4ed8"/>
                  </linearGradient>
                </defs>
              </svg>
            </div>
            <span class="logo-text" *ngIf="!sidebarCollapsed">MCP Gateway</span>
          </div>
          <button class="sidebar-toggle" (click)="toggleSidebar()">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 6h18M3 12h18M3 18h18"/>
            </svg>
          </button>
        </div>
        
        <nav class="sidebar-nav">
          <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="3" width="7" height="7"/>
              <rect x="14" y="3" width="7" height="7"/>
              <rect x="14" y="14" width="7" height="7"/>
              <rect x="3" y="14" width="7" height="7"/>
            </svg>
            <span *ngIf="!sidebarCollapsed">Dashboard</span>
          </a>
          
          <a routerLink="/adapters" routerLinkActive="active" class="nav-item">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
            </svg>
            <span *ngIf="!sidebarCollapsed">Adapters</span>
          </a>
          
        </nav>
        
        <div class="sidebar-footer">
          <button class="theme-toggle" (click)="toggleTheme()" [title]="isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'">
            <svg *ngIf="!isDarkMode" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
            </svg>
            <svg *ngIf="isDarkMode" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="5"/>
              <line x1="12" y1="1" x2="12" y2="3"/>
              <line x1="12" y1="21" x2="12" y2="23"/>
              <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/>
              <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
              <line x1="1" y1="12" x2="3" y2="12"/>
              <line x1="21" y1="12" x2="23" y2="12"/>
              <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/>
              <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>
            </svg>
          </button>
        </div>
      </aside>
      
      <!-- Main Content -->
      <main class="main-content">
        <!-- Top Bar -->
        <header class="topbar">
          <div class="topbar-left">
            <button class="mobile-menu-toggle" (click)="toggleSidebar()">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M3 6h18M3 12h18M3 18h18"/>
              </svg>
            </button>
            <h1 class="page-title">{{ getPageTitle() }}</h1>
          </div>
          
          <div class="topbar-right">
            <div class="user-menu" *ngIf="isAuthenticated">
              <span class="user-name">{{ currentUser?.name || 'User' }}</span>
              <button class="user-avatar" (click)="logout()">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
                  <polyline points="16,17 21,12 16,7"/>
                  <line x1="21" y1="12" x2="9" y2="12"/>
                </svg>
              </button>
            </div>
          </div>
        </header>
        
        <!-- Page Content -->
        <div class="page-content">
          <div class="loading-overlay" *ngIf="isLoading">
            <div class="loading-spinner">
              <div class="loading"></div>
              <p>Loading...</p>
            </div>
          </div>
          
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  sidebarCollapsed = false;
  isDarkMode = false;
  isLoading = false;
  isAuthenticated = false;
  currentUser: any = null;

  private authService = inject(AuthService);

  constructor(
    private themeService: ThemeService,
    private loadingService: LoadingService
  ) {}

  ngOnInit(): void {
    this.themeService.isDarkMode$.subscribe(isDark => {
      this.isDarkMode = isDark;
    });
    
    this.loadingService.isLoading$.subscribe(loading => {
      this.isLoading = loading;
    });

    // Initialize authentication first
    this.authService.initialize().then(() => {
      console.log('Auth service initialized');
    }).catch(error => {
      console.error('Auth initialization failed:', error);
    });

    this.authService.isAuthenticated$.subscribe(isAuth => {
      this.isAuthenticated = isAuth;
      this.currentUser = this.authService.currentUser;
    });

    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.authService.logout();
  }

  getPageTitle(): string {
    // This would be dynamic based on the current route
    return 'MCP Gateway Management';
  }
}