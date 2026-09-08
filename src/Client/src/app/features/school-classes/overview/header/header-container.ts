import { Component, DestroyRef, inject, input, model } from '@angular/core';
import { SchoolClassOverviewStore } from '../../services/school-class-overview.store';
import { Header } from './header';
import { NzModalService } from 'ng-zorro-antd/modal';
import { AddGoodsReceiptModal } from '@ske/shared/goods-receipts';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { from, map, tap } from 'rxjs';
import { isNil } from 'lodash-es';
import { CreateGoodsReceiptImportRequest, FileMetadataDto } from '@ske/models';
import { Router } from '@angular/router';

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
  private readonly _destroyRef = inject(DestroyRef);

  public addGoodsReceipt() {
    const modalRef = this._nzModalService.create({
      nzContent: AddGoodsReceiptModal,
      nzCentered: true,
      nzClosable: false
    });

    modalRef.afterClose
      .pipe(
        takeUntilDestroyed(this._destroyRef),
        map((result) => this.mapImportRequests(result?.files as FileMetadataDto[] | undefined)),
        tap((requests) => this.store.importGoodReceipt(from(requests)))
      )
      .subscribe();
  }

  public mapImportRequests(files: FileMetadataDto[] | undefined): CreateGoodsReceiptImportRequest[] {
    if (isNil(files) || files.length === 0)
      return [];

    return files
      .map((file) => this.mapImportRequest(file))
      .filter((request): request is CreateGoodsReceiptImportRequest => !isNil(request));
  }

  public mapImportRequest(fileMetadata: FileMetadataDto): CreateGoodsReceiptImportRequest | null {
    const classId = this.classId();

    if (isNil(classId))
      return null;

    return {
      fileMetadataId: fileMetadata.id,
      classId
    };
  }

  public close() {
    this._router.navigate(['school-classes']);
  }
}
