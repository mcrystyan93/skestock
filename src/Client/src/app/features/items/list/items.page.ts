import { Component, DestroyRef, inject } from '@angular/core';
import { ItemListState } from '../services/item-list.store';
import { FilterContainer } from './filter/filter-container';
import { Table } from './table/table';
import { GetAllItemsRequest, ItemDto } from '@ske/models';
import { NzModalService } from 'ng-zorro-antd/modal';
import { Header } from './header/header';
import { ItemDetailModal } from '@ske/shared/items';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  imports: [
    FilterContainer,
    Table,
    Header
  ],
  selector: 'ske-items-page',
  templateUrl: './items.page.html',
  providers: [ItemListState, NzModalService],
  host: {
    class: 'flex flex-col grow gap-4'
  }
})
export class ItemsPage {
  public readonly store = inject(ItemListState);

  private readonly _modalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);

  public onFilterChange(filter: GetAllItemsRequest) {
    this.store.load(filter);
  }

  public onLoadMore() {
    this.store.loadMore();
  }

  public onEdit(item: ItemDto) {
    this.openItemModal(item);
  }

  public onAdd() {
    this.openItemModal();
  }

  private openItemModal(item: ItemDto | null = null) {
    const modalRef = this._modalService.create({
      nzContent: ItemDetailModal,
      nzData: {
        item
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

  public onToggleActive(item: ItemDto) {
    if (item.isActive) {
      this._modalService.confirm({
        nzTitle: 'Confirma dezactivarea articolului?',
        nzContent: 'Articolul va fi dezactivat. Sunteti sigur ca doriti sa continuati?',
        nzOkText: 'Dezactiveaza',
        nzCancelText: 'Nu',
        nzOkDanger: true,
        nzCentered: true,
        nzIconType: 'icons:circle-exclamation',
        nzOnOk: () => this.store.toggleActive(item)
      });
      return;
    }

    this.store.toggleActive(item);
  }
}
