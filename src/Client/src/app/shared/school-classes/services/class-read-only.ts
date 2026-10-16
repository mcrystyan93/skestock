import { inject, InjectionToken, signal, type Signal } from '@angular/core';

/**
 * Provided by the school class overview page: true while the class is closed, so every action that
 * changes the class (stock, orders, imports, configuration) is hidden or disabled. Outside the overview
 * there is no provider and components fall back to "not read-only".
 */
export const CLASS_READ_ONLY = new InjectionToken<Signal<boolean>>('CLASS_READ_ONLY');

export function injectClassReadOnly(): Signal<boolean> {
  return inject(CLASS_READ_ONLY, { optional: true }) ?? signal(false).asReadonly();
}
