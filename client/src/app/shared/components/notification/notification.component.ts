import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, Notification } from './notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-notification',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="notification-container">
      <div 
        *ngFor="let notification of notifications" 
        class="notification"
        [class.success]="notification.type === 'success'"
        [class.error]="notification.type === 'error'"
        [class.warning]="notification.type === 'warning'"
        [class.info]="notification.type === 'info'"
        [@slideIn]>
        <div class="notification-icon">
          <svg *ngIf="notification.type === 'success'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
            <path d="M22 4L12 14.01l-3-3"/>
          </svg>
          <svg *ngIf="notification.type === 'error'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <line x1="15" y1="9" x2="9" y2="15"/>
            <line x1="9" y1="9" x2="15" y2="15"/>
          </svg>
          <svg *ngIf="notification.type === 'warning'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
            <line x1="12" y1="9" x2="12" y2="13"/>
            <line x1="12" y1="17" x2="12.01" y2="17"/>
          </svg>
          <svg *ngIf="notification.type === 'info'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <line x1="12" y1="16" x2="12" y2="12"/>
            <line x1="12" y1="8" x2="12.01" y2="8"/>
          </svg>
        </div>
        <div class="notification-content">
          <div class="notification-title">{{ notification.title }}</div>
          <div class="notification-message">{{ notification.message }}</div>
        </div>
        <button class="notification-close" (click)="removeNotification(notification.id)">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"/>
            <line x1="6" y1="6" x2="18" y2="18"/>
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .notification-container {
      position: fixed;
      top: 1rem;
      right: 1rem;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      max-width: 400px;
      pointer-events: none;
    }

    .notification {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 1rem;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 0.75rem;
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
      pointer-events: auto;
      animation: slideIn 0.3s ease-out;
      transition: all 0.3s ease;
      
      &:hover {
        box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      }
    }

    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    .notification-icon {
      flex-shrink: 0;
      width: 40px;
      height: 40px;
      border-radius: 0.5rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .notification.success {
      border-left: 4px solid #10b981;
      
      .notification-icon {
        background: #d1fae5;
        color: #059669;
      }
    }

    .notification.error {
      border-left: 4px solid #ef4444;
      
      .notification-icon {
        background: #fee2e2;
        color: #dc2626;
      }
    }

    .notification.warning {
      border-left: 4px solid #f59e0b;
      
      .notification-icon {
        background: #fef3c7;
        color: #d97706;
      }
    }

    .notification.info {
      border-left: 4px solid #3b82f6;
      
      .notification-icon {
        background: #dbeafe;
        color: #2563eb;
      }
    }

    .notification-content {
      flex: 1;
      min-width: 0;
    }

    .notification-title {
      font-size: 0.875rem;
      font-weight: 600;
      color: #1e293b;
      margin-bottom: 0.25rem;
      line-height: 1.25;
    }

    .notification-message {
      font-size: 0.8125rem;
      color: #64748b;
      line-height: 1.4;
      word-wrap: break-word;
    }

    .notification-close {
      flex-shrink: 0;
      width: 24px;
      height: 24px;
      border: none;
      background: transparent;
      color: #94a3b8;
      cursor: pointer;
      border-radius: 0.375rem;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
      
      &:hover {
        background: #f1f5f9;
        color: #475569;
      }
    }

    /* Dark Theme */
    :host-context(.dark-theme) {
      .notification {
        background: #1e293b;
        border-color: #334155;
      }

      .notification-title {
        color: #f1f5f9;
      }

      .notification-message {
        color: #cbd5e1;
      }

      .notification.success {
        border-left-color: #10b981;
        
        .notification-icon {
          background: #064e3b;
          color: #10b981;
        }
      }

      .notification.error {
        border-left-color: #ef4444;
        
        .notification-icon {
          background: #7f1d1d;
          color: #ef4444;
        }
      }

      .notification.warning {
        border-left-color: #f59e0b;
        
        .notification-icon {
          background: #78350f;
          color: #f59e0b;
        }
      }

      .notification.info {
        border-left-color: #3b82f6;
        
        .notification-icon {
          background: #1e3a8a;
          color: #3b82f6;
        }
      }

      .notification-close {
        color: #64748b;
        
        &:hover {
          background: #334155;
          color: #cbd5e1;
        }
      }
    }

    /* Mobile Responsive */
    @media (max-width: 640px) {
      .notification-container {
        left: 1rem;
        right: 1rem;
        max-width: none;
      }
    }
  `]
})
export class NotificationComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  private subscription?: Subscription;

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.subscription = this.notificationService.notifications$.subscribe(
      (notification) => {
        this.addNotification(notification);
      }
    );
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  private addNotification(notification: Notification): void {
    this.notifications.push(notification);

    if (notification.duration && notification.duration > 0) {
      setTimeout(() => {
        this.removeNotification(notification.id);
      }, notification.duration);
    }
  }

  removeNotification(id: string): void {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }
}

