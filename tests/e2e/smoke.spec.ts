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

test('Ctrl K opens the command palette with the search box focused', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  await page.keyboard.press('Control+k');
  await expect(page.locator('#dfh-palette-q')).toBeFocused();
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

// Sprint 12: details, compare, upload.
async function documentWithVersions(page: import('@playwright/test').Page): Promise<string> {
  await page.goto('/Documents?sort=name');
  const row = page.locator('[data-dfh-doc-table] tbody tr', { has: page.locator('td', { hasText: /^v[2-9]$/ }) }).first();
  return (await row.getAttribute('data-id'))!;
}

test('details: inspector tabs switch and survive a reload', async ({ page }) => {
  await signIn(page, DEMO_USER);
  const id = await documentWithVersions(page);
  await page.goto(`/Documents/Details/${id}`);
  await expect(page.locator('#panel-info')).toBeVisible();
  await page.locator('[data-dfh-tabs] [data-tab="versions"]').click();
  await expect(page.locator('#panel-versions')).toBeVisible();
  await expect(page.locator('#panel-info')).toBeHidden();
  await expect(page).toHaveURL(/tab=versions/);
  await page.reload();
  await expect(page.locator('#panel-versions')).toBeVisible();
  expect(await page.locator('.dfh-version-row').count()).toBeGreaterThanOrEqual(2);
  await expect(page.locator('.dfh-statusbar-path')).toContainText(' / ');
});

test('compare: the seeded AI comparison shows, the pickers change the pair', async ({ page }) => {
  await signIn(page, DEMO_USER);
  const id = await documentWithVersions(page);
  await page.goto(`/Documents/Compare/${id}`);
  await expect(page.locator('.dfh-compare-summary p')).not.toBeEmpty();
  expect(await page.locator('.dfh-change-list li').count()).toBeGreaterThan(0);
  const to = page.locator('#pick-to');
  const newest = await to.inputValue();
  await page.locator('#pick-from').selectOption({ index: 0 }); // the newest version: the page swaps it into "to"
  await expect(page).toHaveURL(new RegExp(`from=${newest}`));
  await expect(page.locator('.dfh-editor-tab.is-active')).toContainText('↔');
});

test('editor tabs: opened documents stay as tabs until closed', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents?sort=name');
  const ids = await page.locator('[data-dfh-doc-table] tbody tr').evaluateAll((rows) => rows.slice(0, 2).map((r) => r.getAttribute('data-id')));
  await page.goto(`/Documents/Details/${ids[0]}`);
  await page.goto(`/Documents/Details/${ids[1]}`);
  const tabs = page.locator('.dfh-editor-tabs .dfh-editor-tab');
  await expect(tabs).toHaveCount(2);
  await tabs.first().locator('[data-dfh-tab-close]').click();
  await expect(tabs).toHaveCount(1);
  await page.reload();
  await expect(tabs).toHaveCount(1);
});

test('upload: the form checks the file type before uploading', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents/Upload');
  await page.locator('[data-dfh-file]').setInputFiles({ name: 'tool.exe', mimeType: 'application/octet-stream', buffer: Buffer.from('MZ') });
  await expect(page.locator('[data-dfh-file-status]')).toHaveText('.exe not allowed');
  await page.locator('[data-dfh-file]').setInputFiles({ name: 'notes.txt', mimeType: 'text/plain', buffer: Buffer.from('hello\n') });
  await expect(page.locator('[data-dfh-file-status]')).toHaveText('ready');
  await expect(page.locator('#Title')).toHaveValue('notes');
});

// Sprint 13: projects, folders, teams.
test('projects: table with inspector, archived tab, filter', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Projects');
  const rows = page.locator('#projects-table tbody tr[data-id]');
  expect(await rows.count()).toBeGreaterThan(0);
  const name = (await rows.first().locator('.dfh-two-line-text a').textContent())!.trim();
  await expect(page.locator('#dfh-inspector-body .dfh-insp-title')).toHaveText(name);
  await page.locator('[data-dfh-row-filter]').fill('zzz-no-such-project');
  await expect(page.locator('.dfh-filter-empty')).toBeVisible();
  await page.locator('.dfh-segmented a', { hasText: 'Archived' }).click();
  await expect(page).toHaveURL(/status=archived/);
  await expect(page.locator('.dfh-segmented a.is-active')).toHaveText('Archived');
});

test('project and folder pages: tree, subfolders, settings', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Projects');
  const id = await page.locator('#projects-table tbody tr[data-id]').first().getAttribute('data-id');
  await page.goto(`/Projects/Details/${id}`);
  await expect(page.locator('.dfh-tree-row.is-selected')).toHaveCount(1);
  const folder = page.locator('.dfh-table tbody tr[data-dfh-href*="/Folders/Details"]').first();
  const folderName = (await folder.locator('a').textContent())!.trim();
  await folder.locator('a').click();
  await expect(page.locator('.dfh-breadcrumb-current')).toHaveText(folderName);
  await expect(page.locator('#Name')).toHaveValue(folderName);
  await expect(page.locator('.dfh-statusbar-path')).toContainText(folderName);
});

test('team page: members, real permission matrix, documents link', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Teams');
  await page.locator('#teams-table tbody tr[data-dfh-href]').first().click();
  await expect(page).toHaveURL(/\/Teams\/Details\/\d+/);
  await expect(page.locator('.dfh-perm-matrix tbody tr')).toHaveCount(6);
  await expect(page.locator('.dfh-perm-matrix thead')).not.toContainText('Viewer');
  await expect(page.locator('.dfh-table--members')).toContainText('you');
  await expect(page.locator('.dfh-viewtab', { hasText: 'Documents' })).toHaveAttribute('href', /\/Documents\?team=\d+/);
});

