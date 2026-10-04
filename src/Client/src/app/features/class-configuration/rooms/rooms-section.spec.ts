import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomConfigurationDto } from '@ske/models';
import { ConfigurationStore } from '../services/configuration.store';
import { RoomsSection } from './rooms-section';

describe('RoomsSection', () => {
  let fixture: ComponentFixture<RoomsSection>;
  let store: {
    roomConfiguration: WritableSignal<RoomConfigurationDto>;
    roomsLoading: WritableSignal<boolean>;
    roomsProblemDetail: WritableSignal<null>;
    roomsValidationErrors: WritableSignal<null>;
    saveRooms: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    store = {
      roomConfiguration: signal({ room4SeatCount: 1, room2SeatCount: 2, room6SeatCount: 3 }),
      roomsLoading: signal(false),
      roomsProblemDetail: signal(null),
      roomsValidationErrors: signal(null),
      saveRooms: vi.fn(),
    };
    TestBed.configureTestingModule({
      imports: [RoomsSection],
      providers: [{ provide: ConfigurationStore, useValue: store }],
    });
    fixture = TestBed.createComponent(RoomsSection);
    fixture.detectChanges();
  });

  it('submits all three values through the configuration store', async () => {
    const inputs = fixture.nativeElement.querySelectorAll(
      'input[type="number"]',
    ) as NodeListOf<HTMLInputElement>;
    inputs[0].value = '100';
    inputs[0].dispatchEvent(new Event('input'));
    inputs[1].value = '20';
    inputs[1].dispatchEvent(new Event('input'));
    inputs[2].value = '30';
    inputs[2].dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();

    expect(store.saveRooms).toHaveBeenCalledWith({
      room4SeatCount: 100,
      room2SeatCount: 20,
      room6SeatCount: 30,
    });
  });

  it('does not send an invalid configuration to the store', async () => {
    const input = fixture.nativeElement.querySelector('#room-4-seat-count') as HTMLInputElement;
    input.value = '201';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();

    expect(store.saveRooms).not.toHaveBeenCalled();
  });

  it('does not submit while a store request is active', async () => {
    store.roomsLoading.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(store.saveRooms).not.toHaveBeenCalled();
  });
});
