import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { NZ_MODAL_DATA } from 'ng-zorro-antd/modal';
import { ItemImportReviewEditableLine, ItemImportReviewState } from '../../../services/item-import-review.store';
import { ItemImportReviewTable } from './item-import-review-table';

@Component({
  imports: [FormsModule, NzAlertComponent, NzButtonComponent, NzDividerComponent, NzModalFooterDirective,
    NzModalTitleDirective, NzSpaceComponent, NzSpaceItemDirective, NzTagComponent, ItemImportReviewTable],
  selector: 'ske-item-import-review-modal',
  templateUrl: './item-import-review-modal.html',
  providers: [ItemImportReviewState]
})
export class ItemImportReviewModal {
  private readonly _modalRef = inject(NzModalRef);
  private readonly _data = inject<{ importId: string }>(NZ_MODAL_DATA);
  public readonly store = inject(ItemImportReviewState);
  public readonly lines = signal<ItemImportReviewEditableLine[]>([]);
  public readonly validationMessage = signal<string | null>(null);

  private readonly _loadEffect = effect(() => {
    const review = this.store.review();
    if (review)
      this.lines.set(this.store.lines());
  });

  private readonly _closeEffect = effect(() => {
    if (this.store.confirmation())
      this._modalRef.close(this.store.confirmation());
  });

  constructor() {
    this.store.load(this._data.importId);
  }

  public save() {
    if (this.lines().some((line) => !line.name.trim() || !line.categoryName.trim() || !line.unit.trim())) {
      this.validationMessage.set('Completati denumirea, categoria si unitatea pentru fiecare rand.');
      return;
    }

    this.validationMessage.set(null);
    this.store.confirm(this.lines());
  }

  public close() {
    this._modalRef.close();
  }
}
