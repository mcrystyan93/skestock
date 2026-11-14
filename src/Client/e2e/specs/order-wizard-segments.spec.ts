import { expect, test, type Page } from '@playwright/test';
import { saveScreenshotsAfterEach, screenshotRoot } from '../screenshots';

saveScreenshotsAfterEach();

const classId = 'class-1';
const shots = `${screenshotRoot}/order-wizard-segments`;
// 1x1 PNG, enough for a thumbnail <img> to load.
const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==', 'base64');
const longName = 'Articol cu un nume extrem de lung fără spații SuperCalifragilisticExpialidociousAbcdefghijklmnopqrstuvwxyz';

const item = (itemId: string, name: string, over: Record<string, unknown> = {}) => ({
  itemId,
  name,
  sku: null,
  imageFileId: `img-${itemId}`,
  unit: 'buc',
  lastQuantity: null,
  averageIntervalDays: null,
  isFrequent: false,
  locations: [] as { locationId: string; locationName: string; isLowStock: boolean }[],
  ...over,
});
const depozitA = { locationId: 'loc-a', locationName: 'Depozit A' };
const depozitB = { locationId: 'loc-b', locationName: 'Depozit B' };

const filler = Array.from({ length: 14 }, (_, index) =>
  item(`f-${index}`, `Articol ${index + 1}`, { lastQuantity: 2, averageIntervalDays: 40 + index, locations: [{ ...depozitA, isLowStock: false }] }),
);

const catalog = [
  // Low in A, fine in B: one id, two groups.
  item('low-1', 'Creioane', { sku: 'CR-1', lastQuantity: 5, averageIntervalDays: 12, isFrequent: true, locations: [{ ...depozitA, isLowStock: true }, { ...depozitB, isLowStock: false }] }),
  item('w-1', 'Detergent', { unit: 'cutie', lastQuantity: 2, averageIntervalDays: 7, isFrequent: true, locations: [{ ...depozitA, isLowStock: false }] }),
  item('m-1', 'Marker', { unit: 'cutie', lastQuantity: 3, averageIntervalDays: 30, locations: [{ ...depozitA, isLowStock: false }] }),
  item('m-2', longName, { unit: 'cutie', lastQuantity: 7, averageIntervalDays: 35, locations: [{ ...depozitB, isLowStock: false }] }),
  item('n-1', 'Hârtie A4', { unit: 'top', locations: [{ ...depozitB, isLowStock: true }] }),
  item('x-1', 'Tablă magnetică', { unit: 'buc', lastQuantity: 1 }),
  ...filler,
];

async function mockApi(page: Page) {
  await page.route('**/api/**', async (route) => {
    const { pathname } = new URL(route.request().url());

    if (pathname === '/api/Users/manage/info') return route.fulfill({ json: { email: 'test@example.com', isEmailConfirmed: true } });
    if (pathname === `/api/SchoolClasses/${classId}`) {
      return route.fulfill({
        json: { id: classId, name: 'Clasa E2E', startDate: '2026-09-01T00:00:00Z', endDate: '2027-06-15T00:00:00Z', status: 'active' },
      });
    }
    if (pathname === `/api/SchoolClasses/${classId}/summary`) return route.fulfill({ json: { id: classId } });
    if (pathname === `/api/OrderLists/catalog/${classId}`) return route.fulfill({ json: catalog });
    if (pathname === '/api/statistics/purchases/items-history') return route.fulfill({ json: [] });
    if (/^\/api\/Items\/[^/]+\/image$/.test(pathname)) return route.fulfill({ body: png, contentType: 'image/png' });
    return route.fulfill({ json: { data: [], hasNextPage: false, sort: [], id: classId, name: 'Clasa E2E' } });
  });
}

const viewports = [
  { name: 'phone', width: 360, height: 740 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'desktop', width: 1280, height: 800 },
];

const noHorizontalScroll = (page: Page) =>
  page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth);

