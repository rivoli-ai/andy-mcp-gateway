import { Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';
import { INotificationService } from '../interfaces/api.interface';

@Injectable({
  providedIn: 'root'
})
export class NotificationService implements INotificationService {
  private readonly defaultConfig: MatSnackBarConfig = {
    duration: 3000,
    horizontalPosition: 'right',
    verticalPosition: 'top'
  };

  constructor(private snackBar: MatSnackBar) {}

  showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      panelClass: ['success-snackbar']
    });
  }

  showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }

  showWarning(message: string): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      panelClass: ['warning-snackbar']
    });
  }

  showInfo(message: string): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      panelClass: ['info-snackbar']
    });
  }
}






