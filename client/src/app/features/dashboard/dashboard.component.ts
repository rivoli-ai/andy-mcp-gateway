import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MarkdownModule } from 'ngx-markdown';
import { McpAdapterService } from '../../core/services/mcp-adapter.service';
import { AdapterList, AdapterStatus } from '../../core/models/mcp-adapter.model';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { NotificationService } from '../../shared/components/notification/notification.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MarkdownModule, StatusBadgeComponent],
  template: `
    <div class="dashboard">
      <!-- Header Section -->
      <div class="dashboard-header">
        <div class="header-content">
          <h1 class="dashboard-title">Dashboard</h1>
          <p class="dashboard-subtitle">Monitor and manage your MCP adapters</p>
        </div>
        <div class="header-actions">
          <div class="action-button-group">
            <button class="action-btn export-btn" (click)="exportAdapters()" [disabled]="isLoading || adapterStats.total === 0" title="Export all adapters to Excel">
              <div class="btn-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <polyline points="14 2 14 8 20 8"/>
                  <line x1="12" y1="18" x2="12" y2="12"/>
                  <line x1="9" y1="15" x2="12" y2="18"/>
                  <line x1="15" y1="15" x2="12" y2="18"/>
                </svg>
              </div>
              <div class="btn-content">
                <span class="btn-label">Export</span>
                <span class="btn-hint">Excel file</span>
              </div>
            </button>
            <button class="action-btn import-btn" (click)="fileInput.click()" [disabled]="isLoading" title="Import adapters from Excel file">
              <div class="btn-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <polyline points="14 2 14 8 20 8"/>
                  <line x1="12" y1="11" x2="12" y2="17"/>
                  <line x1="9" y1="14" x2="12" y2="11"/>
                  <line x1="15" y1="14" x2="12" y2="11"/>
                </svg>
              </div>
              <div class="btn-content">
                <span class="btn-label">Import</span>
                <span class="btn-hint">Upload file</span>
              </div>
            </button>
            <input #fileInput type="file" accept=".xlsx" (change)="onFileSelected($event)" style="display: none;" />
          </div>
          <div class="action-button-group">
            <button class="action-btn primary-btn" routerLink="/adapters/new">
              <div class="btn-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="12" r="10"/>
                  <line x1="12" y1="8" x2="12" y2="16"/>
                  <line x1="8" y1="12" x2="16" y2="12"/>
                </svg>
              </div>
              <div class="btn-content">
                <span class="btn-label">New Adapter</span>
                <span class="btn-hint">Create</span>
              </div>
            </button>
            <button class="action-btn refresh-btn" (click)="refreshData()" [disabled]="isLoading">
              <div class="btn-icon" [class.spinning]="isLoading">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <polyline points="23 4 23 10 17 10"/>
                  <polyline points="1 20 1 14 7 14"/>
                  <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
                </svg>
              </div>
              <div class="btn-content">
                <span class="btn-label">Refresh</span>
                <span class="btn-hint">Reload</span>
              </div>
            </button>
          </div>
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
                <app-status-badge [status]="adapter.status"></app-status-badge>
              </div>
            </div>
            
            <div class="adapter-body" *ngIf="adapter.description">
              <div class="adapter-description">
                <markdown>{{ adapter.description }}</markdown>
              </div>
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
      gap: var(--space-4);
      flex-shrink: 0;
      flex-wrap: wrap;
    }

    .action-button-group {
      display: flex;
      gap: var(--space-2);
      background: var(--bg-elevated);
      border: 1px solid var(--border-light);
      border-radius: var(--radius-xl);
      padding: var(--space-1);
    }

    .action-btn {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-2) var(--space-3);
      border: none;
      border-radius: var(--radius-lg);
      font-size: var(--text-sm);
      font-weight: 500;
      cursor: pointer;
      transition: all var(--transition-normal);
      position: relative;
      overflow: hidden;
      
      &::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: linear-gradient(135deg, rgba(255,255,255,0.1), rgba(255,255,255,0));
        opacity: 0;
        transition: opacity var(--transition-fast);
      }

      &:hover:not(:disabled)::before {
        opacity: 1;
      }

      &:hover:not(:disabled) {
        transform: translateY(-1px);
        box-shadow: var(--shadow-md);
      }

      &:active:not(:disabled) {
        transform: translateY(0);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .btn-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      border-radius: var(--radius-md);
      flex-shrink: 0;
      transition: all var(--transition-normal);

      svg {
        width: 16px;
        height: 16px;
      }

      &.spinning {
        animation: spin 1s linear infinite;
      }
    }

    @keyframes spin {
      from {
        transform: rotate(0deg);
      }
      to {
        transform: rotate(360deg);
      }
    }

    .btn-content {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 2px;
    }

    .btn-label {
      font-size: 0.8125rem;
      font-weight: 600;
      line-height: 1;
    }

    .btn-hint {
      font-size: 0.625rem;
      opacity: 0.75;
      line-height: 1;
    }

    /* Export Button */
    .export-btn {
      background: #10b981;
      color: white;
      border: 1px solid #059669;

      .btn-icon {
        background: rgba(255, 255, 255, 0.15);
      }

      &:hover:not(:disabled) {
        background: #059669;
        border-color: #047857;
        
        .btn-icon {
          background: rgba(255, 255, 255, 0.25);
          transform: scale(1.05);
        }
      }
    }

    /* Import Button */
    .import-btn {
      background: var(--bg-elevated);
      color: var(--text-primary);
      border: 1px solid var(--border-medium);

      .btn-icon {
        background: #f1f5f9;
        color: #64748b;
      }

      &:hover:not(:disabled) {
        background: var(--bg-tertiary);
        border-color: var(--border-strong);
        
        .btn-icon {
          background: #e2e8f0;
          color: #475569;
          transform: scale(1.05);
        }
      }
    }

    /* Primary Button */
    .primary-btn {
      background: var(--primary-600);
      color: white;

      .btn-icon {
        background: rgba(255, 255, 255, 0.15);
      }

      &:hover:not(:disabled) {
        background: var(--primary-700);
        
        .btn-icon {
          background: rgba(255, 255, 255, 0.25);
          transform: scale(1.05);
        }
      }
    }

    /* Refresh Button */
    .refresh-btn {
      background: var(--bg-elevated);
      color: var(--text-primary);
      border: 1px solid var(--border-medium);

      .btn-icon {
        background: #f1f5f9;
        color: #64748b;
      }

      &:hover:not(:disabled) {
        background: var(--bg-tertiary);
        border-color: var(--border-strong);
        
        .btn-icon {
          background: #e2e8f0;
          color: #475569;
        }
      }
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
      
      // Limit to 3 lines with ellipsis
      display: -webkit-box;
      -webkit-line-clamp: 3;
      -webkit-box-orient: vertical;
      overflow: hidden;
      text-overflow: ellipsis;

      ::ng-deep {
        p {
          margin: 0 0 0.5rem 0;
          &:last-child {
            margin-bottom: 0;
          }
        }
        
        code {
          font-family: 'SF Mono', Monaco, 'Cascadia Code', 'Roboto Mono', Consolas, 'Courier New', monospace;
          font-size: 0.8125rem;
          background: #f1f5f9;
          color: #475569;
          padding: 0.125rem 0.375rem;
          border-radius: 0.25rem;
        }
        
        pre {
          background: #f8fafc;
          border: 1px solid #e2e8f0;
          border-radius: 0.375rem;
          padding: 0.75rem;
          overflow-x: auto;
          margin: 0.5rem 0;
          
          code {
            background: none;
            padding: 0;
          }
        }
        
        strong {
          font-weight: 600;
          color: #475569;
        }
        
        em {
          font-style: italic;
        }
        
        ul, ol {
          margin: 0.5rem 0;
          padding-left: 1.5rem;
          
          li {
            margin: 0.25rem 0;
          }
        }
        
        a {
          color: #3b82f6;
          text-decoration: none;
          
          &:hover {
            text-decoration: underline;
          }
        }
        
        h1, h2, h3, h4, h5, h6 {
          margin: 0.75rem 0 0.5rem 0;
          font-weight: 600;
          line-height: 1.3;
          color: #1e293b;
        }
        
        h1 { font-size: 1.125rem; }
        h2 { font-size: 1rem; }
        h3 { font-size: 0.9375rem; }
        h4, h5, h6 { font-size: 0.875rem; }
      }
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

    /* Dark Theme Support */
    :host-context(.dark-theme) {
      .action-button-group {
        background: #1e293b;
        border-color: #334155;
      }

      .export-btn {
        background: #059669;
        border-color: #047857;
        
        &:hover:not(:disabled) {
          background: #047857;
          border-color: #065f46;
        }
      }

      .import-btn,
      .refresh-btn {
        background: #1e293b;
        color: #e2e8f0;
        border-color: #475569;

        .btn-icon {
          background: #334155;
          color: #94a3b8;
        }

        &:hover:not(:disabled) {
          background: #334155;
          border-color: #64748b;
          
          .btn-icon {
            background: #475569;
            color: #cbd5e1;
            transform: scale(1.05);
          }
        }
      }

      .primary-btn {
        background: var(--primary-600);
        
        &:hover:not(:disabled) {
          background: var(--primary-700);
        }
      }

      .adapter-description {
        code {
          background: #1e293b;
          color: #94a3b8;
        }
        
        pre {
          background: #0f172a;
          border-color: #334155;
        }
        
        strong {
          color: #cbd5e1;
        }
        
        h1, h2, h3, h4, h5, h6 {
          color: #f1f5f9;
        }
      }
    }

    /* Responsive Design */
    @media (max-width: 1024px) {
      .header-actions {
        flex-direction: column;
        align-items: stretch;
        
        .action-button-group {
          justify-content: space-between;
        }
      }

      .action-btn {
        flex: 1;
        justify-content: flex-start;
      }
    }

    @media (max-width: 768px) {
      .dashboard-header {
        flex-direction: column;
        align-items: stretch;
      }

      .header-actions {
        justify-content: stretch;
        width: 100%;
      }

      .action-button-group {
        flex-direction: column;
      }

      .action-btn {
        width: 100%;
        justify-content: flex-start;
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

      .btn-content {
        .btn-hint {
          display: none;
        }
      }
    }

    @media (max-width: 480px) {
      .action-btn {
        padding: var(--space-2);
        gap: var(--space-2);
      }

      .btn-icon {
        width: 22px;
        height: 22px;
        
        svg {
          width: 14px;
          height: 14px;
        }
      }

      .btn-label {
        font-size: 0.75rem;
      }

      .btn-hint {
        font-size: 0.5625rem;
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

  constructor(
    private adapterService: McpAdapterService,
    private notificationService: NotificationService
  ) {}

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

  exportAdapters(): void {
    this.isLoading = true;
    this.adapterService.exportAdaptersToExcel().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `MCP_Adapters_Export_${new Date().toISOString().split('T')[0]}.xlsx`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.isLoading = false;
        this.notificationService.success(
          'Export Successful',
          `${this.adapterStats.total} adapter(s) exported to Excel file`
        );
      },
      error: (error) => {
        console.error('Error exporting adapters:', error);
        this.notificationService.error(
          'Export Failed',
          error.message || 'Failed to export adapters. Please try again.'
        );
        this.isLoading = false;
      }
    });
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      this.importAdapters(file);
    }
    // Reset file input
    event.target.value = '';
  }

  importAdapters(file: File): void {
    if (!file.name.endsWith('.xlsx')) {
      this.notificationService.error(
        'Invalid File Type',
        'Please select an Excel (.xlsx) file'
      );
      return;
    }

    this.isLoading = true;
    this.adapterService.importAdaptersFromExcel(file).subscribe({
      next: (result) => {
        this.isLoading = false;
        
        // Build detailed message
        const details: string[] = [];
        
        if (result.validationErrors && result.validationErrors.length > 0) {
          details.push('Validation Warnings: ' + result.validationErrors.length);
        }
        
        if (result.failedAdapters && result.failedAdapters.length > 0) {
          details.push('Failed: ' + result.failedCount);
        }
        
        const detailMessage = details.length > 0 ? details.join(', ') : 'All adapters imported successfully';
        
        // Show appropriate notification
        if (result.successCount > 0 && result.failedCount === 0) {
          this.notificationService.success(
            'Import Successful',
            `${result.successCount} adapter(s) imported. ${detailMessage}`
          );
        } else if (result.successCount > 0) {
          this.notificationService.warning(
            'Import Partially Successful',
            `${result.successCount} adapter(s) imported. ${detailMessage}`
          );
        } else {
          this.notificationService.error(
            'Import Failed',
            `No adapters were imported. ${detailMessage}`
          );
        }
        
        // Log details to console
        if (result.validationErrors && result.validationErrors.length > 0) {
          console.warn('Validation errors:', result.validationErrors);
        }
        if (result.failedAdapters && result.failedAdapters.length > 0) {
          console.error('Failed adapters:', result.failedAdapters);
        }
        
        // Refresh dashboard if any adapters were imported
        if (result.successCount > 0) {
          this.loadDashboardData();
        }
      },
      error: (error) => {
        console.error('Error importing adapters:', error);
        this.notificationService.error(
          'Import Failed',
          error.message || 'Failed to import adapters. Please try again.'
        );
        this.isLoading = false;
      }
    });
  }
}