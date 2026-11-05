import { Component, effect, inject, input, untracked } from '@angular/core';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { FruitDessertMessageType } from '@ske/models';
import { ErrorAlert } from '@ske/shared/errors';
import { injectClassReadOnly } from '@ske/shared/school-classes';
import { FruitDessertStore } from '../../../services/fruit-dessert.store';
import { FruitDessertRow } from './row/fruit-dessert-row';

@Component({
  selector: 'ske-school-class-overview-fruit-dessert-tab',
  imports: [ErrorAlert, FruitDessertRow, NzSpinComponent, NzEmptyComponent],
  providers: [FruitDessertStore],
  templateUrl: './fruit-dessert-tab.html',
  host: { class: 'block min-w-0 p-4 md:p-6' }
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
