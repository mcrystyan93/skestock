export const realtimeGroups = {
  categoriesList: 'categories-list',
  categoryImportsList: 'category-imports-list',
  goodsReceiptImportsList: 'goods-receipts-import-list'
} as const;

export type RealtimeGroup = typeof realtimeGroups[keyof typeof realtimeGroups];
