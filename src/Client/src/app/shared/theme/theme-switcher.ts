import { Component, inject } from '@angular/core';
import { ThemeService } from '@ske/theme';
import { ThemeSwitcherFormComponent } from './theme-switcher-form';
import { Theme } from '@ske/models';

@Component({
  selector: 'ske-theme-switcher',
  imports: [ThemeSwitcherFormComponent],
  template: `
    <app-theme-switcher-form
      [currentTheme]="currentTheme()"
      (onThemeChange)="onThemeChange($event)"
    />
  `,
})
export class ThemeSwitcher {
  private readonly _themeService = inject(ThemeService);

  public readonly currentTheme = this._themeService.currentTheme;

  public onThemeChange(theme: Theme) {
    this._themeService.loadTheme(false, theme);
  }
}
