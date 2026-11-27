import { TestBed } from '@angular/core/testing';
import { type LocationDto, type StockItemDto } from '@ske/models';
import { LocationsHttp } from '@ske/shared/locations';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { of } from 'rxjs';
import { StockMoveForm } from './stock-move-form';

describe('StockMoveForm', () => {
  const location = (id: string, name: string) => (<LocationDto>{ id, name, type: 'room', isDefault: false });
  const source = location('source', 'Sursă');
  const target = location('target', 'Depozit');
  const item = <StockItemDto>{ itemId: 'i1', locationId: source.id, quantity: 3 };

  function create() {
    TestBed.configureTestingModule({
      imports: [StockMoveForm],
      providers: [provideNzIconsTesting(),
        provideNzIcons([
          { name: 'icons:minus', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:circle-check', icon: '<svg viewBox="0 0 24 24"></svg>' },
          { name: 'icons:plus', icon: '<svg viewBox="0 0 24 24"></svg>' }
        ]),
        {
        provide: LocationsHttp,
        useValue: { getAll: () => of({ data: [source, target], sort: [], hasNextPage: false, nextCursor: null }) }
      }]
    });
    const fixture = TestBed.createComponent(StockMoveForm);
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('item', item);
    fixture.detectChanges();
    return fixture;
  }

  it('lists every location except the source as a radio card', () => {
    const fixture = create();

    const labels = [...fixture.nativeElement.querySelectorAll('label[nz-radio]')].map((l: Element) => l.textContent?.trim());
    expect(labels).toEqual(['Depozit']);
  });

  it('steps the quantity within 1..available stock', () => {
    const fixture = create();
    const quantity = fixture.componentInstance.stockMoveForm.quantity().value;
    const step = (fixture.componentInstance as unknown as { step(d: number): void }).step.bind(fixture.componentInstance);

    step(-1);
    expect(quantity()).toBe(1);
    step(1);
    step(1);
    step(1);
    expect(quantity()).toBe(3);
  });

  it('writes the chosen location card into the form', () => {
    const fixture = create();

    fixture.nativeElement.querySelector('label[nz-radio] input').click();
    fixture.detectChanges();

    expect(fixture.componentInstance.stockMoveForm.destination().value()).toBe(target);
  });
});
