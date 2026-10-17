import { expect, test, type Locator, type Page, type TestInfo } from '@playwright/test';

const classId = 'class-1';
const longLocation = 'Depozit B cu o denumire extrem de lungă pentru verificarea împachetării textului';
const longCategory = 'Categorie cu un nume extrem de lung care trebuie să fie împachetat pe mai multe rânduri';
const longItem =
  'Articol cu un nume extrem de lung fără spații intermediare SuperCalifragilisticExpialidociousAbcdefghijklmnop';

const row = (itemId: string, itemName: string, categoryName: string, locationId: string, locationName: string, quantity: number) => ({
  itemId,
  itemName,
  sku: null,
  categoryId: `cat-${categoryName}`,
  categoryName,
  categoryIcon: null,
  locationId,
  locationName,
  lastUpdatedAt: null,
  unit: 'buc',
  isPerishable: false,
  isExpired: false,
  expiredQuantity: 0,
  quantity,
  isLowStock: false,
  hideWhenZeroStock: false,
});

// Walk order (location -> category -> item): Creioane, Hârtie A4, long item, Detergent. Radiere (0) is skipped.
const stockItems = [
  row('i-detergent', 'Detergent', 'Curățenie', 'loc-b', longLocation, 4),
  row('i-long', longItem, longCategory, 'loc-b', longLocation, 8),
  row('i-radiere', 'Radiere', 'Papetărie', 'loc-a', 'Depozit A', 0),
  row('i-hartie', 'Hârtie A4', 'Papetărie', 'loc-a', 'Depozit A', 5),
  row('i-creioane', 'Creioane', 'Papetărie', 'loc-a', 'Depozit A', 10),
];

type Options = { status?: 'active' | 'closed'; bulkStatuses?: number[]; tab?: number };

async function openStockTab(page: Page, options: Options = {}) {
  const bulkBodies: Array<{ reason: number; lines: Array<{ itemId: string; locationId: string; actualQuantity: number }> }> = [];
  const stat = { stockLoads: 0 };
  const bulkStatuses = [...(options.bulkStatuses ?? [])];

  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const { pathname } = new URL(request.url());

    if (pathname === '/api/Users/manage/info') {
      return route.fulfill({ json: { email: 'test@example.com', isEmailConfirmed: true } });
    }
    if (pathname === `/api/SchoolClasses/${classId}`) {
      return route.fulfill({
        json: {
          id: classId,
          name: 'Clasa E2E',
          startDate: '2026-09-01T00:00:00Z',
          endDate: '2027-06-15T00:00:00Z',
          status: options.status ?? 'active',
        },
      });
    }
    if (pathname === `/api/SchoolClasses/${classId}/summary`) {
      return route.fulfill({ json: { id: classId } });
    }
    if (pathname === `/api/Stock/class/${classId}` && request.method() === 'POST') {
      stat.stockLoads++;
      return route.fulfill({ json: { items: stockItems, hasExpiredItems: false } });
    }
    if (pathname === `/api/Stock/class/${classId}/bulk-adjust`) {
      bulkBodies.push(request.postDataJSON());
      const status = bulkStatuses.shift() ?? 200;
      if (status === 409) {
        return route.fulfill({
          status: 409,
          contentType: 'application/problem+json',
          body: JSON.stringify({
            status: 409,
            title: 'Conflict',
            error: { code: 'stock.bulk_concurrency_conflict', errors: [{ params: {} }] },
          }),
        });
      }
      if (status === 500) {
        return route.fulfill({
          status: 500,
          contentType: 'application/problem+json',
          body: JSON.stringify({ status: 500, title: 'Eroare' }),
        });
      }
      return route.fulfill({ status: 200, body: '' });
    }
    return route.fulfill({ json: { data: [], hasNextPage: false, sort: [], id: classId, name: 'Clasa E2E' } });
  });

  await page.goto(`/school-classes/${classId}${options.tab ? `?tab=${options.tab}` : ''}`);
  return { bulkBodies, stat };
}

