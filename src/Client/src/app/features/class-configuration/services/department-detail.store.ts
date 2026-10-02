import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';
import { of, pipe, switchMap, tap } from 'rxjs';
import { DepartmentTemplateDto, SaveDepartmentRequest } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ConfigurationHttp } from './configuration.http';

export const departmentApiEvents = eventGroup({
  source: 'Department API',
  events: { saveSuccess: type<void>() },
});

export const DepartmentDetailState = signalStore(
  withState({ department: null as DepartmentTemplateDto | null }),
  withLoadingFeature('department'),
  withProblemDetailsFeature('department'),
  withProps(() => ({
    departmentHttp: inject(ConfigurationHttp),
    dispatcher: injectDispatch(departmentApiEvents),
  })),
  withMethods((store) => {
    const beginRequest = () => {
      store.clearDepartmentErrors();
      store.setDepartmentLoading();
    };
    const handleError = (error: unknown) => {
      store.handleDepartmentError(error);
      store.setDepartmentLoaded();
    };
    const loadDepartment = rxMethod<string | null>(
      pipe(
        tap(beginRequest),
        switchMap((id) => {
          if (id === null) {
            patchState(store, { department: null });
            store.setDepartmentLoaded();
            return of(null);
          }
          return store.departmentHttp.getDepartment(id).pipe(
            mapResponse({
              next: (department) => {
                patchState(store, { department });
                store.setDepartmentLoaded();
              },
              error: handleError,
            }),
          );
        }),
      ),
    );
    const saveDepartment = rxMethod<SaveDepartmentRequest>(
      pipe(
        tap(beginRequest),
        switchMap((request) =>
          store.departmentHttp.saveDepartment(request).pipe(
            mapResponse({
              next: (department) => {
                patchState(store, { department });
                store.setDepartmentLoaded();
                store.dispatcher.saveSuccess();
              },
              error: handleError,
            }),
          ),
        ),
      ),
    );
    return { loadDepartment, saveDepartment };
  }),
);
