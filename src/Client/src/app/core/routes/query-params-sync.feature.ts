import { signalStoreFeature, withHooks } from '@ngrx/signals';
import { effect, inject, untracked } from '@angular/core';
import { QueryParamState } from './query-param-state';
import { isNil } from 'lodash-es';

export function withQueryParamsSync<T>(options: {
  key: string;
  getValue: (store: any) => () => T;
  setValue: (store: any, value: T) => void;
  parse: (raw: string | null) => T;
  serialize: (value: T) => string;
}) {
  return signalStoreFeature(
    withHooks({
      onInit(store) {
        const qp = inject(QueryParamState);
        const urlValue = qp.param(options.key, null);

        const initial = urlValue();
        if (initial !== null) {
          options.setValue(store, options.parse(initial));
        }

        effect(() => {
          const value = options.getValue(store)();

          if (!isNil(value))
            qp.set(options.key, options.serialize(value));
        });

        effect(() => {
          const parsed = options.parse(urlValue());
          if (parsed !== untracked(options.getValue(store))) {
            options.setValue(store, parsed);
          }
        });
      }
    })
  );
}
