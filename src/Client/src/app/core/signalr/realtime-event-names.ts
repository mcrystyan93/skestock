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
  itemImportBatchCreated: 'ItemImportBatchCreated',
  itemImportBatchProcessed: 'ItemImportBatchProcessed',
  itemImportBatchConfirmed: 'ItemImportBatchConfirmed',
  goodsReceiptImportCreated: 'GoodsReceiptImportCreated',
  goodsReceiptImportProcessed: 'GoodsReceiptImportProcessed',
  goodsReceiptImportConfirmed: 'GoodsReceiptImportConfirmed'
} as const;

export type RealtimeEventName = typeof realtimeEventNames[keyof typeof realtimeEventNames];
