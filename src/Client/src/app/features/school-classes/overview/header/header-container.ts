import {Component, inject, input, model} from '@angular/core';
import {SchoolClassOverviewStore} from '../../services/school-class-overview.store';
import {Header} from './header';
import {NzModalService} from 'ng-zorro-antd/modal';
import {AddGoodsReceiptModal} from '@ske/shared/goods-receipts';
import {Router} from '@angular/router';

@Component({
  imports: [
    Header
  ],
  selector: 'ske-school-class-overview-header-container',
  styles: ``,
  templateUrl: './header-container.html',
  providers: [NzModalService]
})
export class HeaderContainer {
  public readonly selectedTabIndex = model<number>(0);
  public readonly classId = input.required<string | null>();
  public readonly store = inject(SchoolClassOverviewStore);

  private readonly _router = inject(Router);
  private readonly _nzModalService = inject(NzModalService);

  public addGoodsReceipt() {
    this._nzModalService.create({
      nzContent: AddGoodsReceiptModal,
      nzData: {
        classId: this.classId()
      },
      nzCentered: true,
      nzClosable: false
    });
  }

  public close() {
    this._router.navigate(['school-classes']);
  }
}
