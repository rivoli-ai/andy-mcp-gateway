import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { McpAdapterService } from '../../core/services/mcp-adapter.service';
import { AdapterList, AdapterStatus } from '../../core/models/mcp-adapter.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="dashboard">
      <!-- Header Section -->
      <div class="dashboard-header">
        <div class="header-content">
          <h1 class="dashboard-title">Dashboard</h1>
          <p class="dashboard-subtitle">Monitor and manage your MCP adapters</p>
        </div>
        <div class="header-actions">
          <button class="btn btn-primary" routerLink="/adapters/new">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 5v14M5 12h14"/>
            </svg>
            Add Adapter
          </button>
          <button class="btn btn-secondary" (click)="refreshData()" [disabled]="isLoading">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8M3 3v5h5M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16M21 21v-5h-5"/>
            </svg>
            Refresh
          </button>
        </div>
      </div>

      <!-- Stats Grid -->
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon stat-icon-primary">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
            </svg>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ adapterStats.total }}</div>
            <div class="stat-label">Total Adapters</div>
            <div class="stat-change positive">+2 this week</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon stat-icon-success">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
              <path d="M22 4L12 14.01l-3-3"/>
            </svg>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ adapterStats.healthy }}</div>
            <div class="stat-label">Healthy</div>
            <div class="stat-change positive">{{ getPercentage(adapterStats.healthy, adapterStats.total) }}% of total</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon stat-icon-error">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <line x1="15" y1="9" x2="9" y2="15"/>
              <line x1="9" y1="9" x2="15" y2="15"/>
            </svg>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ adapterStats.unhealthy }}</div>
            <div class="stat-label">Unhealthy</div>
            <div class="stat-change" [class.negative]="adapterStats.unhealthy > 0">{{ getPercentage(adapterStats.unhealthy, adapterStats.total) }}% of total</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon stat-icon-warning">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 9v4M12 17h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z"/>
            </svg>
          </div>
          <div class="stat-content">
            <div class="stat-value">{{ adapterStats.disabled }}</div>
            <div class="stat-label">Disabled</div>
            <div class="stat-change">{{ getPercentage(adapterStats.disabled, adapterStats.total) }}% of total</div>
          </div>
        </div>
      </div>

      <!-- Recent Adapters Section -->
      <div class="section" *ngIf="recentAdapters.length > 0">
        <div class="section-header">
          <h2 class="section-title">Recent Adapters</h2>
          <a routerLink="/adapters" class="section-link">View all</a>
        </div>
        
        <div class="adapters-grid">
          <div class="adapter-card" *ngFor="let adapter of recentAdapters">
            <div class="adapter-header">
              <div class="adapter-info">
                <h3 class="adapter-name">{{ adapter.name }}</h3>
                <p class="adapter-url">{{ adapter.url }}</p>
              </div>
              <div class="adapter-status">
                <span class="badge" [ngClass]="getStatusClass(adapter.status)">
                  {{ getStatusText(adapter.status) }}
                </span>
              </div>
            </div>
            
            <div class="adapter-body" *ngIf="adapter.description">
              <p class="adapter-description">{{ adapter.description }}</p>
            </div>
            
            <div class="adapter-footer">
              <div class="adapter-meta">
                <span class="adapter-time">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="12" r="10"/>
                    <path d="M12 6v6l4 2"/>
                  </svg>
                  {{ adapter.createdAt | date:'MMM d, y' }}
                </span>
                <span class="adapter-response" *ngIf="adapter.lastResponseTimeMs">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z"/>
                  </svg>
                  {{ adapter.lastResponseTimeMs }}ms
                </span>
              </div>
              <div class="adapter-actions">
                <button class="btn btn-ghost btn-sm" [routerLink]="['/adapters', adapter.id]">
                  View Details
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Quick Actions Section -->
      <div class="section">
        <div class="section-header">
          <h2 class="section-title">Quick Actions</h2>
        </div>
        
        <div class="actions-grid">
          <button class="action-card" routerLink="/adapters">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
              </svg>
            </div>
            <div class="action-content">
              <h3>Manage Adapters</h3>
              <p>View, edit, and configure your MCP adapters</p>
            </div>
          </button>
          
          
          <button class="action-card" (click)="checkAllHealth()" [disabled]="isLoading">
            <div class="action-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <path d="M22 4L12 14.01l-3-3"/>
              </svg>
            </div>
            <div class="action-content">
              <h3>Health Check</h3>
              <p>Check the status of all adapters</p>
            </div>
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading-overlay" *ngIf="isLoading">
        <div class="loading-spinner">
          <div class="loading"></div>
          <p>Loading dashboard data...</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard {
      max-width: 1200px;
      margin: 0 auto;
    }

    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: var(--space-8);
      gap: var(--space-6);
    }

    .header-content {
      flex: 1;
    }

    .dashboard-title {
      font-size: var(--text-4xl);
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: var(--space-2);
    }

    .dashboard-subtitle {
      font-size: var(--text-lg);
      color: var(--text-secondary);
      margin: 0;
    }

    .header-actions {
      display: flex;
      gap: var(--space-3);
      flex-shrink: 0;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: var(--space-6);
      margin-bottom: var(--space-8);
    }

    .stat-card {
      background: var(--bg-elevated);
      border: 1px solid var(--border-light);
      border-radius: var(--radius-xl);
      padding: var(--space-6);
      display: flex;
      align-items: center;
      gap: var(--space-4);
      transition: all var(--transition-normal);
      position: relative;
      overflow: hidden;

      &:hover {
        transform: translateY(-2px);
        box-shadow: var(--shadow-lg);
      }

      &::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 3px;
        background: linear-gradient(90deg, var(--primary-500), var(--primary-600));
      }
    }

    .stat-icon {
      width: 48px;
      height: 48px;
      border-radius: var(--radius-xl);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;

      &.stat-icon-primary {
        background: var(--primary-50);
        color: var(--primary-600);
      }

      &.stat-icon-success {
        background: var(--success-50);
        color: var(--success-600);
      }

      &.stat-icon-error {
        background: var(--error-50);
        color: var(--error-600);
      }

      &.stat-icon-warning {
        background: var(--warning-50);
        color: var(--warning-600);
      }
    }

    .stat-content {
      flex: 1;
    }

    .stat-value {
      font-size: var(--text-3xl);
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
      margin-bottom: var(--space-1);
    }

    .stat-label {
      font-size: var(--text-sm);
      color: var(--text-secondary);
      margin-bottom: var(--space-1);
    }

    .stat-change {
      font-size: var(--text-xs);
      color: var(--text-tertiary);

      &.positive {
        color: var(--success-600);
      }

      &.negative {
        color: var(--error-600);
      }
    }

    .section {
      margin-bottom: var(--space-8);
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: var(--space-6);
    }

    .section-title {
      font-size: var(--text-2xl);
      font-weight: 600;
      color: var(--text-primary);
      margin: 0;
    }

    .section-link {
      color: var(--primary-600);
      text-decoration: none;
      font-size: var(--text-sm);
      font-weight: 500;
      transition: color var(--transition-fast);

      &:hover {
        color: var(--primary-700);
        text-decoration: underline;
      }
    }

    .adapters-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: var(--space-6);
    }

    .adapter-card {
      background: var(--bg-elevated);
      border: 1px solid var(--border-light);
      border-radius: var(--radius-xl);
      padding: var(--space-6);
      transition: all var(--transition-normal);

      &:hover {
        transform: translateY(-2px);
        box-shadow: var(--shadow-lg);
      }
    }

    .adapter-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: var(--space-4);
    }

    .adapter-info {
      flex: 1;
    }

    .adapter-name {
      font-size: var(--text-lg);
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: var(--space-1);
    }

    .adapter-url {
      font-size: var(--text-sm);
      color: var(--text-tertiary);
      font-family: monospace;
      margin: 0;
    }

    .adapter-status {
      flex-shrink: 0;
    }

    .adapter-body {
      margin-bottom: var(--space-4);
    }

    .adapter-description {
      font-size: var(--text-sm);
      color: var(--text-secondary);
      margin: 0;
      line-height: 1.5;
    }

    .adapter-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .adapter-meta {
      display: flex;
      gap: var(--space-4);
      font-size: var(--text-xs);
      color: var(--text-tertiary);
    }

    .adapter-time,
    .adapter-response {
      display: flex;
      align-items: center;
      gap: var(--space-1);
    }

    .adapter-actions {
      flex-shrink: 0;
    }

    .actions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: var(--space-6);
    }

    .action-card {
      background: var(--bg-elevated);
      border: 1px solid var(--border-light);
      border-radius: var(--radius-xl);
      padding: var(--space-6);
      display: flex;
      align-items: center;
      gap: var(--space-4);
      cursor: pointer;
      transition: all var(--transition-normal);
      text-align: left;
      width: 100%;

      &:hover {
        transform: translateY(-2px);
        box-shadow: var(--shadow-lg);
        border-color: var(--primary-200);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .action-icon {
      width: 48px;
      height: 48px;
      background: var(--primary-50);
      color: var(--primary-600);
      border-radius: var(--radius-xl);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .action-content {
      flex: 1;
    }

    .action-content h3 {
      font-size: var(--text-lg);
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: var(--space-1);
    }

    .action-content p {
      font-size: var(--text-sm);
      color: var(--text-secondary);
      margin: 0;
    }

    .loading-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255, 255, 255, 0.8);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }

    .dark-theme .loading-overlay {
      background: rgba(0, 0, 0, 0.8);
    }

    .loading-spinner {
      text-align: center;

      p {
        margin-top: var(--space-4);
        color: var(--text-secondary);
        font-size: var(--text-sm);
      }
    }

    /* Responsive Design */
    @media (max-width: 768px) {
      .dashboard-header {
        flex-direction: column;
        align-items: stretch;
      }

      .header-actions {
        justify-content: stretch;
      }

      .stats-grid {
        grid-template-columns: 1fr;
      }

      .adapters-grid,
      .actions-grid {
        grid-template-columns: 1fr;
      }

      .adapter-footer {
        flex-direction: column;
        align-items: stretch;
        gap: var(--space-3);
      }

      .adapter-actions {
        align-self: stretch;
      }
    }
  `]
})
export class DashboardComponent implements OnInit {
  adapterStats: AdapterList = {
    adapters: [],
    total: 0,
    healthy: 0,
    unhealthy: 0,
    disabled: 0
  };
  
  recentAdapters: any[] = [];
  isLoading = false;

  constructor(private adapterService: McpAdapterService) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.adapterService.getAllAdapters().subscribe({
      next: (data) => {
        this.adapterStats = data;
        this.recentAdapters = data.adapters
          .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
          .slice(0, 6);
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading dashboard data:', error);
        this.isLoading = false;
      }
    });
  }

  refreshData(): void {
    this.loadDashboardData();
  }

  checkAllHealth(): void {
    this.isLoading = true;
    this.adapterService.checkAllAdaptersHealth().subscribe({
      next: (healthChecks) => {
        console.log('Health checks completed:', healthChecks);
        this.isLoading = false;
        this.loadDashboardData(); // Refresh data to show updated health status
      },
      error: (error) => {
        console.error('Error checking health:', error);
        this.isLoading = false;
      }
    });
  }

  getPercentage(value: number, total: number): number {
    if (total === 0) return 0;
    return Math.round((value / total) * 100);
  }

  getStatusClass(status: AdapterStatus): string {
    switch (status) {
      case AdapterStatus.Healthy:
        return 'badge-success';
      case AdapterStatus.Unhealthy:
        return 'badge-error';
      case AdapterStatus.Disabled:
        return 'badge-warning';
      default:
        return 'badge-neutral';
    }
  }

  getStatusText(status: AdapterStatus): string {
    switch (status) {
      case AdapterStatus.Healthy:
        return 'Healthy';
      case AdapterStatus.Unhealthy:
        return 'Unhealthy';
      case AdapterStatus.Disabled:
        return 'Disabled';
      default:
        return 'Unknown';
    }
  }
}