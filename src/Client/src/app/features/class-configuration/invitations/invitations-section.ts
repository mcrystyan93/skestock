import { Component, inject, viewChild } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { ConfigurationStore } from '../services/configuration.store';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { InvitationsCard } from './invitations-card';

@Component({
  imports: [InvitationsCard, NzTypographyComponent, ErrorAlert],
  selector: 'ske-configuration-invitations-section',
  templateUrl: './invitations-section.html',
})
export class InvitationsSection {
  public readonly store = inject(ConfigurationStore);
  private readonly _card = viewChild(InvitationsCard);

  protected async save(): Promise<void> {
    const card = this._card();
    if (!card || this.store.invitationsLoading() || card.invitationsForm().submitting()) return;

    const { isValid, formData } = await card.submit();
    if (!isValid || !formData) return;

    this.store.saveInvitations(formData);
  }
}
