import { Component, effect, inject, input, untracked } from '@angular/core';
import { HeaderContainer } from './header/header-container';
import { SchoolClassOverviewStore } from '../services/school-class-overview.store';
import { isNil } from 'lodash-es';
import { NzTabComponent, NzTabsComponent } from 'ng-zorro-antd/tabs';
import { GoodsReceiptsTab } from './tabs/goods-receipts/goods-receipts-tab';
import { StockList, TableContainer } from '@ske/shared/stock';

@Component({
  imports: [
    HeaderContainer,
    NzTabsComponent,
    NzTabComponent,
    GoodsReceiptsTab,
    TableContainer,
    StockList
  ],
  selector: 'ske-school-class-overview-page',
  styles: ``,
  templateUrl: './school-class-overview.page.html',
  providers: [SchoolClassOverviewStore],
  host: {
    class: 'flex flex-col grow',
  },
})
export class SchoolClassOverviewPage {
  public readonly id = input.required<number>();
  public readonly store = inject(SchoolClassOverviewStore);


  private readonly _idEffectRef = effect(() => {
    const idValue = this.id();

    if (isNil(idValue) || isNaN(idValue)) return;

    untracked(() => {
      this.store.load(idValue);
    });
  });
}
