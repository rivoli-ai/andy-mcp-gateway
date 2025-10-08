import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { IThemeService } from '../interfaces/api.interface';

@Injectable({
  providedIn: 'root'
})
export class ThemeService implements IThemeService {
  private readonly THEME_KEY = 'mcp-gateway-theme';
  private readonly DARK_THEME_CLASS = 'dark-theme';
  
  private _isDarkMode = new BehaviorSubject<boolean>(this.getInitialTheme());
  public isDarkMode$ = this._isDarkMode.asObservable();

  constructor() {
    this.applyTheme(this._isDarkMode.value);
  }

  get isDarkMode(): boolean {
    return this._isDarkMode.value;
  }

  toggleTheme(): void {
    this.setTheme(!this._isDarkMode.value);
  }

  setTheme(isDark: boolean): void {
    this._isDarkMode.next(isDark);
    this.applyTheme(isDark);
    this.saveTheme(isDark);
  }

  private getInitialTheme(): boolean {
    const savedTheme = localStorage.getItem(this.THEME_KEY);
    if (savedTheme) {
      return savedTheme === 'dark';
    }
    
    // Check system preference
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }

  private applyTheme(isDark: boolean): void {
    const body = document.body;
    if (isDark) {
      body.classList.add(this.DARK_THEME_CLASS);
    } else {
      body.classList.remove(this.DARK_THEME_CLASS);
    }
  }

  private saveTheme(isDark: boolean): void {
    localStorage.setItem(this.THEME_KEY, isDark ? 'dark' : 'light');
  }
}






