import {expect, test, type Page} from '@playwright/test';
import {saveScreenshotsAfterEach} from '../screenshots';

saveScreenshotsAfterEach();

const classId = 'class-1';
const line = (locationId: string, locationName: string, quantity: number) => ({
  itemId: 'i1', itemName: 'Mere', sku: null, categoryId: 'cat-1', categoryName: 'Alimente',
  locationId, locationName, lastUpdatedAt: '2026-09-20T10:00:00Z', unit: 'kg',
  isPerishable: false, isExpired: false, expiredQuantity: 0, quantity, isLowStock: false, hideWhenZeroStock: false,
});
const location = (id: string, name: string, parent: string | null = null) => ({
  id, name, type: 'room', isDefault: false, parentLocationName: parent,
  createdDate: '2026-01-01T00:00:00Z', lastModifiedDate: '2026-01-01T00:00:00Z',
});
const locations = [
  location('l1', 'Cămară'), location('l2', 'Frigider', 'Bucătărie'), location('l3', 'Magazie'),
  location('l4', 'Congelator mare', 'Bucătărie'), location('l5', 'Sala de mese'),
];

async function mockApi(page: Page) {
  await page.route('**/api/**', async (route) => {
    const {pathname} = new URL(route.request().url());
    if (pathname === '/api/Users/manage/info') return route.fulfill({json: {email: 'test@example.com', isEmailConfirmed: true}});
    if (pathname === `/api/SchoolClasses/${classId}`) {
      return route.fulfill({json: {id: classId, name: 'Clasa E2E', startDate: '2026-09-01T00:00:00Z', endDate: '2027-06-15T00:00:00Z', status: 'active'}});
    }
    if (pathname === `/api/Stock/class/${classId}`) return route.fulfill({json: {hasExpiredItems: false, items: [line('l1', 'Cămară', 8)]}});
    if (pathname === '/api/Locations/get-all') return route.fulfill({json: {data: locations, hasNextPage: false, sort: []}});
    return route.fulfill({json: {data: [], hasNextPage: false, sort: []}});
  });
}

for (const [width, height, columns] of [[390, 844, 2], [768, 900, 3], [1440, 900, 3]]) {
  test(`move modal shows location cards in a grid at ${width}px`, async ({page}) => {
    await mockApi(page);
    await page.setViewportSize({width, height});
    await page.goto(`/school-classes/${classId}?tab=0`);

    if (width < 1000) {
      await page.getByRole('button', {name: 'Acțiuni pentru Mere'}).click();
      await page.getByText('Mută stocul').click();
    } else {
      await page.getByRole('button', {name: 'Mută stocul pentru Mere'}).click();
    }
    const modal = page.locator('ske-stock-move-modal');
    const cards = modal.locator('label[nz-radio]');
    await expect(cards).toHaveCount(4); // source location is excluded

    const lefts = new Set(await cards.evaluateAll((els) => els.map((e) => Math.round(e.getBoundingClientRect().left))));
    expect(lefts.size).toBe(columns);
    await cards.nth(columns - 1).click();
    await page.waitForTimeout(300); // let the selected-card transition finish before the screenshot
    await expect(cards.nth(columns - 1).locator('input')).toBeChecked();
    // the radio's checked animation used to overflow the modal body when the last card of a row was picked
    expect(await modal.evaluate((el) => { const body = el.closest('.ant-modal-body')!; return body.scrollWidth <= body.clientWidth; })).toBe(true);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  });
}
