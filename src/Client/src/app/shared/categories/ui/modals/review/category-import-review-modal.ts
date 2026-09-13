import { Component, effect, inject } from '@angular/core';
import {
  NZ_MODAL_DATA,
  NzModalFooterDirective,
  NzModalRef,
  NzModalTitleDirective
} from 'ng-zorro-antd/modal';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzInputDirective } from 'ng-zorro-antd/input';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { ErrorAlert } from '@ske/shared/errors';
import {
  CategoryImportReviewState,
  CategoryImportReviewTarget
} from '../../../services/category-import-review.store';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzButtonComponent,
    NzInputDirective,
    NzSpaceComponent,
    NzSpaceItemDirective,
    ErrorAlert
  ],
  selector: 'ske-category-import-review-modal',
  templateUrl: './category-import-review-modal.html',
  providers: [CategoryImportReviewState]
})
export class CategoryImportReviewModal {
  private readonly _modalRef = inject(NzModalRef);
  private readonly _data = inject<CategoryImportReviewTarget>(NZ_MODAL_DATA);

  public readonly store = inject(CategoryImportReviewState);

  private readonly _closeEffect = effect(() => {
    if (this.store.confirmation())
      this._modalRef.close(this.store.confirmation());
  });

  constructor() {
    this.store.load(this._data);
  }

  public close() {
    this._modalRef.close();
  }
}
