import { eventGroup } from '@ngrx/signals/events';
import { type } from '@ngrx/signals';

export const StockEvents = eventGroup({
  source: 'Stock API',
  events: {
    adjustSuccess: type<void>(),
    addProduct: type<void>()
  }
});
