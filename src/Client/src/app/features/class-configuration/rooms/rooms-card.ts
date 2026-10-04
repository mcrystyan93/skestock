import {Component, input, linkedSignal, output} from '@angular/core';
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
import {NzTypographyComponent} from 'ng-zorro-antd/typography';
import {LoaderDirective} from '@ske/shared/loader';
import {RoomConfigurationDto, SaveRoomConfigurationRequest} from '@ske/models';
import {MAX_ROOM_SEAT_COUNT} from '../services/configuration.constants';

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
    NzTypographyComponent,
    LoaderDirective,
  ],
  selector: 'ske-configuration-rooms-card',
  templateUrl: './rooms-card.html',
})
export class RoomsCard {
  public readonly configuration = input.required<RoomConfigurationDto>();
  public readonly loading = input(false);
  public readonly onSave = output<void>();
  protected readonly maxSeatCount = MAX_ROOM_SEAT_COUNT;

  private readonly _formModel = linkedSignal<SaveRoomConfigurationRequest>(() => ({
    ...this.configuration(),
  }));

  public readonly roomsForm = form(this._formModel, (path) => {
    required(path.room4SeatCount, {message: 'Numărul de locuri este obligatoriu.'});
    validate(path.room4SeatCount, ({value}) =>
      Number.isInteger(value())
        ? undefined
        : {kind: 'integer', message: 'Introduceți un număr întreg.'},
    );
    min(path.room4SeatCount, 0, {message: 'Valoarea minimă este 0.'});
    max(path.room4SeatCount, MAX_ROOM_SEAT_COUNT, {
      message: `Valoarea maximă este ${MAX_ROOM_SEAT_COUNT}.`,
    });

    required(path.room2SeatCount, {message: 'Numărul de locuri este obligatoriu.'});
    validate(path.room2SeatCount, ({value}) =>
      Number.isInteger(value())
        ? undefined
        : {kind: 'integer', message: 'Introduceți un număr întreg.'},
    );
    min(path.room2SeatCount, 0, {message: 'Valoarea minimă este 0.'});
    max(path.room2SeatCount, MAX_ROOM_SEAT_COUNT, {
      message: `Valoarea maximă este ${MAX_ROOM_SEAT_COUNT}.`,
    });

    required(path.room6SeatCount, {message: 'Numărul de locuri este obligatoriu.'});
    validate(path.room6SeatCount, ({value}) =>
      Number.isInteger(value())
        ? undefined
        : {kind: 'integer', message: 'Introduceți un număr întreg.'},
    );
    min(path.room6SeatCount, 0, {message: 'Valoarea minimă este 0.'});
    max(path.room6SeatCount, MAX_ROOM_SEAT_COUNT, {
      message: `Valoarea maximă este ${MAX_ROOM_SEAT_COUNT}.`,
    });
  });

  public async submit(): Promise<RoomsFormSubmit> {
    let formData: SaveRoomConfigurationRequest | null = null;
    const isValid = await submit(this.roomsForm, async () => {
      formData = this.roomsForm().value();
    });
    return {isValid, formData};
  }
}

export type RoomsFormSubmit = {
  isValid: boolean;
  formData: SaveRoomConfigurationRequest | null;
};
