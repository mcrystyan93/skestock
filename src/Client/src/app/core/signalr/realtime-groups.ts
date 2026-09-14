export const realtimeGroups = {
  categoriesList: 'categories-list',
  categoryImportBatchesList: 'category-import-batches-list',
  itemsList: 'items-list',
  itemImportBatchesList: 'item-import-batches-list',
  goodsReceiptImportsList: 'goods-receipts-import-list',
  schoolClass: (classId: string): `school-class:${string}` => `school-class:${classId}`
} as const;

export type RealtimeGroup =
  | Extract<typeof realtimeGroups[keyof typeof realtimeGroups], string>
  | ReturnType<typeof realtimeGroups.schoolClass>;