// Sprint 14: admin, settings, profile.
test('documents: the pager really opens page 2 (Razor Pages "page" route value)', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  const next = page.locator('.dfh-pager a[rel="next"]');
  test.skip((await next.count()) === 0, 'only one page of documents');
  await next.click();
  await expect(page.locator('.dfh-pager .dfh-mono').first()).toHaveText(/^26–/);
});

test('admin users: table, filters as tokens, user page', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  await page.goto('/Admin/Users');
  await expect(page.locator('.dfh-nav-item.is-active .dfh-nav-count')).toHaveText(/\d+/);
  await expect(page.locator('tbody tr[data-id]')).not.toHaveCount(0);
  await page.locator('.dfh-filter-btn', { hasText: 'Role' }).click();
  await page.locator('.dropdown-menu.show .dropdown-item', { hasText: 'Administrator' }).click();
  await expect(page.locator('.dfh-token')).toContainText('role:Administrator');
  await expect(page.locator('tbody tr[data-id]')).toHaveCount(1);
  await page.locator('tbody tr[data-id] a.dfh-cell-link').first().click();
  await expect(page.locator('.dfh-entity-header h1')).toContainText('(you)');
  await expect(page.locator('.dfh-detail-panel')).toHaveCount(3);
});

test('admin analytics, limits and settings: CSS charts and honest notes', async ({ page }) => {
  await signIn(page, DEMO_ADMIN);
  await page.goto('/Admin/Analytics?period=7');
  await expect(page.locator('.dfh-barchart .dfh-bar')).toHaveCount(7);
  await expect(page.locator('.dfh-segmented a.is-active')).toHaveText('7d');
  await page.goto('/Admin/UserLimits');
  await expect(page.locator('.dfh-banner--info')).toContainText('fixed limits');
  await expect(page.locator('button', { hasText: 'Reset usage' })).toHaveCount(0);
  await page.goto('/Admin/Settings');
  await expect(page.locator('.dfh-banner--warning')).toContainText('Saved, not applied yet');
});

test('settings: profile, password and AI pages share the settings tree', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Account/Manage/EditProfile');
  await expect(page).toHaveURL(/\/Account\/Manage$/);
  await expect(page.locator('.dfh-settings-tree .is-selected')).toHaveText('Profile');
  await expect(page.locator('input[aria-label="E-mail"]')).toHaveValue('demo@docflowhub.com');
  await page.locator('.dfh-settings-tree a', { hasText: 'Models & features' }).click();
  await expect(page.locator('.dfh-not-applied').first()).toBeVisible();
  await page.locator('[data-dfh-settings-filter]').fill('pass');
  await expect(page.locator('.dfh-settings-tree a', { hasText: 'Password' })).toBeVisible();
  await expect(page.locator('.dfh-settings-tree a', { hasText: 'Profile' })).toBeHidden();
});

// Sprint 15: command palette, shortcuts, context menu, responsive.
test('palette: Ctrl K finds documents and folders, Enter opens the active row', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  await page.keyboard.press('Control+k');
  const palette = page.locator('#dfh-palette');
  await expect(palette).toBeVisible();
  await page.locator('#dfh-palette-q').fill('annual rep');
  await expect(palette.locator('.dfh-palette-row').first()).toContainText('Annual Report');
  await expect(palette.locator('.dfh-palette-row b').first()).toHaveText(/annual/i);
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/Documents\/Details\/\d+/);
});

test('palette: the title-bar search opens it; actions and Esc work', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/');
  await page.locator('#dfh-global-search').click();
  const palette = page.locator('#dfh-palette');
  await expect(palette).toBeVisible();
  await page.locator('#dfh-palette-q').fill('teams');
  await expect(palette.locator('.dfh-palette-row', { hasText: 'Go to Teams' })).toBeVisible();
  await expect(palette.locator('.dfh-palette-row', { hasText: 'Upload a document' })).toHaveCount(0); // demo: no write actions
  await page.keyboard.press('Escape');
  await expect(palette).toBeHidden();
  await page.keyboard.press('?');
  await expect(page.locator('#dfh-shortcuts')).toBeVisible();
});

test('documents: right click and ⋯ open the row menu; demo disables changes', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  const row = page.locator('tbody tr[data-id]').nth(1);
  await row.locator('td').nth(3).click({ button: 'right' });
  const menu = page.locator('.dfh-menu');
  await expect(menu).toBeVisible();
  await expect(row).toHaveClass(/is-selected/);
  await expect(menu.locator('.dropdown-item', { hasText: 'Open' })).toBeFocused();
  await expect(menu.locator('.dropdown-item', { hasText: 'Move to…' })).toHaveAttribute('aria-disabled', 'true');
  await page.keyboard.press('Escape');
  await expect(menu).toHaveCount(0);
  await row.hover();
  await row.locator('[data-dfh-row-menu]').click();
  await expect(menu).toBeVisible();
  await menu.locator('.dropdown-item', { hasText: 'Open' }).click();
  await expect(page).toHaveURL(/\/Documents\/Details\/\d+/);
});

test('privacy page describes what the app stores, signed in or not', async ({ page }) => {
  await page.goto('/Privacy');
  await expect(page.locator('h1')).toHaveText('Privacy');
  await expect(page.locator('.dfh-doc-page')).toContainText('salted hash');
  await expect(page.locator('body')).not.toContainText('Use this page to detail');
});
