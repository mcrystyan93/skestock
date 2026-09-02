import { CreateSchoolClassRequest, SchoolClassDto, UpdateSchoolClassRequest } from '@ske/models';
import { patchState, signalStore, type, withMethods, withProps, withState } from '@ngrx/signals';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { SchoolClassesHttp } from '@ske/shared/school-classes';
import { isNil } from 'lodash-es';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { EMPTY, of, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { eventGroup, injectDispatch } from '@ngrx/signals/events';

type SchoolClassDetailState = { schoolClass: Partial<SchoolClassDto> };
const initialState: SchoolClassDetailState = { schoolClass: {} };
export const NEW_SCHOOL_CLASS_ROUTE_ID = 'new';
export const schoolClassApiEvents = eventGroup({
  source: 'School Class API',
  events: {
    saveSuccess: type<void>()
  }
});
export const SchoolClassDetailState = signalStore(
  withState(initialState),
  withLoadingFeature('schoolClass'),
  withProblemDetailsFeature('schoolClass'),
  withProps(() => ({
    schoolClassHttp: inject(SchoolClassesHttp),
    dispatcher: injectDispatch(schoolClassApiEvents)
  })),
  withMethods((store) => {
    const getCurrentSchoolClassId = () => {
      const id = store.schoolClass().id;

      if (isNil(id)) {
        store.handleSchoolClassError({ title: 'School class ID missing', status: 400 });
        store.setSchoolClassLoaded();
        return null;
      }

      return id;
    };

    const loadSchoolClass = rxMethod<string>(
      pipe(
        tap(() => {
          store.setSchoolClassLoading();
          store.clearSchoolClassErrors();
        }),
        switchMap((id) => {
          if (id === NEW_SCHOOL_CLASS_ROUTE_ID) {
            patchState(store, {
              schoolClass: {}
            });
            store.setSchoolClassLoaded();
            return of(null);
          }

          if (isNil(id) || id === '') {
            store.handleSchoolClassError({ title: 'School class ID missing', status: 400 });
            store.setSchoolClassLoaded();
            return of(null);
          }

          return store.schoolClassHttp.getById(id).pipe(
            mapResponse({
              next: (schoolClass) => {
                patchState(store, { schoolClass });
                store.setSchoolClassLoaded();
              },
              error: (error) => {
                store.handleSchoolClassError(error);
                store.setSchoolClassLoaded();
              }
            })
          );
        })
      )
    );

    const createSchoolClass = rxMethod<CreateSchoolClassRequest>(
      pipe(
        tap(() => {
          store.setSchoolClassLoading();
          store.clearSchoolClassErrors();
        }),
        switchMap((request) =>
          store.schoolClassHttp.create(request).pipe(
            mapResponse({
              next: (schoolClass) => {
                patchState(store, { schoolClass });
                store.dispatcher.saveSuccess();
                store.setSchoolClassLoaded();
              },
              error: (error) => {
                store.handleSchoolClassError(error);
                store.setSchoolClassLoaded();
              }
            })
          )
        )
      )
    );

    const updateSchoolClass = rxMethod<UpdateSchoolClassRequest>(
      pipe(
        tap(() => {
          store.setSchoolClassLoading();
          store.clearSchoolClassErrors();
        }),
        switchMap((request) => {
          const id = getCurrentSchoolClassId();

          if (isNil(id)) {
            return EMPTY;
          }

          return store.schoolClassHttp.update(id, request).pipe(
            mapResponse({
              next: (schoolClass) => {
                patchState(store, { schoolClass });
                store.dispatcher.saveSuccess();
                store.setSchoolClassLoaded();
              },
              error: (error) => {
                store.handleSchoolClassError(error);
                store.setSchoolClassLoaded();
              }
            })
          );
        })
      )
    );

    const saveSchoolClass = (request: CreateSchoolClassRequest | UpdateSchoolClassRequest) => {
      if (store.schoolClass().id) {
        updateSchoolClass(request);
        return;
      }

      createSchoolClass(request);
    };

    return { loadSchoolClass, saveSchoolClass };
  })
);
