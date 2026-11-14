import { expect, test } from '@playwright/test';
import { DEMO_ADMIN, DEMO_USER, expectShellPage, signIn } from './helpers';

// Smoke: both demo accounts can sign in and every main destination renders in the shell.

const USER_PAGES = ['/', '/Documents', '/Projects', '/Teams', '/Settings/AI', '/Account/Manage'];
const ADMIN_PAGES = ['/Admin', '/Admin/Users', '/Admin/Roles', '/Admin/UserLimits', '/Admin/Analytics', '/Admin/Settings'];

test('demo user: main pages render in the shell', async ({ page }) => {
  await signIn(page, DEMO_USER);
  for (const path of USER_PAGES) {
    await expectShellPage(page, path);
  }
  // No admin rail item for a regular user.
  await expect(page.locator('.dfh-rail [aria-label="Admin"]')).toHaveCount(0);
});

test('demo user: admin area shows the 403 page', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Admin');
  await expect(page.locator('.dfh-error-code')).toHaveText(/403/);
});

test('demo admin: admin pages render with the admin nav', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  for (const path of ADMIN_PAGES) {
    await expectShellPage(page, path);
    await expect(page.locator('.dfh-toolwindow--admin'), `${path} admin nav`).toBeVisible();
  }
  await expect(page.locator('.dfh-rail [aria-label="Admin"]')).toHaveCount(1);
});

test('unknown address shows the 404 page', async ({ page }) => {
  const response = await page.goto('/this-page-does-not-exist');
  expect(response?.status()).toBe(404);
  await expect(page.locator('.dfh-error-code')).toHaveText(/404/);
});

// Demo-only checks: the layout's antiforgery token is accepted, and blocked writes explain themselves.
test('demo: AJAX write with the layout token is blocked as read-only (403), not rejected (400)', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  const status = await page.evaluate(async () => {
    const token = (document.querySelector('meta[name="csrf-token"]') as HTMLMetaElement).content;
    const response = await fetch('/Documents/Index?handler=BulkDelete', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
      body: JSON.stringify({ documentIds: [] }),
    });
    return response.status;
  });
  expect(status).toBe(403);
});

test('demo: a blocked form post shows the read-only toast', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  await page.evaluate(() => {
    const token = (document.querySelector('meta[name="csrf-token"]') as HTMLMetaElement).content;
    const form = document.createElement('form');
    form.method = 'post';
    form.action = '/Documents?handler=Delete';
    form.innerHTML = `<input name="__RequestVerificationToken" value="${token}"><input name="documentId" value="1">`;
    document.body.appendChild(form);
    form.submit();
  });
  await expect(page.locator('.dfh-toast')).toContainText('read-only');
});

test('Ctrl K focuses the global search', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  await page.keyboard.press('Control+k');
  await expect(page.locator('#dfh-global-search')).toBeFocused();
});

// Sprint 10: sign in, dashboard, admin overview.
test('signed out: / goes straight to Sign in', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/Account\/Login/);
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
});

test('demo: clicking a demo account fills the sign-in form', async ({ page }) => {
  await page.goto('/Account/Login');
  const fill = page.locator('.dfh-demo-fill').first();
  test.skip((await fill.count()) === 0, 'not a demo instance');
  await fill.click();
  await expect(page.locator('#Input_Email')).toHaveValue(DEMO_USER.email);
  await Promise.all([page.waitForURL((url) => url.pathname === '/'), page.locator('#login-submit').click()]);
  await expect(page.locator('.dfh-page-header h1')).toHaveText('Overview');
});

test('dashboard: stats, recent documents, strip and activity render', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/');
  await expect(page.locator('.dfh-statline')).toContainText('documents');
  await expect(page.locator('.dfh-table tbody tr').first()).toBeVisible();
  await expect(page.locator('.dfh-strip > section')).toHaveCount(3);
  await expect(page.locator('.dfh-inspector', { hasText: 'Activity' })).toBeVisible();

  // A row opens its document.
  await page.locator('.dfh-table tbody tr td.is-mono').first().click();
  await expect(page).toHaveURL(/\/Documents\/Details\/\d+/);

  // "All time" has no "+N" figure.
  await page.goto('/?period=all');
  await expect(page.locator('.dfh-filter-btn')).toContainText('All time');
  await expect(page.locator('.dfh-statline')).not.toContainText('this week');
});

