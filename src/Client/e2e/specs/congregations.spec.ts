import { expect, test } from '@playwright/test';
import { skipUnlessLoggedIn } from '../auth';
import { saveScreenshotsAfterEach } from '../screenshots';

saveScreenshotsAfterEach();

test.use({ ignoreHTTPSErrors: true });
test.skip(({ isMobile }) => isMobile, 'covers the desktop table; the mobile list is covered by component specs');

type FakeCongregation = {
  id: string;
  name: string;
  email: string;
  canReceiveStudents: boolean;
  isSmallCongregation: boolean;
  createdDate: string;
  lastModifiedDate: string;
  [key: string]: unknown;
};

// The local stack redirects API calls to https and drops the session cookie, so the Congregations API is
// replaced by an in-memory fake; the backend itself is covered by the .NET functional tests.
test('congregation lifecycle: menu, create, filter, edit, delete', async ({ page }) => {
  const { email, password } = skipUnlessLoggedIn(test);

  const now = new Date().toISOString();
  const store: FakeCongregation[] = [
    { id: 'seed-1', name: 'Seed Can Receive', email: 'a@example.com', canReceiveStudents: true, isSmallCongregation: false, createdDate: now, lastModifiedDate: now },
    { id: 'seed-2', name: 'Seed Cannot Receive', email: 'b@example.com', canReceiveStudents: false, isSmallCongregation: true, createdDate: now, lastModifiedDate: now }
  ];
  const deleted: string[] = [];

  await page.route('**/api/Congregations**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const id = path.split('/').pop()!;
    const body = request.postDataJSON?.() ?? null;

    if (path.endsWith('/get-all') && request.method() === 'POST') {
      const filters: { field: string; value: unknown }[] = body.filters ?? [];
      const search = (body.searchTerm ?? '').toLowerCase();
      const data = store.filter(
        (c) =>
          c.name.toLowerCase().includes(search) &&
          filters.every((f) => c[f.field] === f.value)
      );
      return route.fulfill({ json: { data, hasNextPage: false, nextCursor: null, sort: body.sort ?? [] } });
    }
    if (request.method() === 'POST') {
      const created = { ...body, id: `new-${store.length}`, createdDate: now, lastModifiedDate: now };
      store.push(created);
      return route.fulfill({ status: 201, json: created });
    }
    if (request.method() === 'PUT') {
      const index = store.findIndex((c) => c.id === id);
      store[index] = { ...store[index], ...body };
      return route.fulfill({ json: store[index] });
    }
    if (request.method() === 'DELETE') {
      deleted.push(id);
      store.splice(store.findIndex((c) => c.id === id), 1);
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({ json: store.find((c) => c.id === id) });
  });

  await page.goto('/login');
  await page.getByPlaceholder('Email').fill(email);
  await page.getByPlaceholder('Parola').fill(password);
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page).toHaveURL(/school-classes/);
  await page.route('**/api/Users/manage/info', (route) =>
    route.fulfill({ status: 200, json: { email, isEmailConfirmed: true } })
  );

  // menu entry opens the page
  const menuToggle = page.getByRole('button', { name: 'Deschide meniul' });
  if (await menuToggle.isVisible()) await menuToggle.click();
  await page.getByText('Congregații', { exact: true }).first().click();
  await expect(page).toHaveURL(/congregations/);
  await expect(page.getByRole('row', { name: /Seed Can Receive/ })).toBeVisible();
  await expect(page.getByRole('row', { name: /Seed Cannot Receive/ })).toBeVisible();

  // the "Poate primi cursanți: Da" segment keeps only congregations that can receive students
  await page.getByLabel('Poate primi cursanți').getByText('Da', { exact: true }).click();
  await expect(page.getByRole('row', { name: /Seed Cannot Receive/ })).toBeHidden();
  await expect(page.getByRole('row', { name: /Seed Can Receive/ })).toBeVisible();
  await page.getByRole('button', { name: 'Resetează' }).click();
  await expect(page.getByRole('row', { name: /Seed Cannot Receive/ })).toBeVisible();

  // create: validation first, then save
  await page.getByRole('button', { name: 'Adaugă congregație' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByRole('button', { name: 'Salvează și închide' }).click();
  await expect(dialog.getByText('Numele congregației este obligatoriu.')).toBeVisible();
  await expect(dialog.getByText('Emailul este obligatoriu.')).toBeVisible();
  await dialog.getByRole('textbox').nth(0).fill('Brand New');
  await dialog.getByRole('textbox').nth(1).fill('new@example.com');
  await dialog.getByRole('button', { name: 'Salvează și închide' }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByRole('row', { name: /Brand New/ })).toBeVisible();

  // edit
  await page.getByRole('row', { name: /Brand New/ }).click();
  await dialog.getByRole('textbox').nth(0).fill('Brand Renamed');
  await dialog.getByRole('button', { name: 'Salvează și închide' }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByRole('row', { name: /Brand Renamed/ })).toBeVisible();

  // delete needs confirmation
  const renamedRow = page.getByRole('row', { name: /Brand Renamed/ });
  await renamedRow.getByRole('button', { name: 'Șterge congregația' }).click();
  await page.getByRole('button', { name: 'Nu', exact: true }).click();
  await expect(renamedRow).toBeVisible();
  expect(deleted).toHaveLength(0);

  await renamedRow.getByRole('button', { name: 'Șterge congregația' }).click();
  await page.getByRole('button', { name: 'Șterge', exact: true }).click();
  await expect(renamedRow).toBeHidden();
  expect(deleted).toEqual(['new-2']);
});
