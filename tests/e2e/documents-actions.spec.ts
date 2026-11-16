import path from 'path';
import { expect, test, type Page } from '@playwright/test';
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

test('AI card says "off" when summaries are turned off in Settings · AI', async ({ page }) => {
  const toggle = async (on: boolean) => {
    await page.goto('/Settings/AI');
    const box = page.locator('input[name="Input.SummarizationEnabled"][type="checkbox"]');
    if ((await box.isChecked()) === on) return;
    await box.setChecked(on);
    await page.locator('.dfh-form-footer button.btn-primary').click();
    await expect(page.locator('.dfh-toast')).toContainText('AI settings saved');
  };
  await page.goto('/Documents');
  const row = page.locator('tbody tr[data-ai="0"]').first();
  test.skip((await row.count()) === 0, 'every document has a summary');
  const id = await row.getAttribute('data-id');
  await toggle(false);
  try {
    await page.goto(`/Documents/Details/${id}?tab=ai`);
    await expect(page.locator('.dfh-ai-card.is-off')).toContainText('Summaries are turned off');
    await expect(page.locator('form[data-dfh-ai-generate] button')).toHaveText(/Summarize this document/);
  } finally {
    await toggle(true);
  }
});

// Sprint 18: images are previewed inline with safe headers; other types have no inline file.
test('image upload is previewed inline with nosniff and a sandboxing CSP', async ({ page }) => {
  const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==', 'base64');
  await page.goto('/Documents/Upload');
  await page.locator('[data-dfh-file]').setInputFiles({ name: 'e2e-image.png', mimeType: 'image/png', buffer: png });
  await page.locator('[data-dfh-project]').selectOption({ index: 1 });
  const ai = page.locator('input[name="GenerateSummary"][type="checkbox"]');
  if (await ai.count()) await ai.uncheck();
  await page.locator('[data-dfh-upload-submit]').click();
  await expect(page).toHaveURL(/\/Documents\/Details\/\d+/);

  const img = page.locator('.dfh-preview-image img');
  await expect.poll(() => img.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth)).toBe(1);
  const response = await page.request.get((await img.getAttribute('src'))!);
  expect(response.status()).toBe(200);
  expect(response.headers()['content-type']).toBe('image/png');
  expect(response.headers()['x-content-type-options']).toBe('nosniff');
  expect(response.headers()['content-security-policy']).toContain('sandbox');

  // A text document has no inline file.
  await page.goto('/Documents');
  const href = (await page.locator('tbody tr[data-ext="TXT"] a[href*="/Documents/Details/"]').first().getAttribute('href'))!;
  const text = await page.request.get(href + '?handler=File');
  expect(text.status()).toBe(404);
});

// Sprint 19: a PDF that PDF.js cannot open says so and offers the text view.
test('a damaged PDF explains itself and links to the text view', async ({ page }) => {
  await page.goto('/Documents/Upload');
  await page.locator('[data-dfh-file]').setInputFiles({
    name: 'e2e-damaged.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4\nthis is not a real PDF body\n'),
  });
  await page.locator('[data-dfh-project]').selectOption({ index: 1 });
  const ai = page.locator('input[name="GenerateSummary"][type="checkbox"]');
  if (await ai.count()) await ai.uncheck();
  await page.locator('[data-dfh-upload-submit]').click();
  await expect(page).toHaveURL(/\/Documents\/Details\/\d+/);

  await expect(page.locator('[data-dfh-pdf]')).toHaveAttribute('data-state', 'error');
  await expect(page.locator('.dfh-pdf-status')).toContainText('could not be shown');
  await expect(page.locator('[data-dfh-pdf-controls]')).toBeHidden();
  await page.locator('.dfh-pdf-status a', { hasText: 'Show the text' }).click();
  await expect(page).toHaveURL(/view=text/);
  await expect(page.locator('.dfh-preview-note')).toContainText('No text could be found');
  await page.locator('.dfh-preview a', { hasText: 'Show the pages' }).click();
  await expect(page.locator('[data-dfh-pdf]')).toHaveAttribute('data-state', 'error');

  // Clean up: the demo-data PDF tests that may run next take the first PDF in the list.
  await page.locator('button[aria-label="More actions"]').first().click();
  await page.locator('[data-dfh-action="delete"]').click();
  await page.locator('#dfh-delete-modal button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Deleted "e2e-damaged"');
});

