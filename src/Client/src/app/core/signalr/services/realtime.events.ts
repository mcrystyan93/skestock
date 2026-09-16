import { eventGroup } from '@ngrx/signals/events';
import { type } from '@ngrx/signals';

export const realtimeEvents = eventGroup({
  source: 'Realtime SignalR',
  events: {
    categoryCreated: type<{ categoryId: string }>(),
    categoryUpdated: type<{ categoryId: string }>(),
    categoryImportBatchCreated: type<{ categoryImportBatchId: string }>(),
    categoryImportBatchProcessed: type<{ categoryImportBatchId: string }>(),
    categoryImportBatchConfirmed: type<{ categoryImportBatchId: string }>(),
    itemCreated: type<{ itemId: string }>(),
    itemUpdated: type<{ itemId: string }>(),
    itemDisabled: type<{ itemId: string }>(),
    itemEnabled: type<{ itemId: string }>(),
    itemImportBatchCreated: type<{ itemImportBatchId: string }>(),
    itemImportBatchProcessed: type<{ itemImportBatchId: string }>(),
    itemImportBatchConfirmed: type<{ itemImportBatchId: string }>(),
    goodsReceiptImportCreated: type<{ goodsReceiptImportId: string }>(),
    goodsReceiptImportProcessed: type<{ goodsReceiptImportId: string }>(),
    goodsReceiptImportConfirmed: type<{ goodsReceiptImportId: string }>(),
    stockAdjusted: type<{ classId: string, locationId: string }>(),
    stockMoved: type<{
      classId: string,
      itemId: string,
      sourceLocationId: string,
      destinationLocationId: string,
      quantity: number
    }>(),
    stockBatchCreated: type<{ classId: string, locationId: string }>(),
    classItemStockVisibilityChanged: type<{
      classId: string,
      itemId: string,
      hideWhenZeroStock: boolean
    }>()
  }
});
