import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message: string;
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private notificationSubject = new Subject<Notification>();
  public notifications$ = this.notificationSubject.asObservable();

  success(title: string, message: string, duration: number = 5000): void {
    this.show('success', title, message, duration);
  }

  error(title: string, message: string, duration: number = 7000): void {
    this.show('error', title, message, duration);
  }

  warning(title: string, message: string, duration: number = 6000): void {
    this.show('warning', title, message, duration);
  }

  info(title: string, message: string, duration: number = 5000): void {
    this.show('info', title, message, duration);
  }

  private show(type: 'success' | 'error' | 'warning' | 'info', title: string, message: string, duration: number): void {
    const notification: Notification = {
      id: `notification-${Date.now()}-${Math.random()}`,
      type,
      title,
      message,
      duration
    };
    this.notificationSubject.next(notification);
  }
}

