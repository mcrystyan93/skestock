import { Component, effect, inject, input, OnDestroy, OnInit, signal, untracked } from '@angular/core';
import { HeaderContainer } from './header/header-container';
import { SchoolClassOverviewStore } from '../services/school-class-overview.store';
import { isNil } from 'lodash-es';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { GoodsReceiptsTab } from './tabs/goods-receipts/goods-receipts-tab';
import { GoodsReceiptImportsTab } from './tabs/goods-receipt-imports/goods-receipt-imports-tab';
import { OrderListsTab } from './tabs/order-lists/order-lists-tab';
import { StatisticsTab } from './tabs/statistics/statistics-tab';
import { ClassStock } from '@ske/shared/stock';
import { realtimeGroups, SignalRGroupManagerStore } from '@ske/signalr';
import { ErrorAlert } from '@ske/shared/errors';
import { QueryParamState } from '@ske/routes';

@Component({
  imports: [
    HeaderContainer,
    NzTabsComponent,
    NzTabComponent,
    GoodsReceiptsTab,
    GoodsReceiptImportsTab,
    OrderListsTab,
    StatisticsTab,
    ClassStock,
    ErrorAlert
  ],
  selector: 'ske-school-class-overview-page',
  styles: ``,
  templateUrl: './school-class-overview.page.html',
  providers: [SchoolClassOverviewStore, QueryParamState],
  host: {
    class: 'flex min-w-0 flex-col grow overflow-x-hidden'
  }
})
export class SchoolClassOverviewPage implements OnInit, OnDestroy {
  public readonly id = input.required<string>();

  public readonly store = inject(SchoolClassOverviewStore);

  private readonly _signalRGroupManager = inject(SignalRGroupManagerStore);

  public readonly categoryId = signal<string | null>(null);
  public readonly locationId = signal<string | null>(null);
  public readonly receiptId = signal<string | null>(null);


  public ngOnInit() {
    this._signalRGroupManager.join(realtimeGroups.goodsReceiptImportsList);
    this._signalRGroupManager.join(realtimeGroups.schoolClass(this.id()));
  }

  public ngOnDestroy() {
    this._signalRGroupManager.leave(realtimeGroups.goodsReceiptImportsList);
    this._signalRGroupManager.leave(realtimeGroups.schoolClass(this.id()));
  }

  private readonly _idEffectRef = effect(() => {
    const idValue = this.id();

    if (isNil(idValue) || idValue === '') return;

    untracked(() => {
      this.store.load(idValue);
    });
  });
}
