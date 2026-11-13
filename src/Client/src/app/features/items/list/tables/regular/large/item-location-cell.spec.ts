import { TestBed } from '@angular/core/testing';
import { LocationsHttp } from '@ske/shared/locations';
import { ItemLocationCell } from './item-location-cell';

describe('ItemLocationCell', () => {
  function setup(locationId: string | null) {
    TestBed.configureTestingModule({
      imports: [ItemLocationCell],
      providers: [{ provide: LocationsHttp, useValue: { getAll: vi.fn(), getById: vi.fn(), getDefault: vi.fn() } }]
    });

    const fixture = TestBed.createComponent(ItemLocationCell);
    fixture.componentRef.setInput('locationId', locationId);
    fixture.componentRef.setInput('locationName', locationId ? 'Depozit' : null);
    const emitted: (string | null)[] = [];
    fixture.componentInstance.locationChange.subscribe((id) => emitted.push(id));

    return { cell: fixture.componentInstance as unknown as { select(l: { id: string } | null): void }, emitted };
  }

  it('emits the new id when another location is selected', () => {
    const { cell, emitted } = setup('a');

    cell.select({ id: 'b' });

    expect(emitted).toEqual(['b']);
  });

  it('emits null when the location is cleared', () => {
    const { cell, emitted } = setup('a');

    cell.select(null);

    expect(emitted).toEqual([null]);
  });

  it('does not emit when the selection is unchanged', () => {
    const { cell, emitted } = setup('a');

    cell.select({ id: 'a' });

    expect(emitted).toEqual([]);
  });
});
