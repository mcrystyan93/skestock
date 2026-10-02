import { expect, test, type Page } from '@playwright/test';

const classId = 'class-1';
const orderListId = 'order-list-1';
const orderListName = 'Comandă de test';

test('cancelling approval leaves draft edits unsaved in the open modal', async ({ page }) => {
  const { statusRequests } = await openOrderList(page);
  const nameInput = page.getByPlaceholder('Numele comenzii');

  await nameInput.fill('Comandă revizuită');
  await page.getByRole('button', { name: 'Aprobă' }).click();

  const confirmation = page.getByRole('dialog').last();
  await expect(confirmation).toContainText('Aprobați comanda?');
  await expect(confirmation).toContainText('următorul pas de aprovizionare');
  await confirmation.getByRole('button', { name: 'Renunță' }).click();

  await expect(nameInput).toHaveValue('Comandă revizuită');
  expect(statusRequests).toEqual([]);

  await page.getByRole('button', { name: 'Aprobă' }).click();
  await page
    .getByRole('dialog')
    .last()
    .getByRole('button', { name: 'Confirmă trimiterea' })
    .click();

  await expect.poll(() => statusRequests).toEqual(['save', 'submit']);
  await expect(page.getByText('Finalizată', { exact: true })).toBeVisible();
  await expect(nameInput).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Salvează și închide' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Aprobă' })).toHaveCount(0);
});

test('a save failure prevents submission and leaves the draft editable', async ({ page }) => {
  const { statusRequests } = await openOrderList(page, { saveFails: true });
  const nameInput = page.getByPlaceholder('Numele comenzii');

  await nameInput.fill('Comandă revizuită');
  await page.getByRole('button', { name: 'Aprobă' }).click();
  await page
    .getByRole('dialog')
    .last()
    .getByRole('button', { name: 'Confirmă trimiterea' })
    .click();

  await expect(page.getByText('Salvarea a eșuat').first()).toBeVisible();
  expect(statusRequests).toEqual(['save']);
  await expect(page.getByRole('dialog').first().getByText('Ciornă', { exact: true })).toBeVisible();
  await expect(nameInput).toBeEnabled();
  await expect(page.getByRole('button', { name: 'Salvează și închide' })).toBeVisible();
});

test('a submission failure leaves the saved draft editable and can be retried', async ({
  page,
}) => {
  const { statusRequests, setSubmissionFailure } = await openOrderList(page, { submitFails: true });
  const nameInput = page.getByPlaceholder('Numele comenzii');

  await nameInput.fill('Comandă revizuită');
  await page.getByRole('button', { name: 'Aprobă' }).click();
  await page
    .getByRole('dialog')
    .last()
    .getByRole('button', { name: 'Confirmă trimiterea' })
    .click();

  await expect(page.getByText('Trimiterea a eșuat').first()).toBeVisible();
  expect(statusRequests).toEqual(['save', 'submit']);
  await expect(page.getByRole('dialog').first().getByText('Ciornă', { exact: true })).toBeVisible();
  await expect(nameInput).toBeEnabled();
  await expect(nameInput).toHaveValue('Comandă revizuită');

  setSubmissionFailure(false);
  await page.getByRole('button', { name: 'Aprobă' }).click();
  await page
    .getByRole('dialog')
    .last()
    .getByRole('button', { name: 'Confirmă trimiterea' })
    .click();

  await expect.poll(() => statusRequests).toEqual(['save', 'submit', 'submit']);
  await expect(page.getByText('Finalizată', { exact: true })).toBeVisible();
  await expect(nameInput).toBeDisabled();
});

test('an unchanged draft submits directly and approval stays disabled while pending', async ({
  page,
}) => {
  const { statusRequests } = await openOrderList(page, { submitDelayMs: 500 });

  await page.getByRole('button', { name: 'Aprobă' }).click();
  await page
    .getByRole('dialog')
    .last()
    .getByRole('button', { name: 'Confirmă trimiterea' })
    .click();

  await expect(page.getByRole('button', { name: 'Aprobă' })).toBeDisabled();
  expect(statusRequests).toEqual(['submit']);
  await expect(page.getByText('Finalizată', { exact: true })).toBeVisible();
  expect(statusRequests).toEqual(['submit']);
});

test('approval is hidden for cancelled order lists', async ({ page }) => {
  const { statusRequests } = await openOrderList(page, { status: 'Cancelled' });

  await expect(page.getByRole('button', { name: 'Aprobă' })).toHaveCount(0);
  expect(statusRequests).toEqual([]);
});

test('approval is disabled with an explanation for a draft without products', async ({ page }) => {
  const { statusRequests } = await openOrderList(page, { lines: [] });

  await expect(page.getByRole('button', { name: 'Aprobă' })).toBeDisabled();
  await expect(
    page.getByText('Adăugați cel puțin un articol pentru a putea aproba comanda.'),
  ).toBeVisible();
  expect(statusRequests).toEqual([]);
});

