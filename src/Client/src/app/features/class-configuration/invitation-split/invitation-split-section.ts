import { Component, inject, viewChild } from '@angular/core';
import { ErrorAlert } from '@ske/shared/errors';
import { NzTypographyComponent } from 'ng-zorro-antd/typography';
import { ConfigurationStore } from '../services/configuration.store';
import { InvitationSplitCard } from './invitation-split-card';

@Component({
  imports: [ErrorAlert, NzTypographyComponent, InvitationSplitCard],
  selector: 'ske-configuration-invitation-split-section',
  templateUrl: './invitation-split-section.html'
})
export class InvitationSplitSection {
  public readonly store = inject(ConfigurationStore);
  private readonly _card = viewChild(InvitationSplitCard);

  protected async save(): Promise<void> {
    const card = this._card();
    if (!card || this.store.invitationSplitLoading() || card.splitForm().submitting()) return;

    const { isValid, formData } = await card.submit();
    if (!isValid || !formData || this.store.invitationSplitLoading()) return;

    this.store.saveInvitationSplit(formData);
  }
}
