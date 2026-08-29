import { patchState, signalStoreFeature, withMethods, withState } from '@ngrx/signals';
// 1. Define helper types to construct exact literal keys
type LoadingKey<P extends string> = `${P}Loading`;
type SetLoadingKey<P extends string> = `set${Capitalize<P>}Loading`;
type SetLoadedKey<P extends string> = `set${Capitalize<P>}Loaded`;

export function withLoadingFeature<P extends string>(prefix: P) {
  // Generate unique dynamic property names
  const loadingKey = `${prefix}Loading` as LoadingKey<P>;

  const capitalized = (prefix.charAt(0).toUpperCase() + prefix.slice(1)) as Capitalize<P>;
  const setLoadingKey = `set${capitalized}Loading` as SetLoadingKey<P>;
  const setLoadedKey = `set${capitalized}Loaded` as SetLoadedKey<P>;

  return signalStoreFeature(
    // 1. Dynamic State
    withState({
      [loadingKey]: false
    } as Record<LoadingKey<P>, boolean>),
    // 2. Dynamic Methods
    withMethods((store) => {
      const methods = {
        [setLoadingKey]() {
          patchState(store, { [loadingKey]: true } as any);
        },
        [setLoadedKey]() {
          patchState(store, { [loadingKey]: false } as any);
        }
      };

      // Asserting as a mapped interface bypasses the index signature constraint
      return methods as {
        [K in SetLoadingKey<P> | SetLoadedKey<P>]: () => void;
      };
    })
  );
}