const countButton = (page: Page) => page.getByRole('button', { name: 'Inventar stoc' });
const modal = (page: Page) => page.getByRole('dialog').first();
const quantity = (page: Page) => page.getByLabel('Cantitate numărată');

async function openWizard(page: Page) {
  await countButton(page).click();
  await expect(modal(page).getByText('Selectează toate')).toBeVisible();
}

async function toItems(page: Page) {
  await modal(page).getByText('Selectează toate').click();
  await modal(page).getByRole('button', { name: 'Următorul' }).click();
  await expect(modal(page).getByLabel('Poziție')).toHaveText('1 / 4');
}

async function setCount(page: Page, value: number) {
  await quantity(page).fill(String(value));
  await quantity(page).blur();
}

const next = (page: Page) => modal(page).getByRole('button', { name: 'Următorul' }).click();

test.describe('stock count wizard', () => {
  test('button is shown on the Stoc tab', async ({ page }) => {
    await openStockTab(page);
    await expect(countButton(page)).toBeVisible();
  });

  test('button is hidden on other tabs', async ({ page }) => {
    await openStockTab(page, { tab: 1 });
    await expect(page.getByTitle('Clasa E2E')).toBeVisible();
    await page.waitForTimeout(500);
    await expect(countButton(page)).toHaveCount(0);
  });

  test('button is hidden for a closed class', async ({ page }) => {
    await openStockTab(page, { status: 'closed' });
    await expect(page.getByTitle('Clasa E2E')).toBeVisible();
    await page.waitForTimeout(500);
    await expect(countButton(page)).toHaveCount(0);
  });

  test('happy path over two locations sends one bulk request and reloads the list', async ({ page }) => {
    const { bulkBodies, stat } = await openStockTab(page);
    await expect.poll(() => stat.stockLoads).toBeGreaterThan(0);
    await openWizard(page);
    const loadsBeforeSave = stat.stockLoads;

    await modal(page).locator('nz-select').click();
    await page.locator('.ant-select-item-option').filter({ hasText: 'Deteriorat' }).click();
    await toItems(page);

    // 1: Creioane 10 -> 7 (warning)
    await expect(modal(page).getByRole('heading', { name: 'Creioane' })).toBeVisible();
    await setCount(page, 7);
    await expect(modal(page).getByText('Cantitatea va fi redusă cu 3 buc')).toBeVisible();
    await next(page);

    // 2: Hârtie A4 5 -> 9 (success)
    await expect(modal(page).getByLabel('Poziție')).toHaveText('2 / 4');
    await setCount(page, 9);
    await expect(modal(page).getByText('Cantitatea va fi crescută cu 4 buc')).toBeVisible();
    await next(page);

    // 3 and 4 untouched; back keeps the entered value
    await expect(modal(page).getByLabel('Poziție')).toHaveText('3 / 4');
    await modal(page).getByRole('button', { name: 'Înapoi' }).click();
    await expect(quantity(page)).toHaveValue('9');
    await next(page);
    await next(page);
    await next(page);

    await expect(modal(page).getByText('2 articole nemodificate')).toBeVisible();
    const summary = modal(page);
    await expect(summary.getByText('10 → 7')).toBeVisible();
    await expect(summary.getByText('5 → 9')).toBeVisible();
    await expect(summary.getByText('-3')).toBeVisible();
    await expect(summary.getByText('+4')).toBeVisible();
    await expect(summary.getByText('Detergent')).toHaveCount(0);

    await summary.getByRole('button', { name: 'Finalizează' }).click();
    await expect(page.getByText('Inventarul de stoc a fost salvat cu succes!')).toBeVisible();
    await expect(page.getByRole('dialog')).toHaveCount(0);
    await expect.poll(() => stat.stockLoads).toBeGreaterThan(loadsBeforeSave);
    expect(bulkBodies).toEqual([
      {
        reason: 2,
        lines: [
          { itemId: 'i-creioane', locationId: 'loc-a', actualQuantity: 7 },
          { itemId: 'i-hartie', locationId: 'loc-a', actualQuantity: 9 },
        ],
      },
    ]);
  });

  test('"Sari la rezumat" treats untouched items as unchanged', async ({ page }) => {
    await openStockTab(page);
    await openWizard(page);
    await toItems(page);

    await modal(page).getByRole('button', { name: 'Sari la rezumat' }).click();
    await expect(modal(page).getByText('4 articole nemodificate')).toBeVisible();
    await expect(modal(page).getByText('Nu există modificări de salvat.')).toBeVisible();
  });

  test('Finalizează is disabled when nothing changed', async ({ page }) => {
    await openStockTab(page);
    await openWizard(page);
    await toItems(page);
    await modal(page).getByRole('button', { name: 'Sari la rezumat' }).click();
    await expect(modal(page).getByRole('button', { name: 'Finalizează' })).toBeDisabled();
  });

  test('a 409 shows the error in the modal, keeps the values and a retry succeeds', async ({ page }) => {
    const { bulkBodies } = await openStockTab(page, { bulkStatuses: [409, 200] });
    await openWizard(page);
    await toItems(page);
    await setCount(page, 6);
    await modal(page).getByRole('button', { name: 'Sari la rezumat' }).click();
    await modal(page).getByRole('button', { name: 'Finalizează' }).click();

    await expect(
      modal(page).getByText('Stocul a fost modificat în timpul inventarierii. Reîncărcați și încercați din nou.'),
    ).toBeVisible();
    await expect(modal(page).getByText('10 → 6')).toBeVisible();
    expect(bulkBodies).toHaveLength(1);

    await modal(page).getByRole('button', { name: 'Finalizează' }).click();
    await expect(page.getByText('Inventarul de stoc a fost salvat cu succes!')).toBeVisible();
    expect(bulkBodies).toHaveLength(2);
    expect(bulkBodies[1]).toEqual(bulkBodies[0]);
  });

  test('closing with unsaved changes asks for confirmation', async ({ page }) => {
    const { bulkBodies } = await openStockTab(page);
    await openWizard(page);
    await toItems(page);
    await setCount(page, 3);

    await modal(page).getByRole('button', { name: 'Închide' }).click();
    const confirmation = page.locator('.ant-modal-confirm');
    await expect(confirmation).toContainText('Renunțați la modificări?');
    await confirmation.getByRole('button', { name: 'Continuă editarea' }).click();
    await expect(confirmation).toHaveCount(0);
    await expect(quantity(page)).toHaveValue('3');

    await modal(page).getByRole('button', { name: 'Închide' }).click();
    await confirmation.getByRole('button', { name: 'Renunță' }).click();
    await expect(page.getByRole('dialog')).toHaveCount(0);
    expect(bulkBodies).toEqual([]);
  });

  test('clicking a summary row returns to that item', async ({ page }) => {
    await openStockTab(page);
    await openWizard(page);
    await toItems(page);
    await next(page);
    await expect(modal(page).getByLabel('Poziție')).toHaveText('2 / 4');
    await setCount(page, 1); // Hârtie A4
    await modal(page).getByRole('button', { name: 'Sari la rezumat' }).click();

    // table row at md+, card button below md
    await modal(page).locator('tbody button, button.w-full').filter({ hasText: 'Hârtie A4' }).click();
    await expect(modal(page).getByRole('heading', { name: 'Hârtie A4' })).toBeVisible();
    await expect(modal(page).getByLabel('Poziție')).toHaveText('2 / 4');
    await expect(quantity(page)).toHaveValue('1');
  });
});

