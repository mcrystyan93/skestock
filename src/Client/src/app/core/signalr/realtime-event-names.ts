export const realtimeEventNames = {
  categoryCreated: 'CategoryCreated',
  categoryUpdated: 'CategoryUpdated',
  categoryImportCreated: 'CategoryImportCreated',
  categoryImportProcessed: 'CategoryImportProcessed',
  categoryImportConfirmed: 'CategoryImportConfirmed',
  goodsReceiptImportCreated: 'GoodsReceiptImportCreated',
  goodsReceiptImportProcessed: 'GoodsReceiptImportProcessed',
  goodsReceiptImportConfirmed: 'GoodsReceiptImportConfirmed'
} as const;

export type RealtimeEventName = typeof realtimeEventNames[keyof typeof realtimeEventNames];
