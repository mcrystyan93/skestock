import { eventGroup } from '@ngrx/signals/events';
import { type } from '@ngrx/signals';

export const goodsReceiptImportRealtimeEvents = eventGroup({
  source: 'GoodsReceiptImportRealtime SignalR',
  events: {
    markListAsChanged: type<{ importId: string }>(),
    notifyUser: type<{ importId: string }>()
  }
});