// Sprint 20: Word documents. Both tests delete their document, like the PDF one above.
async function uploadFile(page: Page, file: string | { name: string; mimeType: string; buffer: Buffer }) {
  await page.goto('/Documents/Upload');
  await page.locator('[data-dfh-file]').setInputFiles(file);
  await page.locator('[data-dfh-project]').selectOption({ index: 1 });
  const ai = page.locator('input[name="GenerateSummary"][type="checkbox"]');
  if (await ai.count()) await ai.uncheck();
  await page.locator('[data-dfh-upload-submit]').click();
  await expect(page).toHaveURL(/\/Documents\/Details\/\d+/);
}

async function deleteOpenDocument(page: Page, title: string) {
  await page.locator('button[aria-label="More actions"]').first().click();
  await page.locator('[data-dfh-action="delete"]').click();
  await page.locator('#dfh-delete-modal button[type="submit"]').click();
  await expect(page.locator('.dfh-toast')).toContainText(`Deleted "${title}"`);
}

test('a Word document with markup in its text and a script link is shown safely', async ({ page }) => {
  await uploadFile(page, path.join(__dirname, 'fixtures', 'tricky.docx'));

  const docx = page.locator('.dfh-docx');
  // The heading style has a Polish id ("Nagwek1"); it is found by its name.
  await expect(docx.locator('h1')).toHaveText('E2E tricky heading');
  await expect(docx).toContainText('<script>window.__docx_xss=1</script>');
  await expect(docx).toContainText('stays text');
  await expect(docx.locator('script, img')).toHaveCount(0);
  await expect(docx.locator('a')).toHaveCount(1);
  await expect(docx.locator('a')).toHaveAttribute('href', 'https://example.com/handbook');
  await expect(docx.locator('a')).toHaveAttribute('rel', 'noopener noreferrer nofollow');
  await expect(docx).toContainText('script link and web link');
  await expect(docx.locator('.dfh-docx-table td')).toHaveText(['cell one', 'cell two']);
  await expect(docx.locator('.dfh-docx-table td strong')).toHaveText('cell two');
  expect(await page.evaluate(() => (window as unknown as { __docx_xss?: number }).__docx_xss)).toBeUndefined();

  await page.locator('.dfh-segmented a', { hasText: 'Text' }).click();
  await expect(page.locator('pre.dfh-code')).toContainText('E2E tricky heading');
  expect(await page.evaluate(() => (window as unknown as { __docx_xss?: number }).__docx_xss)).toBeUndefined();

  await deleteOpenDocument(page, 'tricky');
});

test('a damaged Word document offers the download instead of a preview', async ({ page }) => {
  await uploadFile(page, {
    name: 'e2e-damaged.docx',
    mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    buffer: Buffer.concat([Buffer.from([0x50, 0x4b, 0x03, 0x04]), Buffer.from('this is not a real Word file')]),
  });

  await expect(page.locator('.dfh-docx')).toHaveCount(0);
  await expect(page.locator('.dfh-preview-note')).toContainText('damaged or password-protected');
  await expect(page.locator('.dfh-preview-page button', { hasText: 'Download DOCX' })).toBeVisible();

  await deleteOpenDocument(page, 'e2e-damaged');
});

