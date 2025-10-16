import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterModule } from '@angular/router';
import { ThemeService } from './core/services/theme.service';
import { LoadingService } from './core/services/loading.service';
import { AuthService } from './core/services/auth.service';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ClickOutsideDirective } from './shared/directives/click-outside.directive';
import { NotificationComponent } from './shared/components/notification/notification.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterModule, ClickOutsideDirective, NotificationComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  sidebarCollapsed = false;
  isDarkMode = false;
  isLoading = false;
  isAuthenticated = false;
  currentUser: any = null;
  userMenuOpen = false;

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
    this.userMenuOpen = false;
    this.authService.logout();
  }

  toggleUserMenu(): void {
    this.userMenuOpen = !this.userMenuOpen;
  }

  closeUserMenu(): void {
    this.userMenuOpen = false;
  }

  getUserInitials(): string {
    if (!this.currentUser?.name) return 'U';
    const names = this.currentUser.name.split(' ');
    if (names.length >= 2) {
      return names[0][0] + names[names.length - 1][0];
    }
    return names[0][0] || 'U';
  }

  getPageTitle(): string {
    // This would be dynamic based on the current route
    return 'MCP Gateway Management';
  }
}