import { effect, linkedSignal, untracked, WritableSignal } from '@angular/core';

export type CheckedRow = { id: string; checked: boolean };
export type CheckedRowsModel = { rows: CheckedRow[] };

/** A form model with one checkbox row per id, checked when the id is in `selectedIds`. */
export function checkedRowsModel(ids: () => string[], selectedIds: () => string[]): WritableSignal<CheckedRowsModel> {
  return linkedSignal<CheckedRowsModel>(() => {
    const selected = new Set(selectedIds());

    return { rows: ids().map((id) => ({ id, checked: selected.has(id) })) };
  });
}

/** Emits the checked ids whenever they differ from the ids the parent currently holds. */
export function emitCheckedIdsOnChange(
  model: () => CheckedRowsModel,
  currentIds: () => string[],
  emit: (ids: string[]) => void
) {
  return effect(() => {
    const ids = model().rows.filter((row) => row.checked).map((row) => row.id);

    untracked(() => {
      const current = currentIds();

      if (ids.length !== current.length || ids.some((id) => !current.includes(id)))
        emit(ids);
    });
  });
}
