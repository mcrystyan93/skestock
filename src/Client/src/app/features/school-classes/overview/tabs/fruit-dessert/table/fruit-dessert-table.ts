import { Component, input, output } from '@angular/core';
import { NzTableModule } from 'ng-zorro-antd/table';
import { FruitDessertMessageType, type FruitDessertPlanEntryDto } from '@ske/models';
import { BaseTable } from '@ske/shared/tables';
import { FruitDessertRow } from '../row/fruit-dessert-row';

@Component({
  selector: 'ske-fruit-dessert-table',
  imports: [NzTableModule, FruitDessertRow],
  templateUrl: './fruit-dessert-table.html',
  host: {
    class: 'absolute block inset-0'
  }
})
export class FruitDessertTable extends BaseTable {
  public readonly entries = input.required<readonly FruitDessertPlanEntryDto[]>();
  public readonly loading = input<boolean>(false);
  public readonly readOnly = input<boolean>(false);
  public readonly sending = input<boolean>(false);
  /** Entry whose popup was blocked, with the URL the user can still open. */
  public readonly fallback = input<{ entryId: string; url: string } | null>(null);

  public readonly congregationChange = output<{ entryId: string; congregationId: string | null }>();
  public readonly emailSentChange = output<{ entryId: string; emailSent: boolean }>();
  public readonly sendMessage = output<{ entryId: string; type: FruitDessertMessageType }>();

  public readonly columnLayout = {
    date: { width: '210px' },
    congregation: { width: '260px' },
    contactPerson: { width: '180px' },
    phone: { width: '140px' },
    email: { width: '140px' },
    whatsapp: { width: '420px' }
  } as const;
}
