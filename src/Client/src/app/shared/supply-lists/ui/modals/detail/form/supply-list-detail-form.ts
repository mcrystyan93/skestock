import { Component, computed, effect, input, linkedSignal, untracked } from '@angular/core';
import {
  ItemDropdownValue,
  SUPPLY_LIST_FREQUENCY_OPTIONS,
  SUPPLY_LIST_MAX_INTERVAL_WEEKS,
  SUPPLY_LIST_MIN_INTERVAL_WEEKS,
  SupplyListDto,
  SupplyListFrequency
} from '@ske/models';
import {
  applyEach,
  disabled,
  form,
  FormField,
  maxLength,
  required,
  schema,
  submit,
  validate
} from '@angular/forms/signals';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormControlComponent, NzFormDirective, NzFormItemComponent, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzInputDirective, NzInputWrapperComponent, NzTextareaCountComponent } from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { NzSelectComponent, NzOptionComponent } from 'ng-zorro-antd/select';
import { NzDividerComponent } from 'ng-zorro-antd/divider';
import { NzListComponent, NzListEmptyComponent } from 'ng-zorro-antd/list';
import { ItemDropdown } from '@ske/shared/items';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import { isNil } from 'lodash-es';
import { SupplyListLine, type SupplyListLineFormModel } from './supply-list-line';

@Component({
  imports: [
    NzRowDirective,
    NzColDirective,
    NzFormLabelComponent,
    FormField,
    NzInputDirective,
    NzFormControlComponent,
    NzInputWrapperComponent,
    NzInputNumberComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzTextareaCountComponent,
    NzSelectComponent,
    NzOptionComponent,
    NzDividerComponent,
    NzListComponent,
    NzListEmptyComponent,
    ItemDropdown,
    SkeletonInputLoaderDirective,
    SupplyListLine
  ],
  selector: 'ske-supply-list-detail-form',
  templateUrl: './supply-list-detail-form.html'
})
export class SupplyListDetailForm {
  public readonly loading = input.required<boolean>();
  public readonly supplyList = input.required<Partial<SupplyListDto>>();
  public readonly editable = computed(() => this.supplyList().isActive !== false);

  public readonly frequencyOptions = SUPPLY_LIST_FREQUENCY_OPTIONS;
  public readonly minIntervalWeeks = SUPPLY_LIST_MIN_INTERVAL_WEEKS;
  public readonly maxIntervalWeeks = SUPPLY_LIST_MAX_INTERVAL_WEEKS;

  private readonly _formModel = linkedSignal({
    source: () => this.supplyList(),
    computation: (supplyList): SupplyListDetailFormModel => ({
      id: supplyList.id ?? null,
      name: supplyList.name ?? '',
      note: supplyList.note ?? '',
      frequency: supplyList.frequency ?? 'Weekly',
      intervalWeeks: supplyList.intervalWeeks ?? null,
      lines: (supplyList.lines ?? []).map((line) => ({
        itemId: line.itemId,
        itemName: line.itemSku ? `(${line.itemSku}) ${line.itemName}` : line.itemName,
        quantity: line.quantity,
        unit: line.unit ?? '',
        notes: line.notes ?? ''
      })),
      lineItem: null
    })
  });

  private readonly _isDisabled = () => this.loading() || !this.editable();

  private readonly _lineSchema = schema<SupplyListLineFormModel>((path) => {
    disabled(path.quantity, { when: this._isDisabled });
    required(path.quantity, { message: 'Cantitatea este obligatorie.' });
    validate(path.quantity, ({ value }) => {
      const quantity = value();

      if (isNil(quantity))
        return null;

      return typeof quantity !== 'number' || !Number.isFinite(quantity) || quantity <= 0
        ? { kind: 'greaterThan', message: 'Cantitatea trebuie să fie mai mare decât 0.' }
        : null;
    });
    disabled(path.unit, { when: this._isDisabled });
    required(path.unit, { message: 'Unitatea este obligatorie.' });
    maxLength(path.unit, 50, { message: 'Unitatea nu poate depăși 50 de caractere.' });
    disabled(path.notes, { when: this._isDisabled });
    maxLength(path.notes, 500, { message: 'Observațiile nu pot depăși 500 de caractere.' });
  });

  public readonly supplyListForm = form(this._formModel, (path) => {
    disabled(path.name, { when: this._isDisabled });
    required(path.name, { message: 'Numele listei este obligatoriu.' });
    maxLength(path.name, 200, { message: 'Numele listei nu poate depăși 200 de caractere.' });
    disabled(path.note, { when: this._isDisabled });
    maxLength(path.note, 1000, { message: 'Nota nu poate depăși 1000 de caractere.' });
    disabled(path.frequency, { when: this._isDisabled });
    required(path.frequency, { message: 'Frecvența este obligatorie.' });
    disabled(path.intervalWeeks, {
      when: ({ valueOf }) => this._isDisabled() || valueOf(path.frequency) !== 'EveryXWeeks'
    });
    validate(path.intervalWeeks, ({ value, valueOf }) => {
      if (valueOf(path.frequency) !== 'EveryXWeeks')
        return null;

      const intervalWeeks = value();

      if (isNil(intervalWeeks))
        return { kind: 'required', message: 'Intervalul de săptămâni este obligatoriu.' };

      return !Number.isInteger(intervalWeeks)
      || intervalWeeks < SUPPLY_LIST_MIN_INTERVAL_WEEKS
      || intervalWeeks > SUPPLY_LIST_MAX_INTERVAL_WEEKS
        ? {
          kind: 'range',
          message: `Intervalul trebuie să fie între ${SUPPLY_LIST_MIN_INTERVAL_WEEKS} și ${SUPPLY_LIST_MAX_INTERVAL_WEEKS} săptămâni.`
        }
        : null;
    });
    disabled(path.lineItem, { when: this._isDisabled });
    applyEach(path.lines, this._lineSchema);
  });

  private readonly _lineItemChangeRef = effect(() => {
    const lineItem = this.supplyListForm.lineItem().value();

    if (isNil(lineItem))
      return;

    untracked(() => {
      this.addItem(lineItem);
      queueMicrotask(() => this.supplyListForm.lineItem().reset(null));
    });
  });

  public addItem(item: NonNullable<ItemDropdownValue>) {
    if (isNil(item.id))
      return;

    this.supplyListForm.lines().value.update((lines) => {
      if (lines.some((line) => line.itemId === item.id))
        return lines;

      const name = item.name ?? '';

      return [{
        itemId: item.id,
        itemName: item.sku ? `(${item.sku}) ${name}` : name,
        quantity: 1,
        unit: item.unit ?? 'buc',
        notes: ''
      }, ...lines];
    });
  }

  protected removeLine(itemId: string) {
    this.supplyListForm.lines().value.update((lines) => lines.filter((line) => line.itemId !== itemId));
  }

  public async submit(): Promise<SupplyListDetailFormSubmit> {
    let formData: SupplyListDetailFormModel | null = null;

    const isValid = await submit(this.supplyListForm, async () => {
      formData = this.supplyListForm().value();
    });

    return { isValid, formData };
  }
}

export type SupplyListDetailFormModel = {
  id: string | null;
  name: string;
  note: string;
  frequency: SupplyListFrequency;
  intervalWeeks: number | null;
  lines: SupplyListLineFormModel[];
  lineItem: ItemDropdownValue;
};

export type SupplyListDetailFormSubmit = {
  isValid: boolean;
  formData: SupplyListDetailFormModel | null;
};
