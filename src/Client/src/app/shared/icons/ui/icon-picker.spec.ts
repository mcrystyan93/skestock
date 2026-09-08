import { TestBed } from '@angular/core/testing';
import { ICON_CATALOG } from '../icon-catalog';
import { IconPicker } from './icon-picker';

describe('IconPicker', () => {
  let picker: IconPicker;

  beforeEach(() => {
    const fixture = TestBed.createComponent(IconPicker);
    picker = fixture.componentInstance;
  });

  it('matches readable names case-insensitively', () => {
    const arrowUp = ICON_CATALOG.find((icon) => icon.path.endsWith('/arrow-up.svg'));

    expect(arrowUp).toBeDefined();
    expect(picker.filterOption('ARROW UP', { nzValue: arrowUp!, nzLabel: arrowUp!.name })).toBe(true);
  });
  it('matches the selected icon path', () => {
    const arrowUp = ICON_CATALOG.find((icon) => icon.path.endsWith('/arrow-up.svg'));

    expect(arrowUp).toBeDefined();
    expect(picker.filterOption('arrow-up', { nzValue: arrowUp!, nzLabel: arrowUp!.name })).toBe(true);
  });

  it('rejects a non-matching search term', () => {
    expect(picker.filterOption('does-not-exist', { nzValue: ICON_CATALOG[0], nzLabel: ICON_CATALOG[0].name })).toBe(false);
  });

  it('shows the first page and appends another page on scroll', () => {
    expect(picker.visibleIcons()).toHaveLength(50);

    picker.loadNextPage();

    expect(picker.visibleIcons()).toHaveLength(100);
  });

  it('resets to the first page when searching', () => {
    picker.loadNextPage();
    picker.onSearch('arrow');

    expect(picker.visibleIcons().length).toBeLessThanOrEqual(50);
    expect(picker.visibleIcons().every((icon) =>
      `${icon.name} ${icon.fileName} ${icon.path}`.toLocaleLowerCase().includes('arrow'))).toBe(true);
  });

  it('does not append pages after the filtered catalog is exhausted', () => {
    picker.onSearch('square-q');
    const matchingCount = picker.visibleIcons().length;

    picker.loadNextPage();

    expect(picker.visibleIcons()).toHaveLength(matchingCount);
  });

  it('keeps the selected icon visible when it is outside the first page', () => {
    const selectedIcon = ICON_CATALOG[100];
    picker.value.set(selectedIcon);

    expect(picker.visibleIcons()).toContainEqual(selectedIcon);
  });
});
