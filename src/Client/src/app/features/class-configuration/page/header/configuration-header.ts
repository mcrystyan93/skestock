import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import {
  NzPageHeaderComponent,
  NzPageHeaderContentDirective,
  NzPageHeaderExtraDirective,
  NzPageHeaderTitleDirective,
} from 'ng-zorro-antd/page-header';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ConfigurationHttp } from '../../services/configuration.http';
import { MenuToggle } from '@ske/layouts/menu-toggle';
import { ThemeSwitcher } from '@ske/shared/theme';
import { NzBreadCrumbComponent, NzBreadCrumbItemComponent } from 'ng-zorro-antd/breadcrumb';

@Component({
  imports: [
    MenuToggle,
    NzPageHeaderComponent,
    NzPageHeaderContentDirective,
    NzPageHeaderExtraDirective,
    NzPageHeaderTitleDirective,
    NzButtonComponent,
    NzIconDirective,
    NzTypographyComponent,
    ThemeSwitcher,
    NzBreadCrumbComponent,
    NzBreadCrumbItemComponent,
  ],
  selector: 'ske-configuration-header',
  templateUrl: './configuration-header.html',
})
export class ConfigurationHeader {
  private readonly _http = inject(ConfigurationHttp);
  private readonly _nzMessageService = inject(NzMessageService);

  public readonly recalculating = signal(false);

  public recalculatePurchaseStatistics() {
    if (this.recalculating()) {
      return;
    }

    this.recalculating.set(true);
    this._http
      .materializePurchaseStatistics()
      .pipe(finalize(() => this.recalculating.set(false)))
      .subscribe({
        next: () => this._nzMessageService.success('Statisticile de achiziții au fost recalculate.'),
        error: (error: unknown) => this._nzMessageService.error(getRecalculateErrorMessage(error)),
      });
  }
}

function getRecalculateErrorMessage(error: unknown) {
  switch (error instanceof HttpErrorResponse ? error.status : undefined) {
    case HttpStatusCode.Conflict:
      return 'Statisticile sunt deja în curs de recalculare. Încercați din nou în scurt timp.';
    case HttpStatusCode.Forbidden:
      return 'Doar administratorii pot recalcula statisticile de achiziții.';
    default:
      return 'Statisticile de achiziții nu au putut fi recalculate.';
  }
}