const widths = [360, 576, 768, 992, 1280, 1600];

test.describe('stock count wizard responsive', () => {
  test.beforeEach(({}, testInfo) => {
    test.skip(testInfo.project.name !== 'chromium', 'layout checks run once, in chromium');
  });

  for (const width of widths) {
    test(`layout holds at ${width}px`, async ({ page }, testInfo) => {
      await page.setViewportSize({ width, height: 800 });
      await openStockTab(page);
      const compact = width < 992;

      // header button: icon-only below lg, icon + text from lg
      const button = countButton(page);
      await expect(button).toBeVisible();
      const hasText = ((await button.innerText()).trim()).length > 0;
      expect(hasText, 'header button text').toBe(!compact);
      await expectInViewport(page, button);

      await button.click();
      await expect(modal(page).getByText('Selectează toate')).toBeVisible();

      await checkStep(page, testInfo, width, '1-locatii', ['Locații', 'Articole', 'Rezumat']);

      await toItems(page);
      await next(page);
      await next(page); // long-named item
      await expect(modal(page).getByRole('heading', { name: longItem })).toBeVisible();
      await setCount(page, 1);
      await checkStep(page, testInfo, width, '2-articole', ['Locații', 'Articole', 'Rezumat']);

      await modal(page).getByRole('button', { name: 'Sari la rezumat' }).click();
      await expect(modal(page).getByText('articole nemodificate')).toBeVisible();
      await expect(modal(page).locator('nz-table')).toHaveCount(width >= 768 ? 1 : 0);
      await expect(modal(page).locator('button.w-full')).toHaveCount(width >= 768 ? 0 : 1);
      await checkStep(page, testInfo, width, '3-rezumat', ['Locații', 'Articole', 'Rezumat']);
    });
  }
});

