import { Component, effect, input, linkedSignal, output, signal, untracked } from '@angular/core';
import { NzSwitchComponent } from 'ng-zorro-antd/switch';
import { form, FormField } from '@angular/forms/signals';
import { NzIconDirective } from 'ng-zorro-antd/icon';

import { Theme } from '@ske/models';

@Component({
  selector: 'app-theme-switcher-form',
  imports: [NzSwitchComponent, FormField, NzIconDirective],
  template: `
    <nz-switch
      [formField]="themeForm.darkMode"
      [nzCheckedChildren]="darkMode"
      [nzUnCheckedChildren]="lightMode" />
    <ng-template #darkMode>
      <nz-icon nzType="icons:moon"></nz-icon>
    </ng-template>
    <ng-template #lightMode>
      <nz-icon nzType="icons:sun-bright"></nz-icon>
    </ng-template>
  `
})
export class ThemeSwitcherFormComponent {
  public readonly currentTheme = input.required<Theme>();

  public readonly onThemeChange = output<Theme>();

  private readonly model = linkedSignal<ThemeToggleFormModel>(() => ({
    darkMode: this.currentTheme() === Theme.dark
  }));

  public readonly themeForm = form(this.model);

  constructor() {
    effect(() => {
      const isDarkMode = this.themeForm.darkMode().value();

      untracked(() => this.onThemeChange.emit(isDarkMode ? Theme.dark : Theme.default));
    });
  }
}

type ThemeToggleFormModel = {
  darkMode: boolean;
};
