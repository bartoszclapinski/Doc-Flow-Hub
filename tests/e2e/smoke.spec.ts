import { expect, Page, test } from '@playwright/test';
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

// Sprint 16: signed-out / is the public landing page (Sprint 10 sent it straight to Sign in).
test('signed out: / is the landing page with working calls to action', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Version control for documents your whole team can read.');
  await expect(page.locator('.dfh-titlebar')).toHaveCount(0);
  for (const id of ['versions', 'ai', 'teams', 'stack', 'changelog']) {
    await expect(page.locator(`section#${id} h2`)).toBeVisible();
  }
  // The hero screenshot is a real image that loaded.
  const hero = page.locator('.dfh-lp-shot img');
  await hero.scrollIntoViewIfNeeded();
  await expect.poll(() => hero.evaluate((img: HTMLImageElement) => img.complete && img.naturalWidth)).toBeGreaterThan(0);
  await expect(page.locator('.dfh-lp-footer')).toContainText('MIT');

  const demo = page.locator('.dfh-lp-hero a', { hasText: 'Try the demo' });
  if (await demo.count()) {
    // Demo: the call to action opens Sign in, which offers the demo accounts first.
    await demo.click();
    await expect(page).toHaveURL(/\/Account\/Login$/);
    await expect(page.locator('form.dfh-demo-account')).toHaveCount(2);
  } else {
    await page.locator('.dfh-lp-hero a', { hasText: 'Create account' }).click();
    await expect(page).toHaveURL(/\/Account\/Register/);
  }
});

