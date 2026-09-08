import { eventGroup } from '@ngrx/signals/events';
import { type } from '@ngrx/signals';

export const categoryRealtimeEvents = eventGroup({
  source: 'CategoryRealtime SignalR',
  events: {
    categoryCreated: type<{ categoryId: string }>(),
    categoryUpdated: type<{ categoryId: string }>()
  }
});
