import { Component, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { CategoryListState } from '../services/category-list.store';
import { FilterContainer } from './filter/filter-container';
import { Table } from './table/table';
import { CategoryDto, GetAllCategoriesRequest } from '@ske/models';
import { NzModalService } from 'ng-zorro-antd/modal';
import { Header } from './header/header';
import { CategoryDetailModal } from '@ske/shared/categories';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SignalRGroupManagerStore } from '@ske/signalr';
import { ErrorAlert } from '@ske/shared/errors';

@Component({
  imports: [
    FilterContainer,
    Table,
    Header,
    ErrorAlert
  ],
  selector: 'ske-categories-page',
  templateUrl: './categories.page.html',
  providers: [CategoryListState, NzModalService],
  host: {
    class: 'flex flex-col grow gap-4'
  }
})
export class CategoriesPage implements OnInit, OnDestroy{
  public readonly store = inject(CategoryListState);

  private readonly _modalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _signalRGroupManager = inject(SignalRGroupManagerStore);

  public onFilterChange(filter: GetAllCategoriesRequest) {
    this.store.load(filter);
  }

  public onLoadMore() {
    this.store.loadMore();
  }

  public onEdit(category: CategoryDto) {
    this.openCategoryModal(category);
  }

  public onAdd() {
    this.openCategoryModal();
  }

  public ngOnInit() {
    this._signalRGroupManager.join('categories-list');
  }

  public ngOnDestroy() {
    this._signalRGroupManager.leave('categories-list');
  }

  private openCategoryModal(category: CategoryDto | null = null) {
    const modalRef = this._modalService.create({
      nzContent: CategoryDetailModal,
      nzData: {
        category
      },
      nzCentered: true,
      nzMaskClosable: false
    });

    modalRef.afterClose.pipe(
      takeUntilDestroyed(this._destroyRef)
    ).subscribe(() => {
      this.store.reload();
    });
  }

  public onDelete(category: CategoryDto) {
    this._modalService.confirm({
      nzTitle: 'Confirma stergerea categoriei?',
      nzContent: 'Categoria va fi stearsa definitiv. Sunteti sigur ca doriti sa continuati?',
      nzOkText: 'Sterge',
      nzCancelText: 'Nu',
      nzOkDanger: true,
      nzCentered: true,
      nzIconType: 'icons:circle-exclamation',
      nzOnOk: () => this.store.deleteCategory(category)
    });
  }
}
