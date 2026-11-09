import { Component, input, output } from '@angular/core';
import { ITEM_MERGE_KIND_LABELS, ItemMergeImpact } from '@ske/models';
import { NzAlertComponent } from 'ng-zorro-antd/alert';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzSpinComponent } from 'ng-zorro-antd/spin';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
@Component({
  imports: [NzAlertComponent, NzButtonComponent, NzSpinComponent, NzTypographyComponent],
  selector: 'ske-item-merge-impact-step',
  template: `
    @if (loading()) {
      <div class="flex justify-center py-8">
        <nz-spin nzTip="Se calculează datele afectate..." />
      </div>
    } @else if (errorMessage()) {
      <nz-alert nzType="error"
                nzShowIcon
                nzMessage="Previzualizarea nu a putut fi încărcată"
                [nzDescription]="errorMessage()" />
      <button nz-button
              class="mt-3"
              type="button"
              (click)="retry.emit()">
        Reîncearcă
      </button>
    } @else if (impacts().length === 0) {
      <p nz-typography
         nzType="secondary">Nu există date de mutat. Articolele secundare vor fi doar dezactivate.</p>
    } @else {
      <p nz-typography
         nzType="secondary">Datele de mai jos vor fi legate de articolul principal.</p>
      <ul class="m-0 flex list-none flex-col gap-2 p-0">
        @for (impact of impacts(); track impact.kind) {
          <li class="flex flex-col gap-1 rounded border border-solid border-slate-200 p-3">
            <div class="flex items-baseline justify-between gap-3">
              <span class="font-medium">{{ labels[impact.kind] }}</span>
              <span>{{ impact.relinked }} mutate</span>
            </div>
            @if (impact.combined > 0) {
              <span class="text-sm text-amber-600">
                @if (impact.kind === 'classItemStockVisibilities' || impact.kind === 'itemLocationThresholds') {
                  {{ impact.combined }} înregistrări duplicate vor fi eliminate. Se păstrează setările articolului principal, dacă există; altfel, ale unei înregistrări secundare.
                } @else {
                  {{ impact.combined }} înregistrări duplicate vor fi eliminate. Cantitățile se însumează în înregistrarea păstrată.
                }
              </span>
            }
          </li>
        }
      </ul>
    }
  `
})
export class ItemMergeImpactStep {
  public readonly impacts = input<ItemMergeImpact[]>([]);
  public readonly loading = input(false);
  public readonly errorMessage = input<string | null>(null);
  public readonly retry = output<void>();
  protected readonly labels = ITEM_MERGE_KIND_LABELS;
}
