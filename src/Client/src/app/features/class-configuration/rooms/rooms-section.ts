import { Component, inject, viewChild } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ConfigurationStore } from '../services/configuration.store';
import { RoomsCard } from './rooms-card';

@Component({
  imports: [ErrorAlert, NzTypographyComponent, RoomsCard],
  selector: 'ske-configuration-rooms-section',
  templateUrl: './rooms-section.html'
})
export class RoomsSection {
  public readonly store = inject(ConfigurationStore);
  private readonly _card = viewChild(RoomsCard);

  protected async save(): Promise<void> {
    const card = this._card();
    if (!card || this.store.roomsLoading() || card.roomsForm().submitting()) return;

    const { isValid, formData } = await card.submit();
    if (!isValid || !formData || this.store.roomsLoading()) return;

    this.store.saveRooms(formData);
  }
}
