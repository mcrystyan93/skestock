import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { ItemHero } from './item-hero';

describe('ItemHero', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons([{ name: 'icons:box', icon: '<svg viewBox="0 0 24 24"></svg>' }]),
      ],
    }),
  );

  const create = (inputs: Record<string, unknown>): ComponentFixture<ItemHero> => {
    const fixture = TestBed.createComponent(ItemHero);
    for (const [key, value] of Object.entries(inputs)) fixture.componentRef.setInput(key, value);
    fixture.detectChanges();
    return fixture;
  };

  it('shows the name and subtitle', () => {
    const el: HTMLElement = create({ itemId: 'i1', name: 'Creioane', subtitle: 'Papetărie · Dulap' }).nativeElement;

    expect(el.textContent).toContain('Creioane');
    expect(el.textContent).toContain('Papetărie · Dulap');
  });

  it('renders the image when imageFileId is set', () => {
    const el: HTMLElement = create({ itemId: 'i1', name: 'Creioane', imageFileId: 'f1' }).nativeElement;

    expect(el.querySelector('img')?.getAttribute('src')).toContain('/api/Items/i1/image?size=full');
  });

  it('falls back to the placeholder icon without an image', () => {
    const el: HTMLElement = create({ itemId: 'i1', name: 'Creioane' }).nativeElement;

    expect(el.querySelector('img')).toBeNull();
    expect(el.querySelector('nz-icon')).not.toBeNull();
  });
});
