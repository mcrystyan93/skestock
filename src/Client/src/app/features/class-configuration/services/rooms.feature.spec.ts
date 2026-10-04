import { TestBed } from '@angular/core/testing';
import { RoomConfigurationDto, SaveRoomConfigurationRequest } from '@ske/models';
import { of, throwError } from 'rxjs';
import { ConfigurationHttp } from './configuration.http';
import { ConfigurationStore } from './configuration.store';

describe('ConfigurationStore room configuration', () => {
  let http: {
    getRoomConfiguration: ReturnType<typeof vi.fn>;
    saveRoomConfiguration: ReturnType<typeof vi.fn>;
  };

  const initialConfiguration: RoomConfigurationDto = {
    room4SeatCount: 0,
    room2SeatCount: 0,
    room6SeatCount: 0,
  };
  const configuration: RoomConfigurationDto = {
    room4SeatCount: 120,
    room2SeatCount: 30,
    room6SeatCount: 40,
  };
  const request: SaveRoomConfigurationRequest = {
    room4SeatCount: 140,
    room2SeatCount: 32,
    room6SeatCount: 45,
  };

  beforeEach(() => {
    http = {
      getRoomConfiguration: vi.fn(),
      saveRoomConfiguration: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [ConfigurationStore, { provide: ConfigurationHttp, useValue: http }],
    });
  });

  it('loads the room configuration and clears loading', () => {
    http.getRoomConfiguration.mockReturnValue(of(configuration));
    const store = TestBed.inject(ConfigurationStore);

    store.loadRooms();

    expect(store.roomConfiguration()).toEqual(configuration);
    expect(store.roomsLoading()).toBe(false);
  });

  it('saves then reloads the confirmed room configuration', () => {
    http.getRoomConfiguration.mockReturnValue(of(configuration));
    http.saveRoomConfiguration.mockReturnValue(of(request));
    const store = TestBed.inject(ConfigurationStore);

    store.saveRooms(request);

    expect(http.saveRoomConfiguration).toHaveBeenCalledWith(request);
    expect(http.getRoomConfiguration).toHaveBeenCalledOnce();
    expect(store.roomConfiguration()).toEqual(configuration);
    expect(store.roomsLoading()).toBe(false);
  });

  it('keeps the last confirmed values when saving fails', () => {
    http.getRoomConfiguration.mockReturnValue(of(configuration));
    http.saveRoomConfiguration.mockReturnValue(
      throwError(() => ({ status: 400, title: 'Validation error' })),
    );
    const store = TestBed.inject(ConfigurationStore);
    store.loadRooms();

    store.saveRooms(request);

    expect(store.roomConfiguration()).toEqual(configuration);
    expect(store.roomsProblemDetail()).toEqual({ status: 400, title: 'Validation error' });
    expect(store.roomsLoading()).toBe(false);
    expect(http.getRoomConfiguration).toHaveBeenCalledOnce();
  });

  it('starts with zero counts without touching invitations', () => {
    const store = TestBed.inject(ConfigurationStore);

    expect(store.roomConfiguration()).toEqual(initialConfiguration);
    expect(store.invitationCount()).toBe(0);
    expect(store.roomsLoading()).toBe(false);
  });
});
