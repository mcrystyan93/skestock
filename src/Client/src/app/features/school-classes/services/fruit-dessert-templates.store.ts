import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withProps, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { mapResponse } from '@ngrx/operators';
import { exhaustMap, pipe, switchMap, tap } from 'rxjs';
import {
  FruitDessertMessageType,
  MessagePlaceholderDto,
  MessageTemplateDto,
  UpdateMessageTemplateRequest
} from '@ske/models';
import { ErrorMessageService, withProblemDetailsFeature } from '@ske/shared/errors';
import { withLoadingFeature } from '@ske/shared/loader';
import { FruitDessertTemplatesHttp } from './fruit-dessert-templates.http';

type FruitDessertTemplatesState = {
  templates: MessageTemplateDto[];
  placeholders: MessagePlaceholderDto[];
  saving: boolean;
};

/** Not `providedIn: 'root'`: provided by the templates modal so its state dies with it. */
export const FruitDessertTemplatesStore = signalStore(
  withState<FruitDessertTemplatesState>({ templates: [], placeholders: [], saving: false }),
  withLoadingFeature('templates'),
  withProblemDetailsFeature('templates'),
  withProblemDetailsFeature('save'),
  withProps(() => ({ _http: inject(FruitDessertTemplatesHttp), _errors: inject(ErrorMessageService) })),
  withComputed((store) => ({
    /** Message of the last failed save (400 validation or 409), or null. */
    saveError: computed(() => {
      const problem = store.saveValidationErrors() ?? store.saveProblemDetail();
      return problem ? (problem.detail ?? store._errors.resolveProblem(problem)) : null;
    })
  })),
  withMethods((store) => ({
    load: rxMethod<void>(
      pipe(
        tap(() => {
          store.clearTemplatesErrors();
          store.setTemplatesLoading();
        }),
        switchMap(() =>
          store._http.get().pipe(
            mapResponse({
              next: ({ templates, placeholders }) => {
                patchState(store, { templates, placeholders });
                store.setTemplatesLoaded();
              },
              error: (error) => {
                store.handleTemplatesError(error);
                store.setTemplatesLoaded();
              }
            })
          )
        )
      )
    ),

    save: rxMethod<{ type: FruitDessertMessageType; request: UpdateMessageTemplateRequest }>(
      pipe(
        tap(() => {
          store.clearSaveErrors();
          patchState(store, { saving: true });
        }),
        exhaustMap(({ type, request }) =>
          store._http.update(type, request).pipe(
            mapResponse({
              next: () => {
                // The PUT returns 204, so the local copy is patched with what was sent.
                patchState(store, {
                  templates: store.templates().map(t =>
                    t.type === type ? { ...t, ...request, lastModified: new Date().toISOString() } : t
                  ),
                  saving: false
                });
              },
              error: (error) => {
                patchState(store, { saving: false });
                store.handleSaveError(error);
              }
            })
          )
        )
      )
    )
  }))
);
