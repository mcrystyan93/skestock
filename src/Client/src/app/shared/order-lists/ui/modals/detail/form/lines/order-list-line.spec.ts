import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { By } from '@angular/platform-browser';
import { ItemThumbnail } from '@ske/shared/items';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { OrderListLine, type OrderListLineFormModel } from './order-list-line';

const icon = (name: string) => ({ name, icon: '<svg viewBox="0 0 1 1"><path d="M0 0h1v1H0z"/></svg>' });

function createLine(overrides: Partial<OrderListLineFormModel> = {}): OrderListLineFormModel {
  return {
    id: null,
    itemId: 'item-1',
    itemImageFileId: 'img-1',
    productName: 'Creion',
    quantity: 1,
    unit: 'buc',
    notes: '',
    clientKey: 'line-1',
    ...overrides
  };
}

describe('OrderListLine', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons(
          ['trash-can', 'minus', 'plus', 'pencil', 'circle-exclamation', 'box'].map((n) => icon(`icons:${n}`))
        )
      ]
    });
  });

  function create(model: Partial<OrderListLineFormModel> = {}) {
    const fixture = TestBed.createComponent(OrderListLine);
    const lineForm = TestBed.runInInjectionContext(() => form(signal(createLine(model))));
    fixture.componentRef.setInput('line', lineForm);
    fixture.detectChanges();
    return fixture;
  }

  const toggle = (root: HTMLElement) =>
    Array.from(root.querySelectorAll('button')).find((b) => b.textContent?.includes('Adaugă notiță'));
  const notesInput = (root: HTMLElement) =>
    root.querySelector<HTMLInputElement>('input[placeholder="Adaugă notițe"]');
  const notesItem = (root: HTMLElement) => notesInput(root)?.closest('nz-form-item');

  it('offers the notes toggle and hides the notes on small screens when there are none', () => {
    const root = create().nativeElement as HTMLElement;

    expect(toggle(root)).toBeDefined();
    expect(notesItem(root)?.classList.contains('max-md:hidden')).toBe(true);
  });

  it('reveals the notes and focuses their input when the toggle is clicked', () => {
    const fixture = create();
    const root = fixture.nativeElement as HTMLElement;
    document.body.appendChild(root);

    toggle(root)?.click();
    fixture.detectChanges();
    TestBed.tick();

    expect(toggle(root)).toBeUndefined();
    expect(notesItem(root)?.classList.contains('max-md:hidden')).toBe(false);
    expect(document.activeElement).toBe(notesInput(root));
    root.remove();
  });

  it('shows existing notes without the toggle and without stealing focus', () => {
    const fixture = create({ notes: 'urgent' });
    const root = fixture.nativeElement as HTMLElement;

    expect(toggle(root)).toBeUndefined();
    expect(notesItem(root)?.classList.contains('max-md:hidden')).toBe(false);
    expect(document.activeElement).not.toBe(notesInput(root));
  });

  it('passes the item id and image file to the thumbnail', () => {
    const fixture = create();
    const thumbnail = fixture.debugElement.query(By.directive(ItemThumbnail)).componentInstance as ItemThumbnail;

    expect(thumbnail.itemId()).toBe('item-1');
    expect(thumbnail.imageFileId()).toBe('img-1');
    expect(fixture.nativeElement.querySelector('img')?.getAttribute('src')).toBe(
      '/api/Items/item-1/image?size=thumb&v=img-1'
    );
  });

  it('renders the placeholder without an image for a free-text line', () => {
    const root = create({ itemId: null, itemImageFileId: null }).nativeElement as HTMLElement;

    expect(root.querySelector('img')).toBeNull();
    expect(root.querySelector('ske-item-thumbnail nz-icon')).not.toBeNull();
  });
});
