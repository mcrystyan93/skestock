import { Component, effect, inject, input, OnDestroy, OnInit, untracked } from '@angular/core';
import { HeaderContainer } from './header/header-container';
import { SchoolClassOverviewStore } from '../services/school-class-overview.store';
import { isNil } from 'lodash-es';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { GoodsReceiptsTab } from './tabs/goods-receipts/goods-receipts-tab';
import { GoodsReceiptImportsTab } from './tabs/goods-receipt-imports/goods-receipt-imports-tab';
import { StockList } from '@ske/shared/stock';
import { realtimeGroups, SignalRGroupManagerStore } from '@ske/signalr';
import { ErrorAlert } from '@ske/shared/errors';

@Component({
  imports: [
    HeaderContainer,
    NzTabsComponent,
    NzTabComponent,
    GoodsReceiptsTab,
    GoodsReceiptImportsTab,
    StockList,
    ErrorAlert
  ],
  selector: 'ske-school-class-overview-page',
  styles: ``,
  templateUrl: './school-class-overview.page.html',
  providers: [SchoolClassOverviewStore],
  host: {
    class: 'flex flex-col grow'
  }
})
export class SchoolClassOverviewPage implements OnInit, OnDestroy {
  public readonly id = input.required<string>();
  public readonly store = inject(SchoolClassOverviewStore);
  private readonly _signalRGroupManager = inject(SignalRGroupManagerStore);


  public ngOnInit() {
    this._signalRGroupManager.join(realtimeGroups.goodsReceiptImportsList);
  }

  public ngOnDestroy() {
    this._signalRGroupManager.leave(realtimeGroups.goodsReceiptImportsList);
  }

  private readonly _idEffectRef = effect(() => {
    const idValue = this.id();

    if (isNil(idValue) || idValue === '') return;

    untracked(() => {
      this.store.load(idValue);
    });
  });
}
