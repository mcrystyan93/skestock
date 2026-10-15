import { expect, test, type Page } from '@playwright/test';

const receiptIds = ['receipt-1', 'receipt-2', 'receipt-3'];
const savedLines = [
  { id: 'saved-line-1', importId: 'import-1', rawItemText: 'Denumire originală din factură',
    matchedItemId: null, locationId: null, locationName: null, quantity: 8, unitPrice: 3.25,
    lineTotal: 26, expiryDate: '2027-02-01' },
  { id: 'saved-line-2', importId: 'import-1', rawItemText: 'Denumire originală din factură',
    matchedItemId: null, locationId: 'location-1', locationName: 'Depozit', quantity: 4,
    unitPrice: 3.25, lineTotal: 13, expiryDate: null }
];

test('receipt expansion lazily reads saved lines and keeps concurrent receipts independent', async ({ page }) => {
  const { requestedLines, stockBatchRequests } = await openReceipts(page);
  expect(requestedLines).toEqual([]);
  const expand = page.getByRole('button', { name: 'Vezi articolele', exact: true });
  await expand.nth(0).focus();
  await page.keyboard.press('Enter');
  const first = page.locator('#receipt-lines-receipt-1');
  await expect(first.getByText('Denumire originală din factură').filter({ visible: true }).first()).toBeVisible();
  await expect(first).toContainText('8');
  await expect(first).toContainText('26');
  await expect(first).toContainText('Depozit');
  await expand.nth(0).click();
  const second = page.locator('#receipt-lines-receipt-2');
  await expect(second).toContainText('Nu există linii salvate pentru această recepție');
  await expect(first.getByText('Denumire originală din factură').filter({ visible: true }).first()).toBeVisible();
  expect(requestedLines).toEqual(['receipt-1', 'receipt-2']);
  expect(stockBatchRequests).toEqual([]);

  await page.getByRole('button', { name: 'Ascunde articolele', exact: true }).first().click();
  await expect(first).toBeHidden();
  await page.getByRole('button', { name: 'Vezi articolele', exact: true }).first().click();
  await expect(first.getByText('Denumire originală din factură').filter({ visible: true }).first()).toBeVisible();
  expect(stockBatchRequests).toEqual([]);
  await expect(page.getByRole('button', { name: /Descarc.*fișierul|Descarca fisierul/ }).first()).toBeEnabled();
});

test('snapshot failure offers retry instead of a successful empty state', async ({ page }) => {
  await openReceipts(page, true);
  await page.getByRole('button', { name: 'Vezi articolele', exact: true }).last().click();
  const third = page.locator('#receipt-lines-receipt-3');
  await expect(third.getByText('Încărcarea a eșuat', { exact: true }).first()).toBeVisible();
  await expect(third).not.toContainText('Nu există linii salvate');
  await third.getByRole('button', { name: 'Reîncearcă încărcarea liniilor' }).click();
  await expect(third.getByText('Denumire originală din factură').filter({ visible: true }).first()).toBeVisible();
});

test('long saved item labels wrap within the snapshot layout', async ({ page }) => {
  await openReceipts(page);
  const label = 'Articol importat cu o descriere lungă '.repeat(10).trim();
  await page.route('**/api/GoodsReceipts/receipt-1/import-lines', route =>
    route.fulfill({ json: [{ ...savedLines[0], rawItemText: label }] }));
  await page.getByRole('button', { name: 'Vezi articolele', exact: true }).first().click();
  const visibleLabel = page.locator('#receipt-lines-receipt-1').getByText(label, { exact: true })
    .filter({ visible: true }).first();
  await expect(visibleLabel).toBeVisible();
  expect(await visibleLabel.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
});

async function openReceipts(page: Page, failFirstRequest = false) {
  const requestedLines: string[] = [];
  const stockBatchRequests: string[] = [];
  let failed = false;
  await page.route('**/api/**', async route => {
    const { pathname } = new URL(route.request().url());
    if (pathname.toLowerCase().includes('stockbatches')) stockBatchRequests.push(pathname);
    if (pathname === '/api/Users/manage/info') {
      await route.fulfill({ json: { email: 'test@example.com', isEmailConfirmed: true } });
    } else if (pathname === '/api/SchoolClasses/class-1') {
      await route.fulfill({ json: { id: 'class-1', name: 'Clasa E2E', startDate: '2026-09-01', endDate: '2027-06-15' } });
    } else if (pathname === '/api/SchoolClasses/class-1/summary') {
      await route.fulfill({ json: { id: 'class-1' } });
    } else if (pathname === '/api/GoodsReceipts/get-all') {
      await route.fulfill({ json: {
        data: receiptIds.map((id, index) => ({ id, classId: 'class-1', className: 'Clasa E2E',
          receivedAt: '2026-10-14T08:00:00Z', note: `Recepție ${index + 1}`, lineCount: 2,
          totalQuantity: 12, totalAmount: 39, createdByName: 'Test user',
          createdDate: '2026-10-14T08:00:00Z', fileMetadataId: 'file-1' })),
        hasNextPage: false, sort: []
      } });
    } else if (pathname.endsWith('/import-lines')) {
      const id = pathname.split('/')[3];
      requestedLines.push(id);
      if (failFirstRequest && id === 'receipt-3' && !failed) {
        failed = true;
        await route.fulfill({ status: 500, json: { status: 500, title: 'Încărcarea a eșuat' } });
      } else {
        await route.fulfill({ json: id === 'receipt-2' ? [] : savedLines });
      }
    } else {
      await route.fulfill({ json: { data: [], hasNextPage: false, sort: [], items: [] } });
    }
  });
  await page.goto('/school-classes/class-1?tab=2');
  await expect(page.getByText('Recepție 1', { exact: true }).first()).toBeVisible();
  return { requestedLines, stockBatchRequests };
}
