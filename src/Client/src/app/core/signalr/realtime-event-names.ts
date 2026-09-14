export const realtimeEventNames = {
  categoryCreated: 'CategoryCreated',
  categoryUpdated: 'CategoryUpdated',
  categoryImportBatchCreated: 'CategoryImportBatchCreated',
  categoryImportBatchProcessed: 'CategoryImportBatchProcessed',
  categoryImportBatchConfirmed: 'CategoryImportBatchConfirmed',
  itemCreated: 'ItemCreated',
  itemUpdated: 'ItemUpdated',
  itemDisabled: 'ItemDisabled',
  itemEnabled: 'ItemEnabled',
  itemImportBatchCreated: 'ItemImportBatchCreated',
  itemImportBatchProcessed: 'ItemImportBatchProcessed',
  itemImportBatchConfirmed: 'ItemImportBatchConfirmed',
  goodsReceiptImportCreated: 'GoodsReceiptImportCreated',
  goodsReceiptImportProcessed: 'GoodsReceiptImportProcessed',
  goodsReceiptImportConfirmed: 'GoodsReceiptImportConfirmed',
  stockAdjusted: 'StockAdjusted',
  stockBatchCreated: 'StockBatchCreated'
} as const;

export type RealtimeEventName = typeof realtimeEventNames[keyof typeof realtimeEventNames];