async function expectInViewport(page: Page, locator: Locator) {
  const box = await locator.boundingBox();
  const vw = page.viewportSize()!.width;
  expect(box, 'bounding box').not.toBeNull();
  expect(box!.x).toBeGreaterThanOrEqual(0);
  expect(box!.x + box!.width).toBeLessThanOrEqual(vw);
}

async function checkStep(page: Page, testInfo: TestInfo, width: number, name: string, titles: string[]) {
  // let the open/transition animation settle
  await page.waitForTimeout(600);
  const dialog = modal(page);

  const overflow = await page.evaluate(() => ({
    page: document.documentElement.scrollWidth - document.documentElement.clientWidth,
    body: (() => {
      const b = document.querySelector('.ant-modal-body') as HTMLElement | null;
      return b ? b.scrollWidth - b.clientWidth : -1;
    })(),
  }));
  expect(overflow.page, `${name}: page horizontal overflow`).toBeLessThanOrEqual(0);
  expect(overflow.body, `${name}: modal body horizontal overflow`).toBeLessThanOrEqual(0);

  const footerButtons = dialog.locator('.ant-modal-footer button');
  for (let i = 0; i < (await footerButtons.count()); i++) {
    await expectInViewport(page, footerButtons.nth(i));
  }

  // title text must lie fully inside the modal body, not merely not overflow it
  const geometry = await page.evaluate((names) => {
    const body = document.querySelector('.ant-modal-body') as HTMLElement;
    const b = body.getBoundingClientRect();
    const titleRects = names.map((name) => {
      const el = [...document.querySelectorAll('.ant-modal .ant-steps-item-title')].find((e) => e.textContent?.trim() === name)!;
      const range = document.createRange();
      range.selectNodeContents(el);
      const r = range.getBoundingClientRect();
      return { name, left: r.left, right: r.right };
    });
    const steps = document.querySelector('.ant-modal nz-steps')!.getBoundingClientRect();
    const first = document.querySelector('.ant-modal nz-spin .min-w-0 > *') as HTMLElement;
    return { body: { left: b.left, right: b.right }, titleRects, stepsBottom: steps.bottom, firstTop: first.getBoundingClientRect().top };
  }, titles);
  for (const t of geometry.titleRects) {
    expect(t.left, `${name}: "${t.name}" left edge`).toBeGreaterThanOrEqual(geometry.body.left);
    expect(t.right, `${name}: "${t.name}" right edge`).toBeLessThanOrEqual(geometry.body.right);
  }
  expect(geometry.firstTop - geometry.stepsBottom, `${name}: gap below steps`).toBeGreaterThanOrEqual(8);

  await testInfo.attach(`${name}-${width}px`, {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png',
  });
}
