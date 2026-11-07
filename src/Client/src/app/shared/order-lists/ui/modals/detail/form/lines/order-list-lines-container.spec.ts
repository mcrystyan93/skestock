import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { ClassStatisticsHttp } from '@ske/shared/class-statistics';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { of } from 'rxjs';
import { OrderListLinesContainer } from './order-list-lines-container';
import type { OrderListLineFormModel } from './order-list-line';

const icon = (name: string) => ({ name, icon: '<svg viewBox="0 0 1 1"><path d="M0 0h1v1H0z"/></svg>' });

function createLine(clientKey: string, overrides: Partial<OrderListLineFormModel> = {}): OrderListLineFormModel {
  return {
    id: null,
    itemId: `item-${clientKey}`,
    itemImageFileId: null,
    productName: `Produs ${clientKey}`,
    quantity: 1,
    unit: 'buc',
    notes: '',
    clientKey,
    ...overrides
  };
}

describe('OrderListLinesContainer', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideNzIconsTesting(),
        provideNzIcons(
          ['trash-can', 'minus', 'plus', 'pencil', 'circle-exclamation', 'box'].map((n) => icon(`icons:${n}`))
        ),
        { provide: ClassStatisticsHttp, useValue: { getItemsPurchaseHistory: () => of([]) } }
      ]
    });
  });

  function create(initial: OrderListLineFormModel[]) {
    const model = signal(initial);
    const fixture = TestBed.createComponent(OrderListLinesContainer);
    fixture.componentRef.setInput('lines', TestBed.runInInjectionContext(() => form(model)));
    fixture.detectChanges();
    return { fixture, model };
  }

  const cards = (root: HTMLElement) => root.querySelectorAll('ske-order-list-line').length;

  it('keeps rendering when the lines array is merged with an added line', () => {
    const { fixture, model } = create([createLine('a'), createLine('b')]);
    const root = fixture.nativeElement as HTMLElement;
    expect(cards(root)).toBe(2);

    model.update((lines) => [...lines, createLine('c')]);

    expect(() => fixture.detectChanges()).not.toThrow();
    expect(cards(root)).toBe(3);
  });

  it('keeps rendering when the lines array is replaced', () => {
    const { fixture, model } = create([createLine('a'), createLine('b')]);
    const root = fixture.nativeElement as HTMLElement;

    model.set([createLine('b'), createLine('d')]);

    expect(() => fixture.detectChanges()).not.toThrow();
    expect(cards(root)).toBe(2);

    model.set([]);
    fixture.detectChanges();
    expect(cards(root)).toBe(0);
  });
});
