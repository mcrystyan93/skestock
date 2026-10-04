import {ComponentFixture, TestBed} from '@angular/core/testing';
import {RoomConfigurationDto} from '@ske/models';
import {RoomsCard} from './rooms-card';

describe('RoomsCard', () => {
  let fixture: ComponentFixture<RoomsCard>;

  beforeEach(() => {
    TestBed.configureTestingModule({imports: [RoomsCard]});
    fixture = TestBed.createComponent(RoomsCard);
    fixture.componentRef.setInput('configuration', {
      room4SeatCount: 1,
      room2SeatCount: 2,
      room6SeatCount: 3,
    });
    fixture.detectChanges();
  });

  it('renders the three room inputs inside a card with a save button', () => {
    expect(fixture.nativeElement.querySelectorAll('nz-input-number')).toHaveLength(3);
    expect(fixture.nativeElement.textContent).toContain('Sala 4 — principală');
    expect(fixture.nativeElement.textContent).toContain('Sala 2 — secundară');
    expect(fixture.nativeElement.textContent).toContain('Sala 6 — secundară');
    expect(fixture.nativeElement.querySelector('button[type="submit"]').textContent).toContain(
      'Salvează',
    );
  });

  it.each([
    ['room4SeatCount', 0],
    ['room4SeatCount', 200],
    ['room2SeatCount', 0],
    ['room2SeatCount', 200],
    ['room6SeatCount', 0],
    ['room6SeatCount', 200],
  ] as const)('accepts %s=%s', async (field, value) => {
    fixture.componentInstance.roomsForm[field]().value.set(value);

    const result = await fixture.componentInstance.submit();

    expect(result.isValid).toBe(true);
  });

  it('returns a complete payload with all room counts', async () => {
    const form = fixture.componentInstance.roomsForm;
    form.room4SeatCount().value.set(12);
    form.room2SeatCount().value.set(34);
    form.room6SeatCount().value.set(56);

    const result = await fixture.componentInstance.submit();

    expect(result.formData).toEqual({room4SeatCount: 12, room2SeatCount: 34, room6SeatCount: 56});
  });

  it.each([
    ['room4SeatCount', -1],
    ['room4SeatCount', 201],
    ['room4SeatCount', 2.5],
    ['room4SeatCount', Number.NaN],
    ['room2SeatCount', -1],
    ['room2SeatCount', 201],
    ['room2SeatCount', 2.5],
    ['room2SeatCount', Number.NaN],
    ['room6SeatCount', -1],
    ['room6SeatCount', 201],
    ['room6SeatCount', 2.5],
    ['room6SeatCount', Number.NaN],
  ] as const)('rejects %s=%s', async (field, value) => {
    const form = fixture.componentInstance.roomsForm;
    form[field]().value.set(value);

    const result = await fixture.componentInstance.submit();

    expect(result.isValid).toBe(false);
    expect(result.formData).toBeNull();
  });

  it.each([
    ['room4SeatCount', 'room-4-seat-count'],
    ['room2SeatCount', 'room-2-seat-count'],
    ['room6SeatCount', 'room-6-seat-count'],
  ] as const)('requires a value for %s', async (field, inputId) => {
    const input = fixture.nativeElement.querySelector(`#${inputId}`) as HTMLInputElement;
    input.value = '';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const result = await fixture.componentInstance.submit();

    expect(result.isValid).toBe(false);
    expect(
      fixture.componentInstance.roomsForm[field]()
        .errors()
        .map((error) => error.kind),
    ).toContain('required');
  });
});
