import { expect, test } from '@playwright/test';
import { DEMO_USER, signIn } from './helpers';

// Documents list writes (delete, bulk delete, move). They change data, so they run only on a
// non-demo instance seeded with the demo data (start once with DemoMode on, then restart with it off).

test.beforeEach(async ({ page }) => {
  await signIn(page, DEMO_USER);
  await page.goto('/Documents');
  test.skip((await page.locator('[data-dfh-bulk="move"]').count()) === 0, 'writes are disabled in the demo');
});

test('bulk delete reports each document and skips the ones the user does not own', async ({ page }) => {
  const owned = page.locator('tbody tr[data-owner="1"]').first();
  const foreign = page.locator('tbody tr[data-owner="0"]').first();
  const ownedId = (await owned.getAttribute('data-id'))!;
  await owned.locator('.dfh-row-check').check();
  await foreign.locator('.dfh-row-check').check();
  await page.locator('[data-dfh-bulk="delete"]').click();

  const modal = page.locator('#dfh-bulk-delete-modal');
  await expect(modal.locator('.modal-title')).toHaveText('Delete 2 documents?');
  await modal.locator('[data-dfh-bulk-confirm]').click();
  await expect(modal.locator('[data-dfh-bulk-caption]')).toHaveText('1 deleted · 1 skipped · done');
  await expect(modal.locator('.dfh-result.is-danger')).toHaveText('not owner · skipped');
  await modal.locator('[data-dfh-bulk-close]').click();

  await expect(page.locator('.dfh-toast')).toContainText('Deleted 1 document');
  await expect(page.locator(`tbody tr[data-id="${ownedId}"]`)).toHaveCount(0);
});

test('move puts a document into the chosen folder', async ({ page }) => {
  const row = page.locator('tbody tr[data-owner="1"]').first();
  const id = (await row.getAttribute('data-id'))!;
  await row.click();
  await page.locator('#dfh-inspector-body [data-bs-toggle="dropdown"]').click();
  await page.locator('#dfh-inspector-body [data-dfh-action="move"]').click();

  const modal = page.locator('#dfh-move-modal');
  await modal.locator('[data-dfh-move-filter]').fill('draft');
  const target = modal.locator('.dfh-picker-row.is-selected');
  await expect(target).toHaveCount(1);
  const path = (await target.getAttribute('data-path'))!;
  await modal.locator('[data-dfh-move-confirm]').click();

  await expect(page.locator('.dfh-toast')).toContainText(`Moved 1 document to ${path}`);
  await page.locator(`tbody tr[data-id="${id}"]`).click();
  await expect(page.locator('#dfh-inspector-body .dfh-insp-path')).toHaveText(path);
});

test('delete removes one document after confirmation', async ({ page }) => {
  const row = page.locator('tbody tr[data-owner="1"]').first();
  const id = (await row.getAttribute('data-id'))!;
  const title = (await row.getAttribute('data-title'))!;
  await row.click();
  await page.locator('#dfh-inspector-body [data-bs-toggle="dropdown"]').click();
  await page.locator('#dfh-inspector-body [data-dfh-action="delete"]').click();

  const modal = page.locator('#dfh-delete-modal');
  await expect(modal.locator('.modal-title')).toHaveText(`Delete “${title}”?`);
  await modal.locator('button[type="submit"]').click();

  await expect(page.locator('.dfh-toast')).toContainText(`Deleted "${title}"`);
  await expect(page.locator(`tbody tr[data-id="${id}"]`)).toHaveCount(0);
});

