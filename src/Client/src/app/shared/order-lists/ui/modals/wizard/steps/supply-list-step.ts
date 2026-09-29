import { Component, input, output } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { formatSupplyListFrequency, SupplyListListItemDto } from '@ske/models';
import { NzCheckboxComponent } from 'ng-zorro-antd/checkbox';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { NzTagComponent } from 'ng-zorro-antd/tag';
import { CHECKABLE_CARD_CLASSES } from './card-classes';
import { formatArticleCount, formatListCount } from './counts';
import { checkedRowsModel, emitCheckedIdsOnChange } from './checked-rows';

@Component({
  imports: [FormField, NzCheckboxComponent, NzEmptyComponent, NzSkeletonComponent, NzTagComponent],
  selector: 'ske-order-list-supply-list-step',
  template: `
    <fieldset class="m-0 border-0 p-0">
      <legend class="mb-1 text-base font-medium">Alege una sau mai multe liste predefinite</legend>
      <p class="mb-3 mt-0 opacity-70">Articolele listelor selectate se îmbină într-o singură comandă.</p>

      @if (loading()) {
        <nz-skeleton aria-busy="true" [nzActive]="true" [nzParagraph]="{ rows: 3 }" />
      } @else if (lists().length === 0) {
        <nz-empty nzNotFoundContent="Nu există liste predefinite active." />
      } @else {
        <ul class="m-0 flex max-h-[45vh] list-none flex-col gap-2 overflow-auto p-0">
          @for (item of listsForm.rows; track $index) {
            @let list = lists()[$index];
            @if (list) {
              <li>
                <label nz-checkbox
                       class="flex! items-center! gap-3 m-0! w-full px-4! py-3! [&>span:last-child]:flex-1 [&>span:last-child]:ps-3 [&>span:last-child]:pe-0"
                       [class]="cardClasses"
                       [formField]="item.checked">
                  <span class="flex grow flex-wrap items-center justify-between gap-x-3 gap-y-1">
                    <span class="flex flex-col">
                      <span class="font-medium">{{ list.name }}</span>
                      <span class="text-sm opacity-70">{{ frequency(list) }}</span>
                    </span>
                    <nz-tag class="m-0">{{ articleCount(list.lineCount) }}</nz-tag>
                  </span>
                </label>
              </li>
            }
          }
        </ul>

        <p class="mb-0 mt-3 min-h-6 text-sm" aria-live="polite">
          @if (selectedIds().length > 0) {
            {{ listCount(selectedIds().length) }}
            @if (summaryLoading()) {
              · se calculează articolele…
            } @else {
              · {{ articleCount(distinctItemCount()) }} distincte în comandă
            }
          }
        </p>
      }
    </fieldset>
  `
})
export class OrderListSupplyListStep {
  public readonly lists = input.required<SupplyListListItemDto[]>();
  public readonly selectedIds = input<string[]>([]);
  public readonly loading = input(false);
  public readonly summaryLoading = input(false);
  public readonly distinctItemCount = input(0);
  public readonly selectedIdsChange = output<string[]>();

  protected readonly cardClasses = CHECKABLE_CARD_CLASSES;
  protected readonly articleCount = formatArticleCount;
  protected readonly listCount = formatListCount;

  private readonly _model = checkedRowsModel(() => this.lists().map((list) => list.id), this.selectedIds);

  protected readonly listsForm = form(this._model);

  private readonly _emitSelection = emitCheckedIdsOnChange(this._model, this.selectedIds, (ids) => this.selectedIdsChange.emit(ids));

  protected frequency(list: SupplyListListItemDto): string {
    return formatSupplyListFrequency(list.frequency, list.intervalWeeks);
  }
}
