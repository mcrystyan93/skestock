import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { CategoryDropdownValue, ItemDto, ItemLocationThreshold, LocationDto } from '@ske/models';
import { LocationsHttp } from '@ske/shared/locations';
import {
  applyWhen,
  applyEach,
  form,
  FormField,
  hidden,
  maxLength,
  min,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import {
  NzFormControlComponent,
  NzFormDirective,
  NzFormItemComponent,
  NzFormLabelComponent,
} from 'ng-zorro-antd/form';
import { SkeletonInputLoaderDirective } from '@ske/shared/loader';
import {
  NzInputAddonAfterDirective,
  NzInputDirective,
  NzInputWrapperComponent,
  NzTextareaCountComponent,
} from 'ng-zorro-antd/input';
import { NzInputNumberComponent } from 'ng-zorro-antd/input-number';
import { CategoryDropdown } from '@ske/shared/categories';
import { NzSwitchComponent } from 'ng-zorro-antd/switch';
import { ItemImageUpload } from '../../image-upload/item-image-upload';
import { NzButtonComponent } from 'ng-zorro-antd/button';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';

@Component({
  imports: [
    NzFormDirective,
    SkeletonInputLoaderDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzFormControlComponent,
    NzInputWrapperComponent,
    FormField,
    NzInputDirective,
    NzInputNumberComponent,
    CategoryDropdown,
    NzSwitchComponent,
    NzColDirective,
    NzRowDirective,
    NzTextareaCountComponent,
    NzInputAddonAfterDirective,
    ItemImageUpload,
    NzButtonComponent,
  ],
  selector: 'ske-item-form',
  styles: ``,
  templateUrl: './form.html',
})
export class Form {
  public readonly loading = input.required<boolean>();
  public readonly item = input.required<Partial<ItemDto>>();

  public readonly imageUploading = signal(false);

  private readonly _locations = toSignal(
    inject(LocationsHttp)
      .getAllPages()
      .pipe(catchError(() => of(null))),
    { initialValue: undefined },
  );
  // undefined = still loading, null = load failed (no rows, so an empty list is submitted)
  public readonly locationsLoading = computed(() => this._locations() === undefined);
  public readonly locationsFailed = computed(() => this._locations() === null);
  private readonly _locationNames = computed(() => {
    const locations = this._locations() ?? [];
    const byId = new Map(locations.map((l) => [l.id, l]));
    return new Map(locations.map((l) => [l.id, locationLabel(l, byId)]));
  });

  private readonly _formModel = linkedSignal({
    source: () => ({ item: this.item(), locations: this._locations() ?? [] }),
    computation: ({ item, locations }) =>
      <ItemFormModel>{
        id: item.id ?? null,
        sku: item.sku ?? '',
        name: item.name ?? '',
        description: item.description ?? '',
        unit: item.unit ?? '',
        locationThresholds: locations.map((l) => ({
          locationId: l.id,
          minThreshold:
            item.locationThresholds?.find((t) => t.locationId === l.id)?.minThreshold ?? 0,
        })),
        isPerishable: item.isPerishable ?? false,
        shelfLifeDays: item.shelfLifeDays ?? null,
        imageFileId: item.imageFileId ?? null,
        category: !item.categoryId ? null : { id: item.categoryId, name: item.categoryName ?? '' },
      },
  });

  public readonly itemForm = form(this._formModel, (schemaPath) => {
    maxLength(schemaPath.sku, 50, {
      message: 'Codul produsului nu poate depăși 50 de caractere.',
    });
    required(schemaPath.name, {
      message: 'Numele produsului este obligatoriu.',
    });
    maxLength(schemaPath.name, 100, {
      message: 'Numele produsului nu poate depăși 100 de caractere.',
    });
    maxLength(schemaPath.description, 500, {
      message: 'Descrierea nu poate depăși 500 de caractere.',
    });
    required(schemaPath.unit, {
      message: 'Unitatea de măsură este obligatorie.',
    });
    maxLength(schemaPath.unit, 20, {
      message: 'Unitatea de măsură nu poate depăși 20 de caractere.',
    });
    applyEach(schemaPath.locationThresholds, (rowPath) => {
      required(rowPath.minThreshold, {
        message: 'Pragul minim este obligatoriu.',
      });
      min(rowPath.minThreshold, 0, {
        message: 'Pragul minim nu poate fi negativ.',
      });
    });
    // Shelf life is only relevant for perishable items: hide it otherwise, and only require a
    // positive value when it is both visible (perishable) and filled in.
    hidden(schemaPath.shelfLifeDays, {
      when: ({ valueOf }) => !valueOf(schemaPath.isPerishable),
    });
    applyWhen(
      schemaPath.shelfLifeDays,
      ({ valueOf }) =>
        valueOf(schemaPath.isPerishable) && valueOf(schemaPath.shelfLifeDays) != null,
      (shelfLifePath) => {
        min(shelfLifePath, 1, {
          message: 'Termenul de valabilitate trebuie să fie de cel puțin o zi.',
        });
      },
    );
    validate(schemaPath.imageFileId, () =>
      this.imageUploading()
        ? { kind: 'imageUploading', message: 'Așteaptă finalizarea încărcării imaginii.' }
        : undefined,
    );
    required(schemaPath.category, {
      message: 'Categoria este obligatorie.',
    });
  });

  public readonly bulkForm = form(signal<{ value: number | null }>({ value: 0 }));

  public applyToAll() {
    const value = this.bulkForm.value().value() ?? 0;
    for (const row of this.itemForm.locationThresholds) {
      row.minThreshold().value.set(value);
    }
  }

  public locationName(locationId: string) {
    return this._locationNames().get(locationId) ?? locationId;
  }

  public async submit(): Promise<ItemFormSubmit> {
    let formData: ItemFormModel | null = null;
    const isValid = await submit(this.itemForm, async (_) => {
      formData = this.itemForm().value();
    });

    return { isValid, formData };
  }
}

export type ItemFormModel = {
  id: string | null;
  sku: string;
  name: string;
  description: string;
  unit: string;
  locationThresholds: ItemLocationThreshold[];
  isPerishable: boolean;
  shelfLifeDays: number | null;
  imageFileId: string | null;
  category: CategoryDropdownValue;
};
export type ItemFormSubmit = {
  isValid: boolean;
  formData: ItemFormModel | null;
};

/** Child locations are labelled with their parent so identically named rooms stay distinguishable. */
function locationLabel(location: LocationDto, byId: Map<string, LocationDto>) {
  return location.parentLocationId
    ? `${byId.get(location.parentLocationId)?.name ?? location.parentLocationName ?? ''} / ${location.name}`
    : location.name;
}