for (const viewport of viewports) {
  test(`order wizard segments at ${viewport.width}x${viewport.height}`, async ({ page }) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    await mockApi(page);
    await page.goto(`/school-classes/${classId}?tab=1`);

    await page.getByRole('button', { name: 'Creează comandă' }).first().click();
    const modal = page.getByRole('dialog').first();
    await modal.locator('fieldset').getByText('Comandă nouă').click();
    await modal.getByRole('button', { name: 'Continuă' }).click();
    await expect(modal.getByText('Bifează produsele pentru comandă')).toBeVisible();

    // At desktop width the editor (and its panel) is already mounted behind this step, so the step is scoped explicitly.
    const step = modal.locator('ske-order-list-suggestions-step');
    await expect(modal.getByRole('tab')).toHaveCount(0);
    const search = step.getByRole('searchbox', { name: /Caută/ });
    await expect(search).toBeVisible();

    // Groups: Depozit A, Depozit B, then "Fără locație" last.
    const headings = await step.locator('h4').allTextContents();
    expect(headings.map((text) => text.trim()).filter(Boolean)[0]).toContain('Depozit A');
    expect(headings.at(-1)).toContain('Fără locație');

    // Icons are present for low stock and frequent items, and thumbnails load.
    await expect(step.getByRole('img', { name: 'Stoc redus' }).first()).toBeVisible();
    await expect(step.getByRole('img', { name: 'Comandat frecvent' }).first()).toBeVisible();
    const thumbnails = step.locator('ske-item-thumbnail img');
    await expect(thumbnails.first()).toBeVisible();
    expect(await noHorizontalScroll(page), 'horizontal scroll').toBe(true);

    // The search stays in view after scrolling the list.
    await step.locator('h4').filter({ hasText: 'Fără locație' }).scrollIntoViewIfNeeded();
    await expect(search).toBeInViewport();

    // A multi-location item ticks together in both groups.
    const creioane = step.getByRole('checkbox', { name: /Creioane/ });
    await expect(creioane).toHaveCount(2);
    await creioane.first().check();
    await expect(creioane.nth(1)).toBeChecked();

    await step.getByRole('checkbox', { name: /Marker/ }).check();
    await search.fill('marker');
    await expect(step.getByRole('checkbox', { name: /Creioane/ })).toHaveCount(0);
    await search.fill('');
    await step.scrollIntoViewIfNeeded();
    await page.evaluate(() => document.querySelector('.ant-modal-body')?.scrollTo(0, 0));
    await page.waitForTimeout(300);
    await page.screenshot({ path: `${shots}/order-wizard-list-${viewport.width}.png` });
    expect(await noHorizontalScroll(page)).toBe(true);

    // Details step, then the editor.
    await modal.getByRole('button', { name: 'Continuă' }).click();
    await modal.getByPlaceholder('Numele comenzii').fill('Comandă segmente');
    await modal.getByRole('button', { name: 'Continuă' }).click();

    const quantities = modal.locator('nz-input-number input');
    await expect(quantities).toHaveCount(2);
    await expect(modal.locator('nz-input-number[aria-label*="Creioane"] input')).toHaveValue('5');
    await expect(modal.locator('nz-input-number[aria-label*="Marker"] input')).toHaveValue('3');
    // At desktop width the editor also lists suggestions, so only the order lines are checked.
    const orderLines = modal.locator('ske-order-list-line');
    await expect(orderLines).toHaveCount(2);
    await expect(orderLines.getByText('Hârtie A4')).toHaveCount(0);
    await expect(orderLines.getByText('Detergent')).toHaveCount(0);
    expect(await noHorizontalScroll(page)).toBe(true);

    // Panel is lg-only: add Detergent with its last quantity (2) and see "Adăugat".
    if (viewport.width >= 1024) {
      await modal.getByRole('button', { name: 'Adaugă Detergent' }).first().click();
      await expect(modal.locator('nz-input-number[aria-label*="Detergent"] input')).toHaveValue('2');
      await expect(modal.getByText('Adăugat').first()).toBeVisible();
      await page.screenshot({ path: `${shots}/order-wizard-editor-${viewport.width}.png` });
    }
  });
}
