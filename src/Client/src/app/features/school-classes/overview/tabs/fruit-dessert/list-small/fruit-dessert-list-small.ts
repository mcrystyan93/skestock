import { Component, input, output } from '@angular/core';
import { NzCardComponent } from 'ng-zorro-antd/card';
import { NzEmptyComponent } from 'ng-zorro-antd/empty';
import { NzSkeletonComponent } from 'ng-zorro-antd/skeleton';
import { FruitDessertMessageType, type FruitDessertPlanEntryDto } from '@ske/models';
import { FruitDessertCard } from '../card/fruit-dessert-card';

@Component({
  selector: 'ske-fruit-dessert-list-small',
  imports: [FruitDessertCard, NzCardComponent, NzEmptyComponent, NzSkeletonComponent],
  templateUrl: './fruit-dessert-list-small.html',
  host: { class: 'absolute inset-0 flex flex-col overflow-hidden' }
})
export class FruitDessertListSmall {
  public readonly entries = input.required<readonly FruitDessertPlanEntryDto[]>();
  public readonly loading = input<boolean>(false);
  public readonly readOnly = input<boolean>(false);
  public readonly sending = input<boolean>(false);
  public readonly downloading = input<boolean>(false);
  /** Entry whose popup was blocked, with the URL the user can still open. */
  public readonly fallback = input<{ entryId: string; url: string } | null>(null);

  public readonly congregationChange = output<{ entryId: string; congregationId: string | null }>();
  public readonly emailSentChange = output<{ entryId: string; emailSent: boolean }>();
  public readonly sendMessage = output<{ entryId: string; type: FruitDessertMessageType }>();
  public readonly downloadInformare = output<{ entryId: string }>();

  protected readonly skeletonPlaceholders = [0, 1, 2, 3];
}
