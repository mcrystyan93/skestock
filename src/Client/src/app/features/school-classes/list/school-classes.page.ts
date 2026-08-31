import { Component, DestroyRef, inject } from '@angular/core';
import { SchoolClassListState } from '../services/school-class-list.store';
import { FilterContainer } from './filter/filter-container';
import { Table } from './table/table';
import { GetAllSchoolClassesRequest, SchoolClassDto } from '@ske/models';
import { NzModalService } from 'ng-zorro-antd/modal';
import { Header } from './header/header';
import { SchoolClassDetailModal } from '@ske/shared/school-classes';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

@Component({
  imports: [
    FilterContainer,
    Table,
    Header
  ],
  selector: 'ske-school-classes-page',
  templateUrl: './school-classes.page.html',
  providers: [SchoolClassListState, NzModalService],
  host: {
    class: 'flex flex-col grow gap-4'
  }
})
export class SchoolClassesPage {
  public readonly store = inject(SchoolClassListState);

  private readonly _modalService = inject(NzModalService);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _router = inject(Router);

  public onFilterChange(filter: GetAllSchoolClassesRequest) {
    this.store.load(filter);
  }

  public onLoadMore() {
    this.store.loadMore();
  }

  public onEdit(schoolClass: SchoolClassDto) {
    this.openSchoolClassModal(schoolClass);
  }

  public onView(schoolClass: SchoolClassDto) {
    this._router.navigate(['/school-classes', schoolClass.id]);
  }

  public onAdd() {
    this.openSchoolClassModal();
  }

  private openSchoolClassModal(schoolClass: SchoolClassDto | null = null) {
    const modalRef = this._modalService.create({
      nzContent: SchoolClassDetailModal,
      nzData: {
        schoolClass
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
}
