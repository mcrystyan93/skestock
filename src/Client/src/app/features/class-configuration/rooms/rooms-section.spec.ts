import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RoomConfigurationDto } from '@ske/models';
import { ConfigurationStore } from '../services/configuration.store';
import { RoomsCard } from './rooms-card';
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

  const card = () =>
    fixture.debugElement.query(By.directive(RoomsCard)).componentInstance as RoomsCard;

  const submitCardForm = async () => {
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();
  };

  it('submits all three values through the configuration store', async () => {
    const form = card().roomsForm;
    form.room4SeatCount().value.set(100);
    form.room2SeatCount().value.set(20);
    form.room6SeatCount().value.set(30);
    fixture.detectChanges();

    await submitCardForm();

    expect(store.saveRooms).toHaveBeenCalledWith({
      room4SeatCount: 100,
      room2SeatCount: 20,
      room6SeatCount: 30,
    });
  });

  it('does not send an invalid configuration to the store', async () => {
    card().roomsForm.room4SeatCount().value.set(201);
    fixture.detectChanges();

    await submitCardForm();

    expect(store.saveRooms).not.toHaveBeenCalled();
  });

  it('does not submit while a store request is active', async () => {
    store.roomsLoading.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(store.saveRooms).not.toHaveBeenCalled();
  });
});