type OpenOrderListOptions = {
  lines?: ReturnType<typeof createLine>[];
  saveFails?: boolean;
  submitFails?: boolean;
  submitDelayMs?: number;
  status?: 'Draft' | 'Submitted' | 'Cancelled';
};

async function openOrderList(page: Page, options: OpenOrderListOptions = {}) {
  const statusRequests: string[] = [];
  let orderList = createOrderList(options.lines ?? [createLine()], options.status ?? 'Draft');
  let shouldFailSubmission = options.submitFails ?? false;

  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const { pathname } = new URL(request.url());

    if (pathname === '/api/Users/manage/info') {
      await route.fulfill({ json: { email: 'test@example.com', isEmailConfirmed: true } });
      return;
    }

    if (pathname === `/api/SchoolClasses/${classId}`) {
      await route.fulfill({
        json: {
          id: classId,
          name: 'Clasa E2E',
          startDate: '2026-09-01T00:00:00Z',
          endDate: '2027-06-15T00:00:00Z',
        },
      });
      return;
    }

    if (pathname === `/api/SchoolClasses/${classId}/summary`) {
      await route.fulfill({ json: { id: classId } });
      return;
    }

    if (pathname === '/api/OrderLists/get-all') {
      await route.fulfill({
        json: {
          data: [
            {
              id: orderList.id,
              classId,
              name: orderList.name,
              status: orderList.status,
              lineCount: orderList.lines.length,
              submittedAt: orderList.submittedAt,
              createdByName: 'Test user',
              createdDate: orderList.createdDate,
              lastModifiedDate: orderList.lastModifiedDate,
            },
          ],
          hasNextPage: false,
          sort: [],
        },
      });
      return;
    }

    if (pathname === `/api/OrderLists/${orderListId}` && request.method() === 'GET') {
      await route.fulfill({ json: orderList });
      return;
    }

    if (pathname === `/api/OrderLists/${orderListId}` && request.method() === 'PUT') {
      statusRequests.push('save');
      if (options.saveFails) {
        await route.fulfill({
          status: 500,
          contentType: 'application/problem+json',
          body: JSON.stringify({ status: 500, title: 'Salvarea a eșuat' }),
        });
        return;
      }

      const requestBody = request.postDataJSON() as {
        name: string;
        note: string | null;
        lines: Array<{
          itemId: string | null;
          productName: string | null;
          quantity: number;
          unit: string | null;
          notes: string | null;
        }>;
      };
      orderList = {
        ...orderList,
        ...requestBody,
        lines: requestBody.lines.map((line, index) => ({
          ...line,
          id: `line-${index + 1}`,
          orderListId,
        })),
      };
      await route.fulfill({ json: orderList });
      return;
    }

    if (pathname === `/api/OrderLists/${orderListId}/submit`) {
      statusRequests.push('submit');
      if (options.submitDelayMs) {
        await new Promise<void>((resolve) => setTimeout(resolve, options.submitDelayMs));
      }

      if (shouldFailSubmission) {
        await route.fulfill({
          status: 500,
          contentType: 'application/problem+json',
          body: JSON.stringify({ status: 500, title: 'Trimiterea a eșuat' }),
        });
        return;
      }

      orderList = {
        ...orderList,
        status: 'Submitted',
        submittedAt: '2026-10-02T10:00:00Z',
      };
      await route.fulfill({ json: orderList });
      return;
    }

    await route.fulfill({
      json: { data: [], hasNextPage: false, sort: [], id: classId, name: 'Clasa E2E' },
    });
  });

  await page.goto(`/school-classes/${classId}?tab=1`);
  await page.getByText(orderListName, { exact: true }).click();
  await expect(page.getByPlaceholder('Numele comenzii')).toHaveValue(orderListName);

  return {
    statusRequests,
    setSubmissionFailure: (shouldFail: boolean) => (shouldFailSubmission = shouldFail),
  };
}

function createOrderList(
  lines: ReturnType<typeof createLine>[],
  status: 'Draft' | 'Submitted' | 'Cancelled',
) {
  return {
    id: orderListId,
    classId,
    className: 'Clasa E2E',
    name: orderListName,
    note: null,
    status,
    submittedAt: null,
    lines,
    createdByName: 'Test user',
    lastModifiedByName: 'Test user',
    createdDate: '2026-09-01T10:00:00Z',
    lastModifiedDate: '2026-09-01T10:00:00Z',
  };
}

function createLine() {
  return {
    id: 'line-1',
    orderListId,
    itemId: null,
    productName: 'Creion',
    quantity: 2,
    unit: 'buc',
    notes: null,
  };
}
