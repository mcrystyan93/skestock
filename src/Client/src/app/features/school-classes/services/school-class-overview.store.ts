import { patchState, signalStore, withMethods, withProps, withState } from '@ngrx/signals';
import { SchoolClassDto } from '@ske/models';
import { withLoadingFeature } from '@ske/shared/loader';
import { withProblemDetailsFeature } from '@ske/shared/errors';
import { inject } from '@angular/core';
import { SchoolClassesHttp } from '@ske/shared/school-classes';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { exhaustMap, filter, finalize, pipe, switchMap, tap } from 'rxjs';
import { mapResponse } from '@ngrx/operators';
import { withSchoolClassSummaryFeature } from './school-class-summary.feature';
import { withGoodReceiptsFeature } from '@ske/shared/goods-receipts';
import { withQueryParamsSync } from '@ske/routes';

type SchoolClassOverviewState = {
  schoolClass: Partial<SchoolClassDto>;
  selectedTabIndex: number;
  receiptIdQueryParam: string | null;
  savingDepartmentId: string | null;
  departmentsMessage: string | null;
};
const initialState: SchoolClassOverviewState = {
  schoolClass: {},
  selectedTabIndex: 0,
  receiptIdQueryParam: null,
  savingDepartmentId: null,
  departmentsMessage: null
};

export const SchoolClassOverviewStore = signalStore(
  withState(initialState),
  withLoadingFeature('schoolClass'),
  withProblemDetailsFeature('schoolClass'),
  withLoadingFeature('departments'),
  withProblemDetailsFeature('departments'),
  withSchoolClassSummaryFeature(),
  withGoodReceiptsFeature(),
  withQueryParamsSync({
    key: 'tab',
    getValue: (store) => () => store.selectedTabIndex(),
    setValue: (store, value) => patchState(store, { selectedTabIndex: value }),
    parse: (raw) => {
      const parsed = parseInt(raw ?? '0', 10);
      return isNaN(parsed) ? 0 : parsed;
    },
    serialize: (value) => value.toString()
  }),
  withQueryParamsSync({
    key: 'receiptId',
    getValue: (store) => () => store.receiptIdQueryParam(),
    setValue: (store, value) => patchState(store, { receiptIdQueryParam: value }),
    parse: (raw) => raw ?? null,
    serialize: (value) => value ?? ''
  }),
  withProps(() => ({
    schoolClassHttp: inject(SchoolClassesHttp)
  })),
  withMethods((store) => {
    const load = rxMethod<string>(
      pipe(
        tap((id) => {
          if (store.schoolClass().id !== id) {
            store.clearDepartmentsErrors();
            patchState(store, { departmentsMessage: null });
          }
          store.clearSchoolClassErrors();
          store.setSchoolClassLoading();
        }),
        switchMap((id) =>
          store.schoolClassHttp.getById(id).pipe(
            mapResponse({
              next: (result) => {
                patchState(store, {
                  schoolClass: result
                });

                store.setSchoolClassLoaded();

                store.loadSummary(result.id);
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

    const initializeConfiguration = rxMethod<void>(
      pipe(
        filter(() => !!store.schoolClass().id && !store.departmentsLoading()),
        exhaustMap(() => {
          const classId = store.schoolClass().id!;
          store.clearDepartmentsErrors();
          store.setDepartmentsLoading();
          patchState(store, { departmentsMessage: null });

          return store.schoolClassHttp.initializeConfiguration(classId).pipe(
            switchMap(() => store.schoolClassHttp.getById(classId)),
            mapResponse({
              next: (schoolClass) => {
                if (store.schoolClass().id !== classId) return;
                patchState(store, {
                  schoolClass,
                  departmentsMessage: 'Departamentele și numărul de invitații au fost copiate în clasă.'
                });
              },
              error: (error) => {
                if (store.schoolClass().id === classId) store.handleDepartmentsError(error);
              }
            }),
            finalize(() => store.setDepartmentsLoaded())
          );
        })
      )
    );

    const saveResponsiblePerson = rxMethod<{ departmentId: string; responsiblePerson: string }>(
      pipe(
        filter(() => !!store.schoolClass().id && !store.departmentsLoading()),
        exhaustMap(({ departmentId, responsiblePerson }) => {
          const classId = store.schoolClass().id!;
          const value = responsiblePerson.trim();
          store.clearDepartmentsErrors();
          store.setDepartmentsLoading();
          patchState(store, { savingDepartmentId: departmentId, departmentsMessage: null });

          return store.schoolClassHttp.updateDepartmentResponsiblePerson(classId, departmentId, value).pipe(
            mapResponse({
              next: () => {
                if (store.schoolClass().id !== classId) return;
                patchState(store, ({ schoolClass }) => ({
                  schoolClass: {
                    ...schoolClass,
                    departments: schoolClass.departments?.map(department =>
                      department.id === departmentId ? { ...department, responsiblePerson: value } : department
                    )
                  },
                  departmentsMessage: 'Responsabilul a fost salvat.'
                }));
              },
              error: (error) => {
                if (store.schoolClass().id === classId) store.handleDepartmentsError(error);
              }
            }),
            finalize(() => {
              patchState(store, { savingDepartmentId: null });
              store.setDepartmentsLoaded();
            })
          );
        })
      )
    );

    const clearDepartmentsFeedback = () => {
      store.clearDepartmentsErrors();
      patchState(store, { departmentsMessage: null });
    };

    const selectTab = (tabIndex: number) => patchState(store, { selectedTabIndex: tabIndex });

    return { load, selectTab, initializeConfiguration, saveResponsiblePerson, clearDepartmentsFeedback };
  })
);
