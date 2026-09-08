import { Component, DestroyRef, effect, inject, signal, viewChild } from '@angular/core';
import { categoryApiEvents, CategoryDetailState, NEW_CATEGORY_ROUTE_ID } from '../../services/category-detail.store';
import { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '@ske/models';
import { NZ_MODAL_DATA, NzModalFooterDirective, NzModalRef, NzModalTitleDirective } from 'ng-zorro-antd/modal';
import { NzSpaceCompactComponent, NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { CategoryFormModel, Form } from './form';
import { isNil } from 'lodash-es';
import { Events } from '@ngrx/signals/events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NzMessageService } from 'ng-zorro-antd/message';
import { BehaviorSubject, filter, switchMap, tap } from 'rxjs';
import { NzDropdownDirective, NzDropdownMenuComponent } from 'ng-zorro-antd/dropdown';
import { NzMenuDirective, NzMenuItemComponent } from 'ng-zorro-antd/menu';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { ErrorAlert } from '@ske/shared/errors';

@Component({
  imports: [
    NzModalTitleDirective,
    NzModalFooterDirective,
    NzSpaceComponent,
    NzButtonComponent,
    Form,
    NzSpaceItemDirective,
    NzSpaceCompactComponent,
    NzDropdownMenuComponent,
    NzMenuDirective,
    NzMenuItemComponent,
    NzIconDirective,
    NzDropdownDirective,
    ErrorAlert
  ],
  selector: 'ske-category-detail-modal',
  styles: ``,
  templateUrl: './category-detail-modal.html',
  providers: [CategoryDetailState]
})
export class CategoryDetailModal {
  public readonly modalData = signal<CategoryDetailModalData>(inject(NZ_MODAL_DATA));
  public readonly store = inject(CategoryDetailState);

  private initialLoad = false;

  private readonly _nzModalRef = inject(NzModalRef);
  private readonly _storeEvents = inject(Events);
  private readonly _formComponent = viewChild(Form);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _nzMessageService = inject(NzMessageService);
  private readonly _close$ = new BehaviorSubject(false);

  private readonly _initialLoadEffectRef = effect(() => {
    if (this.initialLoad)
      return;

    const { category } = this.modalData();

    this.store.loadCategory(category?.id ?? NEW_CATEGORY_ROUTE_ID);
    this.initialLoad = true;
  });

  private readonly _saveSuccessRef = this._storeEvents.on(categoryApiEvents.saveSuccess)
    .pipe(
      takeUntilDestroyed(this._destroyRef),
      tap(() => this._nzMessageService.success('Categoria salvata cu succes!')),
      switchMap(() => this._close$),
      filter((shouldClose) => shouldClose),
      tap(() => this.close())
    )
    .subscribe();


  public close() {
    this._nzModalRef.destroy();
  }

  public async save(shouldClose: boolean = true) {
    const formComponent = this._formComponent();

    if (isNil(formComponent))
      return;

    const { isValid, formData } = await formComponent.submit();

    if (!isValid || isNil(formData))
      return;

    this.store.saveCategory(this.mapSaveRequest(formData));

    if(shouldClose)
      this._close$.next(true);
  }

  private mapSaveRequest(formData: CategoryFormModel): CreateCategoryRequest | UpdateCategoryRequest {
    return {
      name: formData.name,
      icon: formData.icon
    };
  }
}

type CategoryDetailModalData = {
  category: CategoryDto | null;
}
