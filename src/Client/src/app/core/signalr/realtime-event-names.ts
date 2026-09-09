export const realtimeEventNames = {
  categoryCreated: 'CategoryCreated',
  categoryUpdated: 'CategoryUpdated',
  categoryImportCreated: 'CategoryImportCreated',
  categoryImportProcessed: 'CategoryImportProcessed',
  categoryImportConfirmed: 'CategoryImportConfirmed',
  itemCreated: 'ItemCreated',
  itemUpdated: 'ItemUpdated',
  itemDisabled: 'ItemDisabled',
  itemEnabled: 'ItemEnabled',
  itemImportCreated: 'ItemImportCreated',
  itemImportProcessed: 'ItemImportProcessed',
  itemImportConfirmed: 'ItemImportConfirmed',
  goodsReceiptImportCreated: 'GoodsReceiptImportCreated',
  goodsReceiptImportProcessed: 'GoodsReceiptImportProcessed',
  goodsReceiptImportConfirmed: 'GoodsReceiptImportConfirmed'
} as const;

export type RealtimeEventName = typeof realtimeEventNames[keyof typeof realtimeEventNames];
