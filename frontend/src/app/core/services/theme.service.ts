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
    const root = document.documentElement;
    const body = document.body;
    if (isDark) {
      /* Root first so <html> scrollbars pick up dark tokens before the shell paints dark. */
      root.classList.add(this.DARK_THEME_CLASS);
      body?.classList.add(this.DARK_THEME_CLASS);
    } else {
      /* Strip body first so content tokens stay dark until <html> matches — avoids light thumb on a dark page. */
      body?.classList.remove(this.DARK_THEME_CLASS);
      root.classList.remove(this.DARK_THEME_CLASS);
    }

    /* Blink/WebKit cache ::-webkit-scrollbar painting until scroll; force a harmless reflow. */
    this.scheduleScrollbarPaintRefresh();
  }

  /** WebKit/Blink often keep the old scrollbar thumb until the user scrolls; nudge known scroll roots. */
  private scheduleScrollbarPaintRefresh(): void {
    requestAnimationFrame(() => {
      this.bumpScrollRootsForScrollbarInvalidation();
      window.dispatchEvent(new Event('resize'));
      requestAnimationFrame(() => this.bumpScrollRootsForScrollbarInvalidation());
    });
  }

  private bumpScrollRootsForScrollbarInvalidation(): void {
    const seen = new Set<HTMLElement>();

    const bump = (el: Element | null): void => {
      if (!(el instanceof HTMLElement) || seen.has(el)) {
        return;
      }
      seen.add(el);

      if (el.scrollHeight > el.clientHeight) {
        const y = el.scrollTop;
        el.scrollTop = y > 0 ? y - 1 : 1;
        el.scrollTop = y;
        return;
      }
      if (el.scrollWidth > el.clientWidth) {
        const x = el.scrollLeft;
        el.scrollLeft = x > 0 ? x - 1 : 1;
        el.scrollLeft = x;
      }
    };

    bump(document.documentElement);
    bump(document.body);
    bump(document.scrollingElement);
    document
      .querySelectorAll<HTMLElement>(
        '.sidebar-nav, .page-content, .integration-code-sample__body, .integration-code-sample__client-tabs'
      )
      .forEach((el) => bump(el));
  }

  private saveTheme(isDark: boolean): void {
    localStorage.setItem(this.THEME_KEY, isDark ? 'dark' : 'light');
  }
}






