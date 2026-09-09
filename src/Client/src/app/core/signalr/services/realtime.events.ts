import { eventGroup } from '@ngrx/signals/events';
import { type } from '@ngrx/signals';

export const realtimeEvents = eventGroup({
  source: 'Realtime SignalR',
  events: {
    categoryCreated: type<{ categoryId: string }>(),
    categoryUpdated: type<{ categoryId: string }>(),
    categoryImportCreated: type<{ categoryImportId: string }>(),
    categoryImportProcessed: type<{ categoryImportId: string }>(),
    categoryImportConfirmed: type<{ categoryImportId: string }>(),
    itemCreated: type<{ itemId: string }>(),
    itemUpdated: type<{ itemId: string }>(),
    itemDisabled: type<{ itemId: string }>(),
    itemEnabled: type<{ itemId: string }>(),
    itemImportCreated: type<{ itemImportId: string }>(),
    itemImportProcessed: type<{ itemImportId: string }>(),
    itemImportConfirmed: type<{ itemImportId: string }>(),
    goodsReceiptImportCreated: type<{ goodsReceiptImportId: string }>(),
    goodsReceiptImportProcessed: type<{ goodsReceiptImportId: string }>(),
    goodsReceiptImportConfirmed: type<{ goodsReceiptImportId: string }>()
  }
});
