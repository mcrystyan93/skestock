import {expect, test, type Page} from '@playwright/test';
import {saveScreenshotsAfterEach} from '../screenshots';

saveScreenshotsAfterEach();

const classId = 'class-1';

const line = (itemId: string, itemName: string, locationId: string, locationName: string, quantity: number, extra = {}) => ({
  itemId, itemName, sku: null, categoryId: 'cat-1', categoryName: 'Alimente',
  locationId, locationName, lastUpdatedAt: '2026-09-20T10:00:00Z', unit: 'kg',
  isPerishable: true, isExpired: false, expiredQuantity: 0, quantity, isLowStock: false,
  hideWhenZeroStock: false, ...extra,
});

const stock = {
  hasExpiredItems: true,
  items: [
    line('i1', 'Mere', 'l1', 'Cămară', 2, {isLowStock: true, imageFileId: 'f1'}),
    line('i1', 'Mere', 'l2', 'Frigider', 8, {isExpired: true, expiredQuantity: 3}),
    line('i1', 'Mere', 'l3', 'Magazie', 5),
    line('i2', 'Făină', 'l1', 'Cămară', 10),
  ],
};

async function mockApi(page: Page) {
  await page.route('**/api/**', async (route) => {
    const {pathname} = new URL(route.request().url());
    if (pathname === '/api/Users/manage/info') return route.fulfill({json: {email: 'test@example.com', isEmailConfirmed: true}});
    if (pathname === `/api/SchoolClasses/${classId}`) {
      return route.fulfill({json: {id: classId, name: 'Clasa E2E', startDate: '2026-09-01T00:00:00Z', endDate: '2027-06-15T00:00:00Z', status: 'active'}});
    }
    if (pathname === `/api/Stock/class/${classId}`) return route.fulfill({json: stock});
    return route.fulfill({json: {data: [], hasNextPage: false, sort: [], labels: [], series: [], points: [], id: classId, name: 'Clasa E2E'}});
  });
}

for (const width of [360, 768, 900, 1100, 1440]) {
  test(`stock tab lists an item once with its locations at ${width}px`, async ({page}) => {
    await mockApi(page);
    await page.setViewportSize({width, height: 900});
    await page.goto(`/school-classes/${classId}?tab=0`);

    const stockTab = page.locator('ske-class-stock');
    await expect(stockTab.getByText('Mere', {exact: true})).toHaveCount(1);
    await expect(stockTab.getByText('Cămară').first()).toBeVisible();
    await expect(stockTab.getByText('Frigider')).toBeVisible();
    await expect(stockTab.getByText('Expiră (3)')).toBeVisible();
    // thumbnails stay on every layout, they are what makes a product recognisable at a glance
    await expect(stockTab.locator('img[src*="/api/Items/i1/image"]')).toHaveCount(1);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  });
}

test('clicking an item row asks for the location only when there are several', async ({page}) => {
  await mockApi(page);
  await page.setViewportSize({width: 1440, height: 900});
  await page.goto(`/school-classes/${classId}?tab=0`);

  const stockTab = page.locator('ske-class-stock');
  await stockTab.locator('tr', {hasText: 'Mere'}).click({position: {x: 140, y: 20}});
  const picker = page.locator('.ant-modal').filter({hasText: 'Alege locația'});
  await expect(picker).toBeVisible();
  await expect(picker.getByText('Frigider')).toBeVisible();

  await picker.getByText('Frigider').click();
  await expect(page.locator('.ant-modal').filter({hasText: 'Alege locația'})).toHaveCount(0);
  await expect(page.locator('ske-stock-adjustment-modal')).toBeVisible();
});

test('clicking a single-location item row opens the adjustment modal directly', async ({page}) => {
  await mockApi(page);
  await page.setViewportSize({width: 1440, height: 900});
  await page.goto(`/school-classes/${classId}?tab=0`);

  await page.locator('ske-class-stock tr', {hasText: 'Făină'}).click({position: {x: 140, y: 20}});
  await expect(page.locator('ske-stock-adjustment-modal')).toBeVisible();
  await expect(page.getByText('Alege locația')).toHaveCount(0);
});

test('phone actions menu opens on first tap and offers a location per action', async ({page}) => {
  await mockApi(page);
  await page.setViewportSize({width: 390, height: 844});
  await page.goto(`/school-classes/${classId}?tab=0`);

  await page.getByRole('button', {name: 'Acțiuni pentru Mere'}).click();
  await expect(page.getByText('Mută stocul')).toBeVisible();
});
