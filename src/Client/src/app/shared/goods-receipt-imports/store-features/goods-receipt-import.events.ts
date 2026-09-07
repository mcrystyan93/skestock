import { eventGroup } from '@ngrx/signals/events';
import { type } from '@ngrx/signals';

export const goodsReceiptImportRealtimeEvents = eventGroup({
  source: 'GoodsReceiptImportRealtime SignalR',
  events: {
    goodsReceiptImportCreated: type<{ goodsReceiptImportId: string }>(),
    goodsReceiptImportProcessed: type<{ goodsReceiptImportId: string }>(),
  }
});
