import { expect, test, type Page } from '@playwright/test';
import { saveScreenshotsAfterEach } from '../screenshots';

saveScreenshotsAfterEach();

const expense = (id: string, status: string, extra: object = {}) => ({
  id, schoolClassId: 'class-1', source: 'receipt', status, currency: 'RON',
  uploadedAt: '2026-10-14T08:00:00Z', createdDate: '2026-10-14T08:00:00Z', lastModifiedDate: '2026-10-14T08:00:00Z',
  fileMetadataId: 'file-1', ...extra
});

const expenses = [
  expense('e1', 'processing'),
  expense('e2', 'processed', { supplierName: 'ACME SRL', documentNumber: 'F-12', expenseDate: '2026-10-05', totalAmount: 119, vatAmount: 19 }),
  expense('e3', 'failed', { errorMessage: 'Documentul nu poate fi citit' })
];

test('expenses tab lists statuses and opens the upload modal from the header', async ({ page }) => {
  await openExpenses(page);

  await expect(page.getByText('În procesare', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Procesat', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Eroare', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('ACME SRL')).toBeVisible();

  await page.getByRole('button', { name: /Adaugă cheltuială/ }).first().click();
  await expect(page.getByText('Încarcă un bon sau o factură')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Introdu manual' })).toBeVisible();
});

test('expenses list renders as table on desktop and cards on mobile', async ({ page }) => {
  await openExpenses(page);

  await expect(page.getByText('F-12')).toBeVisible();
});

test('a failed expense opens with its error and can be completed by hand', async ({ page }) => {
  await openExpenses(page);
  let saved: unknown;
  await page.route('**/api/Expenses/e3', async route => {
    if (route.request().method() === 'PUT') {
      saved = route.request().postDataJSON();
      await route.fulfill({ json: expense('e3', 'processed', { supplierName: 'Librărie' }) });
    } else {
      await route.fulfill({ json: expenses[2] });
    }
  });

  await page.getByText('Eroare', { exact: true }).first().click();
  await expect(page.getByText('Documentul nu poate fi citit')).toBeVisible();
  await page.getByLabel('Furnizor').fill('Librărie');
  await page.getByRole('button', { name: 'Salvează' }).click();

  await expect.poll(() => saved).toMatchObject({ supplierName: 'Librărie', currency: 'RON' });
});

async function openExpenses(page: Page) {
  await page.route('**/api/**', async route => {
    const { pathname } = new URL(route.request().url());
    if (pathname === '/api/Users/manage/info') {
      await route.fulfill({ json: { email: 'test@example.com', isEmailConfirmed: true } });
    } else if (pathname === '/api/SchoolClasses/class-1') {
      await route.fulfill({ json: { id: 'class-1', name: 'Clasa E2E', startDate: '2026-09-01', endDate: '2027-06-15' } });
    } else if (pathname === '/api/SchoolClasses/class-1/summary') {
      await route.fulfill({ json: { id: 'class-1' } });
    } else if (pathname === '/api/Expenses/get-all') {
      await route.fulfill({ json: { data: expenses, hasNextPage: false, sort: [] } });
    } else {
      await route.fulfill({ json: { data: [], hasNextPage: false, sort: [], items: [] } });
    }
  });
  await page.goto('/school-classes/class-1?tab=8');
  await expect(page.getByText('ACME SRL')).toBeVisible();
}
