import {Component, computed, input, linkedSignal, output} from '@angular/core';
import {FormField, form, max, min, required, submit, validate} from '@angular/forms/signals';
import {NzButtonComponent} from 'ng-zorro-antd/button';
import {NzCardComponent} from 'ng-zorro-antd/card';
import {
  NzFormControlComponent,
  NzFormDirective,
  NzFormItemComponent,
  NzFormLabelComponent,
} from 'ng-zorro-antd/form';
import {NzInputNumberComponent} from 'ng-zorro-antd/input-number';
import {NzTagComponent} from 'ng-zorro-antd/tag';
import {NzTypographyComponent} from 'ng-zorro-antd/typography';
import {LoaderDirective} from '@ske/shared/loader';
import {InvitationSplitDto, SaveInvitationSplitRequest} from '@ske/models';
import {
  INSTRUCTOR_HEADCOUNT,
  MAX_INVITATION_SPLIT_COUNT,
  MAX_STUDENT_HEADCOUNT,
  MIN_STUDENT_HEADCOUNT,
} from '../services/configuration.constants';

const INVITATION_SPLIT_FIELDS: readonly {
  key: keyof SaveInvitationSplitRequest;
  id: string;
  label: string;
  hint?: string;
}[] = [
  {
    key: 'studentInvitationCount',
    id: 'student-invitation-count',
    label: 'Cursanți',
    hint: `Se înmulțește cu aprox. ${MIN_STUDENT_HEADCOUNT}–${MAX_STUDENT_HEADCOUNT} cursanți.`,
  },
  {
    key: 'instructorInvitationCount',
    id: 'instructor-invitation-count',
    label: 'Instructori',
    hint: `Se înmulțește cu ${INSTRUCTOR_HEADCOUNT} instructori.`,
  },
  {key: 'openingPrayerInvitationCount', id: 'opening-prayer-invitation-count', label: 'Rugăciune deschidere'},
  {key: 'experienceInvitationCount', id: 'experience-invitation-count', label: 'Experiențe'},
  {key: 'changeInvitationCount', id: 'change-invitation-count', label: 'Schimbări'},
];

@Component({
  imports: [
    FormField,
    NzButtonComponent,
    NzCardComponent,
    NzFormControlComponent,
    NzFormDirective,
    NzFormItemComponent,
    NzFormLabelComponent,
    NzInputNumberComponent,
    NzTagComponent,
    NzTypographyComponent,
    LoaderDirective,
  ],
  selector: 'ske-configuration-invitation-split-card',
  templateUrl: './invitation-split-card.html',
})
export class InvitationSplitCard {
  public readonly split = input.required<InvitationSplitDto>();
  public readonly invitationCount = input.required<number>();
  public readonly loading = input(false);
  public readonly onSave = output<void>();
  protected readonly maxCount = MAX_INVITATION_SPLIT_COUNT;
  protected readonly fields = INVITATION_SPLIT_FIELDS;

  private readonly _formModel = linkedSignal<SaveInvitationSplitRequest>(() => ({...this.split()}));

  public readonly splitForm = form(this._formModel, (path) => {
    for (const {key} of INVITATION_SPLIT_FIELDS) {
      required(path[key], {message: 'Numărul de invitații este obligatoriu.'});
      validate(path[key], ({value}) =>
        // An empty field is reported by `required` alone.
        value() == null || Number.isInteger(value())
          ? undefined
          : {kind: 'integer', message: 'Introduceți un număr întreg.'},
      );
      min(path[key], 0, {message: 'Valoarea minimă este 0.'});
      max(path[key], MAX_INVITATION_SPLIT_COUNT, {message: `Valoarea maximă este ${MAX_INVITATION_SPLIT_COUNT}.`});
    }
  });

  protected readonly range = computed(() => invitationRange(this.splitForm().value()));
  protected readonly rangeLabel = computed(() => {
    const {min, max} = this.range();
    return min === max ? `${min}` : `${min}–${max}`;
  });
  // Informational only: never blocks saving.
  protected readonly exceedsMaximum = computed(
    () => this.invitationCount() > 0 && this.range().max > this.invitationCount(),
  );

  public async submit(): Promise<InvitationSplitFormSubmit> {
    let formData: SaveInvitationSplitRequest | null = null;
    const isValid = await submit(this.splitForm, async () => {
      formData = this.splitForm().value();
    });
    return {isValid, formData};
  }
}

export type InvitationSplitFormSubmit = {
  isValid: boolean;
  formData: SaveInvitationSplitRequest | null;
};

export type InvitationRange = { min: number; max: number };

// Empty or invalid fields count as 0 so the total never shows NaN.
export function invitationRange(split: SaveInvitationSplitRequest): InvitationRange {
  const count = (value: unknown) => (typeof value === 'number' && Number.isFinite(value) ? value : 0);
  const students = count(split.studentInvitationCount);
  const fixed =
    count(split.instructorInvitationCount) * INSTRUCTOR_HEADCOUNT +
    count(split.openingPrayerInvitationCount) +
    count(split.experienceInvitationCount) +
    count(split.changeInvitationCount);
  return {
    min: students * MIN_STUDENT_HEADCOUNT + fixed,
    max: students * MAX_STUDENT_HEADCOUNT + fixed,
  };
}