test('demo admin: / is the personal dashboard, /Admin the system overview', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  await page.goto('/');
  await expect(page).toHaveURL(/\/$/);
  await expect(page.locator('.dfh-toolwindow--admin')).toHaveCount(0);
  await page.goto('/Admin');
  await expect(page.locator('.dfh-page-header h1')).toHaveText('Overview');
  await expect(page.locator('.dfh-table tbody tr').first()).toBeVisible();
  await expect(page.locator('.dfh-inspector', { hasText: 'System activity' })).toBeVisible();
});

test('register: password rules update live and gate the submit button', async ({ page }) => {
  await page.goto('/Account/Register');
  test.skip(page.url().includes('/Account/Login'), 'registration is disabled in the demo');
  const submit = page.locator('#registerSubmit');
  await expect(submit).toBeDisabled();
  await page.locator('#Input_Password').fill('abc');
  await expect(page.locator('[data-rule="length"]')).not.toHaveClass(/is-met/);
  await page.locator('#Input_Password').fill('Abcdefg1!');
  await expect(page.locator('.dfh-rules li.is-met')).toHaveCount(4);
  await expect(page.locator('.dfh-strength')).toHaveAttribute('data-score', '4');
  await expect(submit).toBeDisabled(); // confirmation still empty
  await page.locator('#Input_ConfirmPassword').fill('Abcdefg1!');
  await expect(submit).toBeEnabled();
});

// Sprint 11: documents list.
test('documents: links keep the filters, the tree scopes the list', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents?owner=me&sort=name');
  const rows = page.locator('[data-dfh-doc-table] tbody tr');
  expect(await rows.count()).toBeGreaterThan(0);
  expect(await rows.count()).toBeLessThanOrEqual(25);
  await expect(page.locator('.dfh-token')).toContainText('owner:me');
  for (const link of await page.locator('.dfh-pager a, .dfh-tree a.dfh-tree-link').all()) {
    const href = (await link.getAttribute('href')) ?? '';
    expect(href, 'link keeps the owner filter').toContain('owner=me');
    expect(href, 'link keeps the sort').toContain('sort=name');
  }
  const folder = page.locator('.dfh-tree a.dfh-tree-link[href*="folder="]').first();
  await folder.click();
  await expect(page.locator('.dfh-tree-row.is-selected')).toHaveCount(1);
  await expect(page.locator('.dfh-breadcrumb-current')).toHaveText((await page.locator('.dfh-tree-row.is-selected .dfh-tree-name').textContent()) ?? '');
});

test('documents: selecting a row fills the inspector; arrows move the selection', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  const rows = page.locator('[data-dfh-doc-table] tbody tr');
  await expect(page.locator('#dfh-inspector-body .dfh-insp-title')).toHaveText((await rows.nth(0).getAttribute('data-title')) ?? '');
  await rows.nth(2).click();
  await expect(rows.nth(2)).toHaveClass(/is-selected/);
  await expect(page.locator('#dfh-inspector-body .dfh-insp-title')).toHaveText((await rows.nth(2).getAttribute('data-title')) ?? '');
  await page.keyboard.press('ArrowDown');
  await expect(rows.nth(3)).toHaveClass(/is-selected/);
  await expect(page.locator('#dfh-inspector-body .dfh-insp-title')).toHaveText((await rows.nth(3).getAttribute('data-title')) ?? '');
});

test('documents: checking rows opens the bulk bar, Escape clears it', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  const bar = page.locator('#dfh-bulkbar');
  await expect(bar).toBeHidden();
  await page.locator('.dfh-row-check').nth(0).check();
  await page.locator('.dfh-row-check').nth(1).check();
  await expect(bar).toBeVisible();
  await expect(bar.locator('[data-dfh-bulk-count]')).toHaveText('2');
  // Escape in a menu closes the menu only.
  await page.locator('.dfh-toolbar [data-bs-toggle="dropdown"]').first().click();
  await page.keyboard.press('Escape');
  await expect(bar).toBeVisible();
  await page.locator('body').click({ position: { x: 600, y: 760 } });
  await page.keyboard.press('Escape');
  await expect(bar).toBeHidden();
  await page.locator('#dfh-check-all').check();
  await expect(bar.locator('[data-dfh-bulk-count]')).toHaveText(String(await page.locator('.dfh-row-check').count()));
});

test('demo: a blocked AJAX move reports the read-only message', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  test.skip((await page.locator('[data-dfh-bulk="move"]').count()) > 0, 'not a demo instance');
  const result = await page.evaluate(() =>
    (window as any).DocFlowHub.postJson('/Documents?handler=Move', { documentIds: [1], projectId: 1, folderId: null }));
  expect(result.success).toBe(false);
  expect(result.readOnly).toBe(true);
  expect(result.message).toContain('read-only');
});
