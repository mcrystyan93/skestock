import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStoreFeature, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { RoomConfigurationDto, SaveRoomConfigurationRequest } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ConfigurationHttp } from './configuration.http';

const EMPTY_ROOM_CONFIGURATION: RoomConfigurationDto = {
  room4SeatCount: 0,
  room2SeatCount: 0,
  room6SeatCount: 0,
};

export function withRooms() {
  return signalStoreFeature(
    withState({ roomConfiguration: EMPTY_ROOM_CONFIGURATION }),
    withLoadingFeature('rooms'),
    withProblemDetailsFeature('rooms'),
    withProps(() => ({ roomsHttp: inject(ConfigurationHttp) })),
    withMethods((store) => {
      const beginRequest = () => {
        store.clearRoomsErrors();
        store.setRoomsLoading();
      };
      const applyConfiguration = (configuration: RoomConfigurationDto) => {
        patchState(store, { roomConfiguration: configuration });
        store.setRoomsLoaded();
      };
      const handleError = (error: unknown) => {
        store.handleRoomsError(error);
        store.setRoomsLoaded();
      };
      const loadRooms = rxMethod<void>(
        pipe(
          tap(beginRequest),
          switchMap(() =>
            store.roomsHttp
              .getRoomConfiguration()
              .pipe(mapResponse({ next: applyConfiguration, error: handleError })),
          ),
        ),
      );
      const saveRooms = rxMethod<SaveRoomConfigurationRequest>(
        pipe(
          tap(beginRequest),
          switchMap((request) =>
            store.roomsHttp.saveRoomConfiguration(request).pipe(
              switchMap(() => store.roomsHttp.getRoomConfiguration()),
              mapResponse({ next: applyConfiguration, error: handleError }),
            ),
          ),
        ),
      );

      return { loadRooms, saveRooms };
    }),
  );
}
