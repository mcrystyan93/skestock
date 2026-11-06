import { Component, effect, inject, input, untracked } from '@angular/core';
import { FruitDessertMessageType } from '@ske/models';
import { LayoutBreakpoint } from '@ske/shared/directives';
import { ErrorAlert } from '@ske/shared/errors';
import { injectClassReadOnly } from '@ske/shared/school-classes';
import { FruitDessertStore } from '../../../services/fruit-dessert.store';
import { FruitDessertListSmall } from './list-small/fruit-dessert-list-small';
import { FruitDessertTable } from './table/fruit-dessert-table';

@Component({
  selector: 'ske-school-class-overview-fruit-dessert-tab',
  imports: [ErrorAlert, FruitDessertTable, FruitDessertListSmall, LayoutBreakpoint],
  providers: [FruitDessertStore],
  templateUrl: './fruit-dessert-tab.html',
  host: { class: 'flex flex-col grow absolute inset-0 gap-2 mt-4' }
})
export class FruitDessertTab {
  public readonly classId = input<string | null>(null);

  protected readonly store = inject(FruitDessertStore);
  protected readonly readOnly = injectClassReadOnly();

  private readonly _classIdEffectRef = effect(() => {
    const classId = this.classId();

    if (classId) untracked(() => this.store.load(classId));
  });

  protected send(entryId: string, type: FruitDessertMessageType) {
    // The store ignores a send while one is in flight; opening a tab first would leave an orphan blank tab.
    if (this.store.sending()) return;

    // Must open synchronously inside the click: after the awaited POST browsers block the popup. No `noopener` feature
    // string, or window.open returns null and the tab cannot be navigated afterwards.
    const win = window.open('', '_blank');
    if (win) win.opener = null;

    this.store.sendMessage({ entryId, type, win });
  }
}
