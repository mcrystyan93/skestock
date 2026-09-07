import { Component, input, linkedSignal } from '@angular/core';
import { GoodsReceiptImportReviewDto } from '@ske/models';
import { form, FormField, required, schema, submit } from '@angular/forms/signals';
import { NzFormControlComponent, NzFormDirective, NzFormLabelComponent } from 'ng-zorro-antd/form';
import { NzColDirective, NzRowDirective } from 'ng-zorro-antd/grid';
import { NzInputDirective } from 'ng-zorro-antd/input';

@Component({
  imports: [
    NzFormDirective,
    NzRowDirective,
    NzColDirective,
    NzFormLabelComponent,
    NzInputDirective,
    FormField,
    NzFormControlComponent
  ],
  selector: 'ske-goods-receipt-import-review-info',
  styles: ``,
  templateUrl: './review-info.html'
})
export class ReviewInfo {
  public readonly reviewInfo = input.required<GoodsReceiptImportReviewDto | null>();
  public readonly loading = input.required<boolean>();

  public readonly _formModel = linkedSignal(({
    source: () => this.reviewInfo(),
    computation: (reviewInfo) => (<ReviewInfoFormModel>{
      supplierReference: reviewInfo?.supplierReference ?? '',
      note: ''
    })
  }));

  private readonly _reviewSchema = schema<ReviewInfoFormModel>((path) => {
    required(path.supplierReference, { message: 'Referinta furnizorului este obligatorie.' });
    required(path.note, { message: 'Nota este obligatorie.' });
  });

  public readonly reviewForm = form(this._formModel, this._reviewSchema);

  public async submit(): Promise<ReviewInfoSubmitResult> {
    let isValid: boolean = false;
    let model: ReviewInfoFormModel | null = null;

    isValid = await submit(this.reviewForm, async (_) => {
      model = this.reviewForm().value();
    });

    return { isValid, model };
  }
}

export type ReviewInfoFormModel = {
  supplierReference: string;
  note: string;
}
export type ReviewInfoSubmitResult = {
  isValid: boolean;
  model: ReviewInfoFormModel | null;
}
