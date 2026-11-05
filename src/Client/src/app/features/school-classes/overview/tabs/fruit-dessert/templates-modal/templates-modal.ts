import { Component, effect, inject, signal, untracked } from '@angular/core';
import { FruitDessertMessageType, UpdateMessageTemplateRequest } from '@ske/models';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { FruitDessertTemplatesStore } from '../../../../services/fruit-dessert-templates.store';
import { TemplatePane } from './template-pane';

const TAB_TITLES: Record<FruitDessertMessageType, string> = {
  [FruitDessertMessageType.Reminder]: 'Reminder',
  [FruitDessertMessageType.Thanks]: 'Mulțumire'
};

@Component({
  selector: 'ske-fruit-dessert-templates-modal',
  templateUrl: './templates-modal.html',
  imports: [
    NzButtonComponent,
    NzModalFooterDirective,
    NzModalTitleDirective,
    NzSpinComponent,
    NzTabComponent,
    NzTabsComponent,
    TemplatePane
  ],
  providers: [FruitDessertTemplatesStore]
})
export class TemplatesModal {
  public readonly store = inject(FruitDessertTemplatesStore);

  protected readonly titles = TAB_TITLES;
  // The tab whose save is in flight/last failed, so the server error shows only there.
  protected readonly savedType = signal<FruitDessertMessageType | null>(null);

  private readonly _modalRef = inject(NzModalRef);
  private readonly _message = inject(NzMessageService);

  public constructor() {
    this.store.load();

    let wasSaving = false;
    effect(() => {
      const saving = this.store.saving();
      untracked(() => {
        if (wasSaving && !saving && !this.store.saveError()) this._message.success('Șablon salvat');
        wasSaving = saving;
      });
    });
  }

  public close(): void {
    this._modalRef.close();
  }

  protected save(type: FruitDessertMessageType, request: UpdateMessageTemplateRequest): void {
    this.savedType.set(type);
    this.store.save({ type, request });
  }
}
