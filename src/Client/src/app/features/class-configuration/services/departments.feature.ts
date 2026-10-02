import { inject } from '@angular/core';
import { mapResponse } from '@ngrx/operators';
import { patchState, signalStoreFeature, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap } from 'rxjs';
import { DepartmentTemplateDto } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { ConfigurationHttp } from './configuration.http';

export function withDepartments() {
  return signalStoreFeature(
    withState({ departments: [] as DepartmentTemplateDto[] }),
    withLoadingFeature('departments'),
    withProblemDetailsFeature('departments'),
    withProps(() => ({
      departmentsHttp: inject(ConfigurationHttp),
    })),
    withMethods((store) => {
      const beginRequest = () => {
        store.clearDepartmentsErrors();
        store.setDepartmentsLoading();
      };
      const handleError = (error: unknown) => {
        store.handleDepartmentsError(error);
        store.setDepartmentsLoaded();
      };
      const loadDepartments = rxMethod<void>(
        pipe(
          tap(beginRequest),
          switchMap(() =>
            store.departmentsHttp.getDepartments().pipe(
              mapResponse({
                next: (departments) => {
                  patchState(store, { departments });
                  store.setDepartmentsLoaded();
                },
                error: handleError,
              }),
            ),
          ),
        ),
      );
      const deleteDepartment = rxMethod<string>(
        pipe(
          tap(beginRequest),
          switchMap((id) =>
            store.departmentsHttp.deleteDepartment(id).pipe(
              mapResponse({
                next: () => {
                  patchState(store, {
                    departments: store.departments().filter((department) => department.id !== id),
                  });
                  loadDepartments();
                },
                error: handleError,
              }),
            ),
          ),
        ),
      );
      return { loadDepartments, deleteDepartment };
    }),
  );
}