test('landing: anchors scroll to their sections and phones get no sideways scroll', async ({ page }) => {
  await page.goto('/');
  await page.locator('.dfh-lp-anchors a[href="#stack"]').click();
  await expect(page).toHaveURL(/#stack$/);
  await expect(page.locator('#stack h2')).toBeInViewport();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  await expect(page.locator('.dfh-lp-anchors')).toBeHidden();
  await expect(page.locator('.dfh-lp-nav').getByRole('link', { name: 'Sign in' })).toBeVisible();
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(0);
});

test('signed out: Sign in still works from the landing page', async ({ page }) => {
  await page.goto('/');
  await page.locator('.dfh-lp-nav').getByRole('link', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/Account\/Login$/);
  await expect(page.locator('#login-submit')).toBeVisible();
});

// Sprint 17: the demo accounts are explained and sign in with one click.
test('demo sign-in: accounts are explained; one click signs in as user or admin', async ({ page }) => {
  await page.goto('/Account/Login');
  const cards = page.locator('form.dfh-demo-account');
  test.skip((await cards.count()) === 0, 'not a demo instance');
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Explore the read-only demo');
  await expect(cards.nth(0)).toContainText('Demo user');
  await expect(cards.nth(0)).toContainText(DEMO_USER.email);
  await expect(cards.nth(1)).toContainText('admin panel');
  // The explanation is collapsed until asked for.
  const why = page.locator('details.dfh-demo-why');
  await expect(why.locator('ul')).toBeHidden();
  await why.locator('summary').click();
  await expect(why).toContainText('read-only');

  await Promise.all([page.waitForURL((url) => url.pathname === '/'), page.getByRole('button', { name: 'Sign in as user' }).click()]);
  await expect(page.locator('.dfh-page-header h1')).toHaveText('Overview');
  await expect(page.locator('.dfh-rail a[href="/Admin"]')).toHaveCount(0);

  await page.context().clearCookies();
  await page.goto('/Account/Login');
  await Promise.all([page.waitForURL((url) => url.pathname === '/'), page.getByRole('button', { name: 'Sign in as admin' }).click()]);
  await expect(page.locator('.dfh-rail a[href="/Admin"]')).toBeVisible();

  // The e-mail form still works next to the cards.
  await page.context().clearCookies();
  await signIn(page, DEMO_USER);
  await expect(page.locator('.dfh-page-header h1')).toHaveText('Overview');
});

test('demo: /Account/Register explains why there is no sign-up', async ({ page }) => {
  await page.goto('/Account/Login');
  test.skip((await page.locator('form.dfh-demo-account').count()) === 0, 'not a demo instance');
  await page.goto('/Account/Register');
  await expect(page).toHaveURL(/\/Account\/Register$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('No sign-up in the demo');
  await expect(page.locator('details.dfh-demo-why ul')).toBeVisible();
  await expect(page.locator('#registerSubmit')).toHaveCount(0);
  await page.getByRole('link', { name: 'Explore the demo' }).click();
  await expect(page.locator('form.dfh-demo-account')).toHaveCount(2);
});

test('without demo mode: no demo accounts, and the demo sign-in handler does not exist', async ({ page }) => {
  await page.goto('/Account/Login');
  test.skip((await page.locator('form.dfh-demo-account').count()) > 0, 'demo instance');
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Sign in');
  const token = await page.locator('#account input[name="__RequestVerificationToken"]').inputValue();
  const response = await page.request.post('/Account/Login?handler=Demo', {
    form: { __RequestVerificationToken: token, account: 'user' },
    maxRedirects: 0,
  });
  expect(response.status()).toBe(404);
});

test('demo sign-in on a phone: full-width buttons, no sideways scroll', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/Account/Login');
  test.skip((await page.locator('form.dfh-demo-account').count()) === 0, 'not a demo instance');
  const button = page.getByRole('button', { name: 'Sign in as user' });
  const box = await button.boundingBox();
  expect(box!.width).toBeGreaterThan(250);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(0);
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
  test.skip((await page.locator('#registerSubmit').count()) === 0, 'registration is disabled in the demo');
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

// Sprint 21: the line-by-line diff. Demo versions of one document differ in a few lines.
test('compare: line diff side by side and unified, with change jumps', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await signIn(page, DEMO_USER);
  const id = await documentWithVersions(page);
  await page.goto(`/Documents/Compare/${id}`);

  const diff = page.locator('[data-dfh-diff]');
  await expect(diff).toHaveClass(/is-split/);
  const added = await diff.locator('.dfh-diff-line.is-add').count();
  const removed = await diff.locator('.dfh-diff-line.is-del').count();
  const changes = await diff.locator('[data-dfh-change]').count();
  expect(added).toBeGreaterThan(0);
  expect(removed).toBeGreaterThan(0);
  await expect(page.locator('.dfh-diff-count .is-add')).toHaveText(`+${added}`);
  await expect(page.locator('.dfh-diff-count .is-del')).toHaveText(`−${removed}`);
  await expect(page.locator('[data-dfh-diff-pos]')).toHaveText(`change 1 of ${changes}`);
  await expect(page.locator('.dfh-diff-empty')).toHaveCount(0);

  // Side by side: a removed line and the added line of the same row sit next to each other.
  const pair = diff.locator('[data-dfh-change]', { has: page.locator('.is-del') }).filter({ has: page.locator('.is-add') }).first();
  const del = (await pair.locator('.is-del').first().boundingBox())!;
  const add = (await pair.locator('.is-add').first().boundingBox())!;
  expect(add.x).toBeGreaterThan(del.x + del.width - 2);
  expect(Math.abs(add.y - del.y)).toBeLessThan(2);

  await page.locator('[data-dfh-diff-next]').click();
  const second = changes > 1 ? 2 : 1;
  await expect(page.locator('[data-dfh-diff-pos]')).toHaveText(`change ${second} of ${changes}`);
  await expect(diff.locator(`#change-${second}`)).toHaveClass(/is-current/);
  await page.keyboard.press('p');
  await expect(page.locator('[data-dfh-diff-pos]')).toHaveText(`change 1 of ${changes}`);

  // Unified: one column, removed lines above the added ones.
  await page.locator('.dfh-diff-layout a', { hasText: 'Unified' }).click();
  await expect(page).toHaveURL(/view=unified/);
  await expect(diff).toHaveClass(/is-unified/);
  const uDel = (await pair.locator('.is-del').first().boundingBox())!;
  const uAdd = (await pair.locator('.is-add').first().boundingBox())!;
  expect(Math.abs(uAdd.x - uDel.x)).toBeLessThan(2);
  expect(uAdd.y).toBeGreaterThan(uDel.y);

  // The switches are links that keep each other and the version pair.
  await page.locator('[data-dfh-diff-ws]').click();
  await expect(page).toHaveURL(/ws=ignore/);
  await expect(page).toHaveURL(/view=unified/);
  await expect(page).toHaveURL(/from=\d+&to=\d+/);
  await expect(page.locator('[data-dfh-diff-ws]')).toHaveAttribute('aria-pressed', 'true');
  expect(errors).toEqual([]);
});

test('compare on a phone: the diff is unified and nothing scrolls sideways', async ({ page }) => {
  await signIn(page, DEMO_USER);
  const id = await documentWithVersions(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/Documents/Compare/${id}`);

  const diff = page.locator('[data-dfh-diff]');
  await expect(diff.locator('.dfh-diff-line.is-add').first()).toBeVisible();
  await expect(page.locator('.dfh-diff-layout')).toBeHidden();
  const pair = diff.locator('[data-dfh-change]', { has: page.locator('.is-del') }).filter({ has: page.locator('.is-add') }).first();
  const del = (await pair.locator('.is-del').first().boundingBox())!;
  const add = (await pair.locator('.is-add').first().boundingBox())!;
  expect(Math.abs(add.x - del.x)).toBeLessThan(2);
  expect(add.y).toBeGreaterThan(del.y);
  expect(del.x).toBeGreaterThanOrEqual(0);
  expect(del.x + del.width).toBeLessThanOrEqual(390);
  expect(await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)).toBeLessThanOrEqual(0);
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

// Sprint 18: document preview (read-only, works in the demo).
async function openDocument(page: Page, row: string) {
  await page.goto('/Documents');
  const href = await page.locator(`${row} a[href*="/Documents/Details/"]`).first().getAttribute('href');
  expect(href, `a document row matching ${row}`).toBeTruthy();
  await page.goto(href!);
}

test('preview: Markdown is rendered; Source shows numbered lines', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await openDocument(page, 'tbody tr[data-ext="MD"]');
  await expect(page.locator('.dfh-md h1')).toBeVisible();
  await page.locator('.dfh-segmented a', { hasText: 'Source' }).click();
  await expect(page).toHaveURL(/view=source/);
  await expect(page.locator('.dfh-code-line').first()).toHaveText(/^# /);
  await page.locator('.dfh-segmented a', { hasText: 'Rendered' }).click();
  await expect(page.locator('.dfh-md h1')).toBeVisible();
});

test('preview: text files show numbered lines, the encoding and a wrap switch', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await openDocument(page, 'tbody tr[data-ext="TXT"]');
  await expect(page.locator('.dfh-preview-bar')).toContainText('UTF-8');
  await expect(page.locator('.dfh-code-line').first()).not.toBeEmpty();
  await page.locator('.dfh-preview-bar a', { hasText: 'Wrap lines' }).click();
  await expect(page).toHaveURL(/wrap=0/);
  await expect(page.locator('pre.dfh-code')).toHaveClass(/is-nowrap/);
});

// Sprint 20: a Word document as formatted HTML. Demo DOCX files have headings, a list and a table.
test('preview: a Word document is formatted; Text shows its plain text', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await signIn(page, DEMO_USER);
  await openDocument(page, 'tbody tr[data-ext="DOCX"]');

  const docx = page.locator('.dfh-docx');
  await expect(page.locator('.dfh-preview-badge')).toHaveText('Simplified layout');
  await expect(docx.locator('h1.dfh-docx-title')).toBeVisible();
  await expect(docx.locator('h1', { hasText: 'Action points' })).toBeVisible();
  await expect(docx.locator('ul > li')).toHaveCount(3);
  await expect(docx.locator('strong', { hasText: 'Author:' })).toBeVisible();
  await expect(docx.locator('.dfh-docx-table th')).toHaveText(['Version', 'Date', 'Author', 'Summary']);
  await expect(docx.locator('.dfh-docx-table tr').nth(1).locator('td').first()).toHaveText('v1');
  await expect(docx.locator('em')).toContainText('Sample content');

  await page.locator('.dfh-segmented a', { hasText: 'Text' }).click();
  await expect(page).toHaveURL(/view=text/);
  await expect(page.locator('.dfh-docx')).toHaveCount(0);
  await expect(page.locator('.dfh-preview-badge')).toHaveCount(0);
  await expect(page.locator('pre.dfh-code')).toContainText('Version history');
  await page.locator('.dfh-preview-bar a', { hasText: 'Wrap lines' }).click();
  await expect(page).toHaveURL(/view=text/);
  await expect(page.locator('pre.dfh-code')).toHaveClass(/is-nowrap/);
  await page.locator('.dfh-segmented a', { hasText: 'Document' }).click();
  await expect(page.locator('.dfh-docx h1.dfh-docx-title')).toBeVisible();
  expect(errors).toEqual([]);
});

// Sprint 19: PDF pages through the self-hosted PDF.js. Demo PDFs have three pages.
const PDFJS_LOAD = 60_000;
test('preview: PDF pages render with thumbnails, page navigation and zoom', async ({ page }) => {
  test.slow(); // loads PDF.js
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(e.message));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await signIn(page, DEMO_USER);
  await openDocument(page, 'tbody tr[data-ext="PDF"]');

  const pdf = page.locator('[data-dfh-pdf]');
  // The first PDF downloads PDF.js (about 1.7 MB); on a slow link to the live demo that takes a while.
  await expect(pdf).toHaveAttribute('data-state', 'ready', { timeout: PDFJS_LOAD });
  await expect(pdf).toHaveAttribute('data-pages', '3');
  const first = page.locator('.dfh-pdf-page[data-page="1"]');
  await expect(first).toHaveAttribute('data-rendered', 'true', { timeout: PDFJS_LOAD });
  // Something was drawn: dark pixels (text) on the white page.
  const inked = await first.locator('canvas').evaluate((c: HTMLCanvasElement) => {
    const data = c.getContext('2d')!.getImageData(0, 0, c.width, c.height).data;
    let dark = 0;
    for (let i = 0; i < data.length; i += 4) if (data[i] < 128) dark++;
    return dark;
  });
  expect(inked).toBeGreaterThan(200);
  // The text layer makes the text selectable.
  await expect(first.locator('.textLayer')).toContainText('Page 1 of 3');
  // The file comes from the inline handler with headers that stop sniffing and scripts.
  const file = await page.request.get((await pdf.getAttribute('data-src'))!);
  expect(file.headers()['content-type']).toBe('application/pdf');
  expect(file.headers()['x-content-type-options']).toBe('nosniff');
  expect(file.headers()['content-security-policy']).toContain('sandbox');

  const current = page.locator('[data-dfh-pdf-page]');
  await expect(page.locator('[data-dfh-pdf-count]')).toHaveText('3');
  await expect(current).toHaveText('1');
  await expect(page.locator('[data-dfh-pdf-prev]')).toBeDisabled();

  // Thumbnails: three, drawn lazily; a click goes to that page.
  const thumbs = page.locator('.dfh-pdf-thumb');
  await expect(thumbs).toHaveCount(3);
  await expect(thumbs.first()).toHaveAttribute('data-rendered', 'true');
  await thumbs.nth(2).click();
  await expect(current).toHaveText('3');
  await expect(thumbs.nth(2)).toHaveAttribute('aria-current', 'page');
  await expect(page.locator('.dfh-pdf-page[data-page="3"]')).toHaveAttribute('data-rendered', 'true');
  await expect(page.locator('.dfh-pdf-page[data-page="3"] .textLayer')).toContainText('version history');
  await expect(page.locator('[data-dfh-pdf-next]')).toBeDisabled();
  await page.locator('[data-dfh-pdf-prev]').click();
  await expect(current).toHaveText('2');

  // Zoom in, then back to fit width.
  const zoom = page.locator('[data-dfh-pdf-zoom]');
  const fitLabel = (await zoom.textContent())!;
  const width = () => first.evaluate((e) => e.getBoundingClientRect().width);
  const fitWidth = await width();
  await page.locator('[data-dfh-pdf-zoom-in]').click();
  await expect(zoom).not.toHaveText(fitLabel);
  await expect(page.locator('[data-dfh-pdf-fit]')).toHaveAttribute('aria-pressed', 'false');
  expect(await width()).toBeGreaterThan(fitWidth);
  await page.locator('[data-dfh-pdf-fit]').click();
  await expect(zoom).toHaveText(fitLabel);
  await expect(page.locator('[data-dfh-pdf-fit]')).toHaveAttribute('aria-pressed', 'true');

  // The text view.
  await page.locator('.dfh-segmented a', { hasText: 'Text' }).click();
  await expect(page).toHaveURL(/view=text/);
  await expect(page.locator('.dfh-code-line').first()).not.toBeEmpty();
  await expect(page.locator('[data-dfh-pdf]')).toHaveCount(0);
  await page.locator('.dfh-segmented a', { hasText: 'Pages' }).click();
  await expect(page.locator('[data-dfh-pdf]')).toHaveAttribute('data-state', 'ready', { timeout: PDFJS_LOAD });

  expect(errors).toEqual([]);
});

test('PDF.js files are served with their content types', async ({ request }) => {
  const files: [string, RegExp][] = [
    ['/lib/pdfjs/build/pdf.min.mjs', /javascript/],
    ['/lib/pdfjs/build/pdf.worker.min.mjs', /javascript/],
    ['/lib/pdfjs/standard_fonts/FoxitSerif.pfb', /octet-stream/],
    ['/lib/pdfjs/standard_fonts/LiberationSans-Regular.ttf', /font|ttf|octet/],
    ['/lib/pdfjs/cmaps/Adobe-Japan1-UCS2.bcmap', /octet-stream/],
    ['/lib/pdfjs/iccs/CGATS001Compat-v2-micro.icc', /iccprofile/],
    ['/lib/pdfjs/wasm/openjpeg.wasm', /wasm/],
  ];
  for (const [path, type] of files) {
    const response = await request.get(path);
    expect(response.status(), path).toBe(200);
    expect(response.headers()['content-type'], path).toMatch(type);
  }
});

test('preview: an older version opens with a "viewing" banner', async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  // The row shows the newest version number, not how many are left (a write test may delete old ones),
  // so try the candidates until one still has an older version.
  const hrefs = await page.locator('tbody tr[data-version]:not([data-version="0"]):not([data-version="1"]) a[href*="/Documents/Details/"]')
    .evaluateAll((links) => links.map((a) => a.getAttribute('href')!));
  const eye = page.locator('#panel-versions .dfh-version-row:not(.is-current) a[aria-label^="Preview v"]').first();
  let found = false;
  for (const href of hrefs.slice(0, 8)) {
    await page.goto(href + '?tab=versions');
    if (await eye.count()) { found = true; break; }
  }
  expect(found, 'a document with an older version').toBe(true);
  const number = (await eye.getAttribute('aria-label'))!.replace('Preview v', '');
  await eye.click();
  await expect(page.locator('.dfh-preview-version')).toContainText(`Viewing v${number} of`);
  await expect(page.locator('.dfh-preview-bar')).toContainText(`v${number}`);
  await page.locator('.dfh-preview-version a', { hasText: 'Back to current' }).click();
  await expect(page.locator('.dfh-preview-version')).toHaveCount(0);
});

test('preview on a phone: content flows with the page, no sideways scroll', async ({ page }) => {
  test.slow(); // loads PDF.js
  await signIn(page, DEMO_USER);
  await page.setViewportSize({ width: 390, height: 844 });
  const overflow = () => page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  await openDocument(page, 'tbody tr[data-ext="MD"]');
  await expect(page.locator('.dfh-md')).toBeVisible();
  expect(await overflow()).toBeLessThanOrEqual(0);

  // PDF: no thumbnail strip, the page fits the width.
  await openDocument(page, 'tbody tr[data-ext="PDF"]');
  await expect(page.locator('.dfh-pdf-page[data-page="1"]')).toHaveAttribute('data-rendered', 'true', { timeout: PDFJS_LOAD });
  await expect(page.locator('.dfh-pdf-thumbs')).toBeHidden();
  const box = await page.locator('.dfh-pdf-page[data-page="1"]').boundingBox();
  expect(box!.x).toBeGreaterThanOrEqual(0);
  expect(box!.x + box!.width).toBeLessThanOrEqual(390);
  expect(await overflow()).toBeLessThanOrEqual(0);

  // Word: the sheet and its table stay inside the screen.
  await openDocument(page, 'tbody tr[data-ext="DOCX"]');
  await expect(page.locator('.dfh-docx-table')).toBeVisible();
  for (const part of ['.dfh-docx', '.dfh-docx-table']) {
    const b = await page.locator(part).first().boundingBox();
    expect(b!.x).toBeGreaterThanOrEqual(0);
    expect(b!.x + b!.width).toBeLessThanOrEqual(390);
  }
  expect(await overflow()).toBeLessThanOrEqual(0);
});