// Sprint 21: the line diff of two uploaded versions, with known edits.
test('compare shows the exact added, removed and changed lines of two versions', async ({ page }) => {
  const v1 = Array.from({ length: 40 }, (_, i) => `Line ${i + 1}`);
  v1[2] = 'The budget is 100 EUR for this quarter';
  v1[29] = 'spaced  out   line';
  const v2 = v1.slice();
  v2[2] = 'The budget is 250 EUR for this quarter';
  v2[29] = 'spaced out line';
  v2.splice(19, 1); // "Line 20" is removed
  v2.push('A new closing line', '<b>not bold</b>');

  await uploadFile(page, { name: 'e2e-diff.txt', mimeType: 'text/plain', buffer: Buffer.from(v1.join('\n') + '\n') });
  const id = /\/Documents\/Details\/(\d+)/.exec(page.url())![1];
  await page.locator('a', { hasText: 'New version' }).click();
  await page.locator('#new-version [data-dfh-file]').setInputFiles({ name: 'e2e-diff.txt', mimeType: 'text/plain', buffer: Buffer.from(v2.join('\r\n')) });
  await page.locator('#ChangeSummary').fill('Budget and closing line');
  await page.locator('[data-dfh-upload-submit]').click();
  await expect(page.locator('.dfh-toast')).toContainText('Uploaded v2');

  await page.goto(`/Documents/Compare/${id}?from=1&to=2`);
  const diff = page.locator('[data-dfh-diff]');
  await expect(page.locator('.dfh-diff-count .is-add')).toHaveText('+4');
  await expect(page.locator('.dfh-diff-count .is-del')).toHaveText('−3');
  await expect(page.locator('[data-dfh-diff-pos]')).toHaveText('change 1 of 4');

  // Change 1: one line with the changed words marked; both line numbers are 3.
  const first = diff.locator('#change-1');
  await expect(first.locator('.is-del .dfh-diff-t')).toHaveText('The budget is 100 EUR for this quarter');
  await expect(first.locator('.is-del .dfh-diff-w')).toHaveText('100');
  await expect(first.locator('.is-add .dfh-diff-w')).toHaveText('250');
  await expect(first.locator('.is-del .dfh-diff-n.is-old')).toHaveText('3');
  await expect(first.locator('.is-add .dfh-diff-n.is-new')).toHaveText('3');
  // Change 2: a removed line only. Change 4: two added lines, markup shown as text.
  await expect(diff.locator('#change-2 .dfh-diff-line')).toHaveCount(1);
  await expect(diff.locator('#change-2 .is-del .dfh-diff-t')).toHaveText('Line 20');
  await expect(diff.locator('#change-4 .is-add .dfh-diff-t')).toHaveText(['A new closing line', '<b>not bold</b>']);
  await expect(diff.locator('b')).toHaveCount(0);
  // After the removed line the numbers differ: old 21 is new 20.
  const after = diff.locator('.dfh-diff-line.is-ctx', { has: page.locator('.dfh-diff-n.is-old', { hasText: /^21$/ }) });
  await expect(after.locator('.dfh-diff-n.is-new')).toHaveText('20');

  // Unchanged stretches are folded around three lines of context.
  await expect(diff.locator('.dfh-diff-fold')).toHaveText(['⋯ 10 unchanged lines', '⋯ 3 unchanged lines', '⋯ 4 unchanged lines']);
  await diff.locator('.dfh-diff-fold').first().click();
  await expect(page).toHaveURL(/context=all/);
  await expect(diff.locator('.dfh-diff-fold')).toHaveCount(0);
  await expect(diff.locator('.dfh-diff-line.is-ctx')).toHaveCount(37);
  await expect(page.locator('[data-dfh-diff-all]')).toHaveAttribute('aria-pressed', 'true');

  // Ignoring whitespace drops the spacing-only change.
  await page.locator('[data-dfh-diff-ws]').click();
  await expect(page.locator('.dfh-diff-count .is-add')).toHaveText('+3');
  await expect(page.locator('.dfh-diff-count .is-del')).toHaveText('−2');
  await expect(page.locator('[data-dfh-diff-pos]')).toHaveText('change 1 of 3');

  // An address with a change number opens at that change.
  await page.goto(`/Documents/Compare/${id}?from=1&to=2#change-3`);
  await expect(page.locator('[data-dfh-diff-pos]')).toHaveText('change 3 of 4');
  await expect(diff.locator('#change-3')).toHaveClass(/is-current/);
  await expect(diff.locator('#change-3 .is-del .dfh-diff-t')).toHaveText('spaced  out   line');

  await page.goto(`/Documents/Details/${id}`);
  await deleteOpenDocument(page, 'e2e-diff');
});