// Sprint 12: upload, edit, new version, delete a version, sharing.
test('upload, edit, new version, delete the old version, share and unshare', async ({ page }) => {
  await page.goto('/Documents/Upload');
  await page.locator('[data-dfh-file]').setInputFiles({ name: 'e2e-upload.txt', mimeType: 'text/plain', buffer: Buffer.from('First version\n') });
  const project = page.locator('[data-dfh-project]');
  const projectName = (await project.locator('option').nth(1).textContent())!.trim();
  await project.selectOption({ index: 1 });
  await page.locator('input[name="GenerateSummary"][type="checkbox"]').uncheck();
  await page.locator('[data-dfh-upload-submit]').click();

  await expect(page).toHaveURL(/\/Documents\/Details\/\d+/);
  await expect(page.locator('.dfh-toast')).toContainText('Uploaded "e2e-upload"');
  await expect(page.locator('.dfh-breadcrumb')).toContainText(projectName);

  await page.locator('a', { hasText: 'New version' }).click();
  await page.locator('#Title').fill('E2E upload renamed');
  await page.locator('button', { hasText: 'Save details' }).click();
  await expect(page.locator('.dfh-toast')).toContainText('Details saved');
  await expect(page.locator('#panel-info .dfh-insp-title')).toHaveText('E2E upload renamed');

  await page.locator('a', { hasText: 'New version' }).click();
  await page.locator('#new-version [data-dfh-file]').setInputFiles({ name: 'e2e-upload.txt', mimeType: 'text/plain', buffer: Buffer.from('Second version\n') });
  await page.locator('#ChangeSummary').fill('Second draft');
  await page.locator('[data-dfh-upload-submit]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Uploaded v2');
  await expect(page.locator('#panel-versions .dfh-version-row')).toHaveCount(2);

  await page.locator('[data-dfh-version-delete]').click();
  await page.locator('#dfh-version-delete-modal button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Version deleted');
  await expect(page.locator('#panel-versions .dfh-version-row')).toHaveCount(1);

  await page.locator('[data-dfh-tabs] [data-tab="sharing"]').click();
  const team = page.locator('#panel-sharing select[name="teamId"]');
  if ((await team.locator('option').count()) > 1) {
    await team.selectOption({ index: 1 });
    await page.locator('#panel-sharing button[type="submit"]').click();
    await expect(page.locator('.dfh-toast')).toContainText('Shared with');
    await team.selectOption({ index: 0 });
    await page.locator('#panel-sharing button[type="submit"]').click();
    await expect(page.locator('.dfh-toast')).toContainText('private again');
  }
});

test('generate summary reports the outcome (an error toast when no AI key is configured)', async ({ page }) => {
  await page.goto('/Documents/Upload');
  await page.locator('[data-dfh-file]').setInputFiles({ name: 'e2e-summary.txt', mimeType: 'text/plain', buffer: Buffer.from('Text to summarize\n') });
  await page.locator('input[name="GenerateSummary"][type="checkbox"]').uncheck();
  await page.locator('[data-dfh-upload-submit]').click();
  await expect(page).toHaveURL(/\/Documents\/Details\/\d+/);

  await page.locator('[data-dfh-tabs] [data-tab="ai"]').click();
  await page.locator('#panel-ai button[type="submit"]', { hasText: 'Generate summary' }).click();
  await expect(page).toHaveURL(/tab=ai/);
  await expect(page.locator('.dfh-toast')).toContainText(/AI summary ready|could not|summary/i);
});

// Sprint 15: row menu and row shortcuts.
test('row shortcuts: Del opens delete, M opens move; the row menu deletes', async ({ page }) => {
  const row = page.locator('tbody tr[data-owner="1"]').first();
  const id = (await row.getAttribute('data-id'))!;
  const title = (await row.getAttribute('data-title'))!;
  await row.click();
  await page.keyboard.press('m');
  await expect(page.locator('#dfh-move-modal')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.locator('#dfh-move-modal')).toBeHidden();
  await row.focus();
  await page.keyboard.press('Delete');
  await expect(page.locator('#dfh-delete-modal .modal-title')).toHaveText(`Delete “${title}”?`);
  await page.locator('#dfh-delete-modal [data-bs-dismiss="modal"].btn').click();
  await expect(page.locator('#dfh-delete-modal')).toBeHidden();

  await row.locator('td').nth(3).click({ button: 'right' });
  await page.locator('.dfh-menu .dropdown-item', { hasText: 'Delete…' }).click();
  await page.locator('#dfh-delete-modal button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText(`Deleted "${title}"`);
  await expect(page.locator(`tbody tr[data-id="${id}"]`)).toHaveCount(0);
});
