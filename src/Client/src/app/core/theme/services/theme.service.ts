import {DOCUMENT, inject, Injectable, Service, signal} from '@angular/core';

import { Theme } from '@ske/models';

@Service()
export class ThemeService {
  private readonly _document = inject(DOCUMENT);

  private getInitialTheme(): Theme {
    const storedTheme = localStorage.getItem('theme');
    if (storedTheme && Object.values(Theme).includes(storedTheme as Theme)) {
      return storedTheme as Theme;
    }

    return this.getSystemTheme();
  }

  public readonly currentTheme = signal<Theme>(this.getInitialTheme());

  private removeUnusedTheme(theme: Theme) {
    this._document.documentElement.classList.remove(theme);
    const removedThemeStyle = document.getElementById(theme);
    if (removedThemeStyle) {
      document.head.removeChild(removedThemeStyle);
    }
  }

  private loadCss(href: string, id: string): Promise<Event> {
    return new Promise((resolve, reject) => {
      const style = document.createElement('link');
      style.rel = 'stylesheet';
      style.href = href;
      style.id = id;
      style.onload = resolve;
      style.onerror = reject;
      document.head.append(style);
    });
  }

  // Keeps the mobile browser/status bar colour in sync with the user-selected theme.
  private updateThemeColor(theme: Theme) {
    const color = theme === Theme.dark ? '#141414' : '#ffffff';
    this._document
      .querySelectorAll<HTMLMetaElement>('meta[name="theme-color"]')
      .forEach((meta) => {
        meta.content = color;
      });
    this._document.documentElement.style.colorScheme = theme === Theme.dark ? 'dark' : 'light';
  }

  public getSystemTheme(): Theme {
    return this._document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)').matches ? Theme.dark : Theme.default;
  }

  public loadTheme(firstLoad = true, theme: Theme | null = null): Promise<Event> {
    theme ??= this.currentTheme();
    if (firstLoad) {
      document.documentElement.classList.add(theme);
    }

    return new Promise<Event>((resolve, reject) => {
      this.loadCss(`${theme}.css`, theme).then(
        (e) => {
          if (!firstLoad) {
            document.documentElement.classList.add(theme);
          }

          [Theme.compact, Theme.dark, Theme.default]
            .filter((e) => e !== theme)
            .forEach((e) => this.removeUnusedTheme(e));

          localStorage.setItem('theme', theme);
          this.currentTheme.set(theme);
          this.updateThemeColor(theme);

          resolve(e);
        },
        (e) => reject(e),
      );
    });
  }
}
