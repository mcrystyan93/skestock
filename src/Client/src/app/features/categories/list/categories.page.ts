import { Component, DestroyRef, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CategoryListState } from '../services/category-list.store';
import { CategoryDto, GetAllCategoriesRequest } from '@ske/models';
import { NzModalService } from 'ng-zorro-antd/modal';
import { Header } from './header/header';
import { CategoryDetailModal, CategoryImportModal, CategoryImportState } from '@ske/shared/categories';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { realtimeGroups, SignalRGroupManagerStore } from '@ske/signalr';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { CategoryListTab } from './tabs/category-list-tab';
import { CategoryImportListTab } from './tabs/category-import-list-tab';

@Component({
  imports: [
    Header,
    NzTabsComponent,
    NzTabComponent,
    CategoryListTab,
    CategoryImportListTab
  ],
  selector: 'ske-categories-page',
  templateUrl: './categories.page.html',
  providers: [CategoryListState, CategoryImportState, NzModalService],
  host: {
    class: 'flex flex-col grow gap-4'
  }
})
export class CategoriesPage implements OnInit, OnDestroy {
  public readonly store = inject(CategoryListState);
  public readonly selectedTabIndex = signal(0);

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

  public onImport() {
    this._modalService.create({
      nzContent: CategoryImportModal,
      nzCentered: true,
      nzMaskClosable: false
    });
  }

  public ngOnInit() {
    this._signalRGroupManager.join(realtimeGroups.categoriesList);
    this._signalRGroupManager.join(realtimeGroups.categoryImportsList);
  }

  public ngOnDestroy() {
    this._signalRGroupManager.leave(realtimeGroups.categoriesList);
    this._signalRGroupManager.leave(realtimeGroups.categoryImportsList);
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
