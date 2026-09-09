import { Component, effect, input, linkedSignal, output } from '@angular/core';
import { GetAllItemImportsRequest } from '@ske/models';
import { form, FormField, submit } from '@angular/forms/signals';
import { FormsModule } from '@angular/forms';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzFormDirective, NzFormControlComponent, NzFormItemComponent } from 'ng-zorro-antd/form';
import { NzIconDirective } from 'ng-zorro-antd/icon';
import { NzInputDirective, NzInputWrapperComponent } from 'ng-zorro-antd/input';
import { NzSpaceComponent, NzSpaceItemDirective } from 'ng-zorro-antd/space';

@Component({
  imports: [FormsModule, NzFormDirective, NzFormItemComponent, NzFormControlComponent, NzRowDirective,
    NzColDirective, NzInputWrapperComponent, NzIconDirective, NzInputDirective, FormField,
    NzSpaceComponent, NzSpaceItemDirective, NzButtonComponent],
  selector: 'ske-item-import-filter-form',
  templateUrl: './item-import-filter-form.html'
})
export class ItemImportFilterForm {
  public readonly loading = input.required<boolean>();
  public readonly filter = input.required<GetAllItemImportsRequest>();
  public readonly onFilterChange = output<GetAllItemImportsRequest>();

  private _initialFilterEmitted = false;
  private readonly _formModel = linkedSignal({
    source: () => this.filter(),
    computation: (filter) => ({ searchTerm: filter.searchTerm ?? '' })
  });
  public readonly filterForm = form(this._formModel);

  private readonly _initialFilterEffectRef = effect(() => {
    if (this._initialFilterEmitted)
      return;
    this.filter();
    this.onFilterChange.emit(this.buildFilterCriteria());
    this._initialFilterEmitted = true;
  });

  public async onSubmit() {
    let validFilter: GetAllItemImportsRequest | null = null;
    const valid = await submit(this.filterForm, async () => {
      validFilter = this.buildFilterCriteria();
    });
    if (valid && validFilter)
      this.onFilterChange.emit(validFilter);
  }

  public clear() {
    this.filterForm().reset({ searchTerm: '' });
    void this.onSubmit();
  }

  private buildFilterCriteria(): GetAllItemImportsRequest {
    return { ...this.filter(), searchTerm: this.filterForm().value().searchTerm };
  }
}
