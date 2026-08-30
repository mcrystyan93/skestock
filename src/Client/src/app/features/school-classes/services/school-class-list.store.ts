import { signalStore, withMethods, withState } from '@ngrx/signals';
import { withSchoolClassCollection } from '@ske/shared/school-classes';

type SchoolClassListState = {};
const initialState: SchoolClassListState = {};

export const SchoolClassListState = signalStore(
  withState(initialState),
  withSchoolClassCollection(),
  withMethods((store) => {
    const reload = () => {
      store.load(store.filter());
    };

    return { reload };
  })
);
