import { NgOptimizedImage } from '@angular/common';
import { Component, computed, effect, input, linkedSignal, model, signal, untracked } from '@angular/core';
import { form, FormField, type FormValueControl } from '@angular/forms/signals';
import { NzFilterOptionType, NzOptionComponent, NzSelectComponent, NzSelectItemInterface } from 'ng-zorro-antd/select';
import { ICON_CATALOG, type IconPickerValue } from '../icon-catalog';
import { NzIconDirective } from 'ng-zorro-antd/icon';

@Component({
  selector: 'ske-icon-picker',
  imports: [
    FormField,
    NgOptimizedImage,
    NzOptionComponent,
    NzSelectComponent,
    NzIconDirective
  ],
  template: `
    <nz-select [formField]="iconForm.icon"
               [nzAllowClear]="allowClear()"
               [nzDisabled]="disabled()"
               [nzFilterOption]="filterOption"
               [nzPlaceHolder]="placeholder()"
               [nzShowSearch]="true"
               (nzOnSearch)="onSearch($event)"
               (nzScrollToBottom)="loadNextPage()"
               [nzId]="id()"
               [attr.aria-label]="ariaLabel()"
               [nzCustomTemplate]="customSelectTemplate"
               [compareWith]="(a, b) => a && b ? a.fileName === b.fileName : a === b"
               class="w-full">
      @for (icon of visibleIcons(); track icon.path) {
        <nz-option [nzLabel]="icon.name"
                   [nzValue]="icon"
                   nzCustomContent>
          <div class="flex items-center gap-2">
            <nz-icon [nzType]="'icons:' + icon.fileName"></nz-icon>
            <span>{{ icon.name }}</span>
          </div>
        </nz-option>
      }
    </nz-select>
    <ng-template #customSelectTemplate
                 let-selected>
      <div class="flex items-center gap-2">
        @let value = selected.nzValue;
        <nz-icon [nzType]="'icons:' + value.fileName"></nz-icon>
        <span>{{ value.name }}</span>
      </div>
    </ng-template>
  `
})
export class IconPicker implements FormValueControl<IconPickerValue | null> {
  private static readonly PAGE_SIZE = 50;

  public readonly value = model<IconPickerValue | null>(null);
  public readonly disabled = input<boolean>(false);
  public readonly allowClear = input<boolean>(true);
  public readonly placeholder = input<string>('Selectați o pictogramă');
  public readonly ariaLabel = input<string>('Selectați o pictogramă');
  public readonly id = input<string | null>(null);
  public readonly visibleIcons = computed(() => {
    const filteredIcons = this.filteredIcons();
    const visibleIcons = filteredIcons.slice(0, this.page() * IconPicker.PAGE_SIZE);
    const selectedIcon = this.value();
    const searchTerm = this.searchTerm().trim();

    if (selectedIcon &&
        searchTerm.length === 0 &&
        !visibleIcons.some((icon) => icon.fileName === selectedIcon.fileName)) {
      return [...visibleIcons, selectedIcon];
    }

    return visibleIcons;
  });

  private readonly searchTerm = signal('');
  private readonly page = signal(1);
  private readonly filteredIcons = computed(() => {
    const searchTerm = this.searchTerm().trim().toLocaleLowerCase();

    if (searchTerm.length === 0) {
      return ICON_CATALOG;
    }

    return ICON_CATALOG.filter((icon) =>
      `${icon.name} ${icon.fileName} ${icon.path}`.toLocaleLowerCase().includes(searchTerm));
  });

  private readonly _formModel = linkedSignal({
    source: () => this.value(),
    computation: (value) => (<IconPickerFormModel>{ icon: value })
  });

  public readonly iconForm = form(this._formModel);

  public readonly filterOption: NzFilterOptionType = (inputValue, option) => {
    const icon = option.nzValue;

    if (!isIconPickerValue(icon)) {
      return false;
    }

    const searchTerm = inputValue.trim().toLocaleLowerCase();
    return `${icon.name} ${icon.fileName} ${icon.path}`.toLocaleLowerCase().includes(searchTerm);
  };

  public onSearch(searchTerm: string): void {
    this.searchTerm.set(searchTerm);
    this.page.set(1);
  }

  public loadNextPage(): void {
    if (this.page() * IconPicker.PAGE_SIZE < this.filteredIcons().length) {
      this.page.update((page) => page + 1);
    }
  }

  private readonly _formIconChangeEffectRef = effect(() => {
    const icon = this.iconForm.icon().value();

    untracked(() => this.value.set(icon));
  });

  public log(value: any) {
    console.log(value);
  }
}

type IconPickerFormModel = {
  icon: IconPickerValue | null;
};

function isIconPickerValue(value: NzSelectItemInterface['nzValue']): value is IconPickerValue {
  return typeof value === 'object' &&
    value !== null &&
    'name' in value &&
    typeof value.name === 'string' &&
    'fileName' in value &&
    typeof value.fileName === 'string' &&
    'path' in value &&
    typeof value.path === 'string';
}
