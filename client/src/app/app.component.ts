import { Component, HostListener, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterModule } from '@angular/router';
import { ThemeService } from './core/services/theme.service';
import { LoadingService } from './core/services/loading.service';
import { AuthService } from './core/services/auth.service';
import { NotificationComponent } from './shared/components/notification/notification.component';

const MOBILE_SIDEBAR_BP = 1024;

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterModule, NotificationComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  readonly auth = inject(AuthService);

  sidebarCollapsed = false;
  isNarrowViewport = false;
  isDarkMode = false;
  isLoading = false;

  constructor(
    private themeService: ThemeService,
    private loadingService: LoadingService
  ) {}

  ngOnInit(): void {
    this.syncViewportFlag();
    if (this.isNarrowViewport) {
      this.sidebarCollapsed = true;
    }

    this.themeService.isDarkMode$.subscribe(isDark => {
      this.isDarkMode = isDark;
    });

    this.loadingService.isLoading$.subscribe(loading => {
      this.isLoading = loading;
    });
  }

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  /** Mobile drawer backdrop */
  closeSidebarDrawer(): void {
    if (!this.isNarrowViewport || this.sidebarCollapsed) return;
    this.sidebarCollapsed = true;
  }

  @HostListener('window:resize')
  syncViewportFlag(): void {
    this.isNarrowViewport = typeof window !== 'undefined' && window.innerWidth <= MOBILE_SIDEBAR_BP;
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.auth.logout();
  }

  getUserInitials(): string {
    const u = this.auth.user();
    const label = u?.name || u?.email || '';
    if (!label) return 'U';
    const names = label.split(/\s+/).filter(Boolean);
    if (names.length >= 2) {
      return (names[0][0] + names[names.length - 1][0]).toUpperCase();
    }
    return (names[0][0] || 'U').toUpperCase();
  }

  getPageTitle(): string {
    // This would be dynamic based on the current route
    return 'MCP Gateway Management';
  }
}