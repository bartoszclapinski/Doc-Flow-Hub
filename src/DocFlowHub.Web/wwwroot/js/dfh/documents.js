// Documents list (design 3h; states 7c, 7g, 7h, 7i): row selection + inspector, multi-select +
// bulk bar, tree expand/collapse, delete / bulk delete / move dialogs.
(function () {
    'use strict';

    const D = window.DocFlowHub;
    const SKELETON =
        '<div class="dfh-insp-section" aria-busy="true">' +
        '<span class="dfh-skel" style="width: 28px"></span>' +
        '<span class="dfh-skel dfh-skel--title" style="width: 80%"></span>' +
        '<span class="dfh-skel" style="width: 50%"></span>' +
        '<span class="dfh-skel" style="width: 100%; margin-top: 10px"></span>' +
        '<span class="dfh-skel" style="width: 90%"></span>' +
        '<span class="dfh-skel" style="width: 70%"></span>' +
        '</div>';

    document.addEventListener('DOMContentLoaded', function () {
        initTree();
        const table = document.querySelector('[data-dfh-doc-table]');
        if (table) {
            initSelection(table);
            initChecks(table);
        }
        initDelete();
        initBulkDelete();
        initMove();
    });

    // ---------- Tree: chevrons expand and collapse folders ----------
    function initTree() {
        document.addEventListener('click', function (e) {
            const toggle = e.target.closest('[data-dfh-tree-toggle]');
            if (!toggle) return;
            const item = toggle.closest('[aria-expanded]');
            if (!item) return;
            item.setAttribute('aria-expanded', item.getAttribute('aria-expanded') === 'true' ? 'false' : 'true');
        });
    }

    // ---------- Row selection → inspector ----------
    function initSelection(table) {
        const body = document.getElementById('dfh-inspector-body');
        const cache = new Map();
        let controller = null;

        if (body && body.dataset.id) cache.set(body.dataset.id, body.innerHTML);

        function rows() { return Array.from(table.tBodies[0].rows); }
        function selected() { return table.querySelector('tbody tr.is-selected'); }

        function select(row, focus) {
            if (!row) return;
            rows().forEach(function (r) {
                const on = r === row;
                r.classList.toggle('is-selected', on);
                r.setAttribute('aria-selected', on ? 'true' : 'false');
                r.tabIndex = on ? 0 : -1;
            });
            if (focus) {
                row.focus({ preventScroll: true });
                row.scrollIntoView({ block: 'nearest' });
            }
            load(row.dataset.id);
        }

        async function load(id) {
            if (!body || body.dataset.id === id) return;
            body.dataset.id = id;
            if (controller) controller.abort();

            if (cache.has(id)) {
                body.innerHTML = cache.get(id);
                return;
            }
            body.innerHTML = SKELETON;
            controller = new AbortController();
            const url = '/Documents?handler=Inspector&id=' + encodeURIComponent(id) +
                '&returnUrl=' + encodeURIComponent(body.dataset.returnUrl || '/Documents');
            try {
                const response = await fetch(url, {
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    credentials: 'same-origin',
                    signal: controller.signal
                });
                const html = await response.text();
                if (body.dataset.id !== id) return;
                if (response.ok) {
                    cache.set(id, html);
                    body.innerHTML = html;
                } else {
                    body.innerHTML = emptyState(response.status === 404 ? html : 'The details could not be loaded.');
                }
            } catch (e) {
                if (e.name === 'AbortError') return;
                if (body.dataset.id === id) body.innerHTML = emptyState('The details could not be loaded. Check your connection.');
            }
        }

        function emptyState(text) {
            const box = document.createElement('div');
            box.className = 'dfh-empty dfh-empty--inline';
            const p = document.createElement('p');
            p.textContent = text;
            box.appendChild(p);
            return box.outerHTML;
        }

        function open(row) {
            if (row) window.location.href = '/Documents/Details/' + row.dataset.id;
        }

        table.tBodies[0].addEventListener('click', function (e) {
            if (e.target.closest('a, button, input, label')) return;
            select(e.target.closest('tr'), false);
        });
        table.tBodies[0].addEventListener('dblclick', function (e) {
            if (e.target.closest('a, button, input, label')) return;
            open(e.target.closest('tr'));
        });
        table.tBodies[0].addEventListener('keydown', function (e) {
            const row = e.target.closest('tr');
            if (!row || e.target !== row) return;
            const list = rows();
            const index = list.indexOf(row);
            if (e.key === 'ArrowDown' && index < list.length - 1) {
                e.preventDefault();
                select(list[index + 1], true);
            } else if (e.key === 'ArrowUp' && index > 0) {
                e.preventDefault();
                select(list[index - 1], true);
            } else if (e.key === 'Enter') {
                e.preventDefault();
                open(row);
            } else if (e.key === ' ') {
                e.preventDefault();
                const check = row.querySelector('.dfh-row-check');
                check.checked = !check.checked;
                check.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
        // Focus moving into a row (Tab, click on a checkbox) selects it too.
        table.tBodies[0].addEventListener('focusin', function (e) {
            const row = e.target.closest('tr');
            if (row && !row.classList.contains('is-selected')) select(row, false);
        });

        // Keep the selected row reachable with Tab.
        if (!selected() && rows().length) rows()[0].tabIndex = 0;
    }

    // ---------- Checkboxes → bulk bar ----------
    function initChecks(table) {
        const all = document.getElementById('dfh-check-all');
        const bar = document.getElementById('dfh-bulkbar');
        const count = bar && bar.querySelector('[data-dfh-bulk-count]');
        const checks = function () { return Array.from(table.querySelectorAll('.dfh-row-check')); };

        function update() {
            const list = checks();
            const on = list.filter(function (c) { return c.checked; });
            list.forEach(function (c) { c.closest('tr').classList.toggle('is-checked', c.checked); });
            if (all) {
                all.checked = on.length > 0 && on.length === list.length;
                all.indeterminate = on.length > 0 && on.length < list.length;
            }
            if (bar) {
                bar.hidden = on.length === 0;
                count.textContent = String(on.length);
            }
        }

        table.addEventListener('change', function (e) {
            if (e.target === all) {
                checks().forEach(function (c) { c.checked = all.checked; });
            }
            update();
        });

        if (bar) {
            bar.addEventListener('click', function (e) {
                const action = e.target.closest('[data-dfh-bulk]');
                if (!action) return;
                const kind = action.getAttribute('data-dfh-bulk');
                if (kind === 'clear') {
                    checks().forEach(function (c) { c.checked = false; });
                    update();
                    return;
                }
                const items = checks().filter(function (c) { return c.checked; }).map(function (c) { return describe(c.closest('tr')); });
                if (kind === 'delete') openBulkDelete(items);
                if (kind === 'move') openMove(items);
            });
        }

        // Escape inside a dialog or an open menu closes that, not the selection. Capture phase: this
        // runs before Bootstrap closes the overlay, so it can still see that one was open.
        window.addEventListener('keydown', function (e) {
            if (e.key !== 'Escape' || !bar || bar.hidden) return;
            if (e.target.closest('.modal') || document.querySelector('.modal.show, .dropdown-menu.show')) return;
            checks().forEach(function (c) { c.checked = false; });
            update();
        }, true);
        update();
    }

    function describe(row) {
        return {
            id: Number(row.dataset.id),
            title: row.dataset.title,
            ext: row.dataset.ext,
            kind: row.dataset.kind,
            location: row.dataset.location,
            owner: row.dataset.owner === '1'
        };
    }

    // ---------- Inspector menu actions ----------
    document.addEventListener('click', function (e) {
        const action = e.target.closest('[data-dfh-action]');
        if (!action) return;
        const item = {
            id: Number(action.dataset.id),
            title: action.dataset.title,
            location: action.dataset.location,
            size: action.dataset.size,
            versions: Number(action.dataset.versions || 0)
        };
        if (action.dataset.dfhAction === 'delete') openDelete(item);
        if (action.dataset.dfhAction === 'move') {
            const row = document.querySelector('tr[data-id="' + item.id + '"]');
            openMove([row ? describe(row) : item]);
        }
    });

    // ---------- 7g: delete one document ----------
    let deleteModal = null;
    function initDelete() {
        const el = document.getElementById('dfh-delete-modal');
        if (!el || !window.bootstrap) return;
        deleteModal = bootstrap.Modal.getOrCreateInstance(el);
        el.addEventListener('shown.bs.modal', function () {
            el.querySelector('[data-bs-dismiss="modal"].btn').focus();
        });
    }

    function openDelete(item) {
        if (!deleteModal) return;
        const el = document.getElementById('dfh-delete-modal');
        el.querySelector('#dfh-delete-title').textContent = 'Delete “' + item.title + '”?';
        el.querySelector('[data-dfh-delete-versions]').textContent =
            item.versions > 1 ? 'all ' + item.versions + ' versions' : 'its file';
        el.querySelector('[data-dfh-delete-location]').textContent = item.location || 'No project';
        el.querySelector('[data-dfh-delete-size]').textContent = item.size || '';
        el.querySelector('input[name="documentId"]').value = String(item.id);
        deleteModal.show();
    }

    // ---------- 7h: delete several documents ----------
    let bulkModal = null;
    let bulkItems = [];
    let bulkDone = false;

    function initBulkDelete() {
        const el = document.getElementById('dfh-bulk-delete-modal');
        if (!el || !window.bootstrap) return;
        bulkModal = bootstrap.Modal.getOrCreateInstance(el);
        el.querySelector('[data-dfh-bulk-confirm]').addEventListener('click', runBulkDelete);
        el.querySelector('[data-dfh-bulk-close]').addEventListener('click', function () { bulkModal.hide(); });
        el.addEventListener('hidden.bs.modal', function () {
            if (bulkDone) window.location.reload();
        });
        el.addEventListener('shown.bs.modal', function () {
            el.querySelector('[data-dfh-bulk-cancel]').focus();
        });
    }

    function openBulkDelete(items) {
        if (!bulkModal || items.length === 0) return;
        const el = document.getElementById('dfh-bulk-delete-modal');
        bulkItems = items;
        bulkDone = false;
        const noun = items.length === 1 ? 'document' : 'documents';
        el.querySelector('#dfh-bulk-delete-title').textContent = 'Delete ' + items.length + ' ' + noun + '?';
        el.querySelector('[data-dfh-bulk-intro]').hidden = false;
        el.querySelector('[data-dfh-bulk-confirm] span').textContent = 'Delete ' + items.length + ' ' + noun;
        el.querySelector('[data-dfh-bulk-confirm]').hidden = false;
        el.querySelector('[data-dfh-bulk-confirm]').disabled = false;
        el.querySelector('[data-dfh-bulk-cancel]').hidden = false;
        el.querySelector('[data-dfh-bulk-close]').hidden = true;
        el.querySelector('.btn-close').hidden = false;
        el.querySelector('[data-dfh-bulk-progress]').hidden = true;
        el.querySelector('.dfh-progress-fill').style.width = '0%';

        const list = el.querySelector('[data-dfh-bulk-list]');
        list.replaceChildren.apply(list, items.map(function (item) {
            const li = document.createElement('li');
            li.dataset.id = String(item.id);
            const ext = document.createElement('span');
            ext.className = 'dfh-filetype dfh-filetype--' + (item.kind || 'txt');
            ext.textContent = item.ext || '';
            const name = document.createElement('span');
            name.className = 'dfh-ellipsis';
            name.textContent = item.title;
            const status = document.createElement('span');
            status.className = 'dfh-result';
            if (!item.owner) {
                status.className += ' is-warning';
                status.textContent = 'not owner';
                status.title = 'You can only delete documents you own; this one will be skipped.';
            }
            li.append(ext, name, status);
            return li;
        }));
        bulkModal.show();
    }

    async function runBulkDelete() {
        const el = document.getElementById('dfh-bulk-delete-modal');
        const noun = bulkItems.length === 1 ? 'document' : 'documents';
        el.querySelector('#dfh-bulk-delete-title').textContent = 'Deleting ' + bulkItems.length + ' ' + noun;
        el.querySelector('[data-dfh-bulk-intro]').hidden = true;
        el.querySelector('[data-dfh-bulk-confirm]').disabled = true;
        el.querySelector('[data-dfh-bulk-cancel]').hidden = true;
        el.querySelector('.btn-close').hidden = true;
        const progress = el.querySelector('[data-dfh-bulk-progress]');
        const caption = el.querySelector('[data-dfh-bulk-caption]');
        progress.hidden = false;
        caption.textContent = 'deleting…';
        el.querySelector('.dfh-progress-fill').style.width = '35%';

        const result = await D.postJson('/Documents?handler=BulkDelete', {
            documentIds: bulkItems.map(function (i) { return i.id; })
        });

        if (!result.success) {
            bulkModal.hide();
            D.reportFailure(result, 'The documents could not be deleted.');
            return;
        }

        (result.results || []).forEach(function (r) {
            const status = el.querySelector('[data-dfh-bulk-list] li[data-id="' + r.documentId + '"] .dfh-result');
            if (!status) return;
            status.className = 'dfh-result ' + (r.success ? 'is-success' : 'is-danger');
            status.textContent = '';
            status.title = '';
            if (r.success) {
                status.innerHTML = '<i class="bi bi-check-lg" aria-label="deleted"></i>';
            } else {
                status.textContent = r.reason || 'failed';
            }
        });
        el.querySelector('.dfh-progress-fill').style.width = '100%';
        caption.textContent = result.deleted + ' deleted' + (result.failed ? ' · ' + result.failed + ' skipped' : '') + ' · done';
        el.querySelector('#dfh-bulk-delete-title').textContent =
            'Deleted ' + result.deleted + ' of ' + bulkItems.length + ' ' + noun;
        el.querySelector('[data-dfh-bulk-confirm]').hidden = true;
        el.querySelector('[data-dfh-bulk-close]').hidden = false;
        el.querySelector('[data-dfh-bulk-close]').focus();
        bulkDone = result.deleted > 0;
        if (bulkDone) {
            D.toastAfterReload('success', 'Deleted ' + result.deleted + ' ' + (result.deleted === 1 ? 'document' : 'documents'),
                result.failed ? { meta: result.failed + ' skipped' } : undefined);
        }
    }

    // ---------- 7i: move to a project or folder ----------
    let moveModal = null;
    let moveItems = [];
    let moveTarget = null;
    let moveRows = [];

    function initMove() {
        const el = document.getElementById('dfh-move-modal');
        if (!el || !window.bootstrap) return;
        moveModal = bootstrap.Modal.getOrCreateInstance(el);

        const box = el.querySelector('[data-dfh-move-tree]');
        const filter = el.querySelector('[data-dfh-move-filter]');
        if (box) {
            let projects = [];
            try { projects = JSON.parse(document.getElementById('dfh-move-data').textContent) || []; } catch (e) { /* none */ }
            buildPicker(box, projects);
            box.addEventListener('click', function (e) {
                const row = e.target.closest('.dfh-picker-row');
                if (row) choose(row);
            });
            box.addEventListener('dblclick', function (e) {
                if (e.target.closest('.dfh-picker-row')) runMove();
            });
        }
        if (filter) {
            filter.addEventListener('input', function () { applyFilter(filter.value); });
            filter.addEventListener('keydown', function (e) {
                if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                    e.preventDefault();
                    step(e.key === 'ArrowDown' ? 1 : -1);
                } else if (e.key === 'Enter') {
                    e.preventDefault();
                    if (moveTarget) runMove();
                }
            });
        }
        el.querySelector('[data-dfh-move-confirm]').addEventListener('click', runMove);
        el.addEventListener('shown.bs.modal', function () {
            if (filter) filter.focus();
        });
    }

    function buildPicker(box, projects) {
        moveRows = [];
        function add(level, data) {
            const row = document.createElement('button');
            row.type = 'button';
            row.className = 'dfh-picker-row';
            row.setAttribute('role', 'option');
            row.setAttribute('aria-selected', 'false');
            row.style.setProperty('--level', String(level));
            row.dataset.project = String(data.projectId);
            if (data.folderId) row.dataset.folder = String(data.folderId);
            row.dataset.path = data.path;
            row.dataset.name = data.name;
            if (data.color) {
                const swatch = document.createElement('span');
                swatch.className = 'dfh-swatch';
                swatch.style.background = data.color;
                row.appendChild(swatch);
            } else {
                const icon = document.createElement('i');
                icon.className = 'bi bi-folder2';
                icon.setAttribute('aria-hidden', 'true');
                row.appendChild(icon);
            }
            const name = document.createElement('span');
            name.className = 'dfh-picker-name';
            name.textContent = data.name;
            row.appendChild(name);
            if (typeof data.count === 'number') {
                const count = document.createElement('span');
                count.className = 'dfh-tree-count';
                count.textContent = String(data.count);
                row.appendChild(count);
            }
            box.appendChild(row);
            moveRows.push({ el: row, level: level, name: data.name.toLowerCase(), parents: data.parents });
            return moveRows[moveRows.length - 1];
        }
        function addFolders(folders, project, level, path, parents) {
            folders.forEach(function (f) {
                const own = add(level, {
                    projectId: project.id, folderId: f.id, name: f.name, count: f.count,
                    path: path + ' / ' + f.name, parents: parents
                });
                addFolders(f.children || [], project, level + 1, path + ' / ' + f.name, parents.concat([own]));
            });
        }
        projects.forEach(function (p) {
            const root = add(0, { projectId: p.id, name: p.name, color: p.color, path: p.name, parents: [] });
            addFolders(p.folders || [], p, 1, p.name, [root]);
        });
    }

    function applyFilter(text) {
        const q = text.trim().toLowerCase();
        moveRows.forEach(function (r) { r.match = !q || r.name.indexOf(q) >= 0; });
        moveRows.forEach(function (r) {
            // A match keeps its ancestors visible so the path stays readable.
            if (r.match) r.parents.forEach(function (p) { p.keep = true; });
        });
        moveRows.forEach(function (r) {
            r.el.hidden = !(r.match || r.keep);
            r.keep = false;
            const name = r.el.querySelector('.dfh-picker-name');
            const original = r.el.dataset.name;
            const at = q ? original.toLowerCase().indexOf(q) : -1;
            if (at >= 0) {
                name.replaceChildren(
                    document.createTextNode(original.slice(0, at)),
                    Object.assign(document.createElement('b'), { textContent: original.slice(at, at + q.length) }),
                    document.createTextNode(original.slice(at + q.length)));
            } else {
                name.textContent = original;
            }
        });
        const box = document.querySelector('[data-dfh-move-tree]');
        let empty = box && box.querySelector('.dfh-picker-empty');
        const none = moveRows.every(function (r) { return r.el.hidden; });
        if (box && none && !empty) {
            empty = document.createElement('p');
            empty.className = 'dfh-picker-empty';
            box.appendChild(empty);
        }
        if (empty) {
            empty.hidden = !none;
            empty.textContent = 'No project or folder matches “' + text.trim() + '”.';
        }
        if (moveTarget && moveTarget.hidden) choose(null);
        if (!moveTarget && q) {
            const first = moveRows.find(function (r) { return !r.el.hidden && r.match; });
            if (first) choose(first.el);
        }
    }

    function step(delta) {
        const visible = moveRows.filter(function (r) { return !r.el.hidden; }).map(function (r) { return r.el; });
        if (visible.length === 0) return;
        const index = visible.indexOf(moveTarget);
        const next = visible[Math.min(visible.length - 1, Math.max(0, index + delta))];
        choose(next);
        next.scrollIntoView({ block: 'nearest' });
    }

    function choose(row) {
        moveRows.forEach(function (r) {
            const on = r.el === row;
            r.el.classList.toggle('is-selected', on);
            r.el.setAttribute('aria-selected', on ? 'true' : 'false');
            const icon = r.el.querySelector('i.bi');
            if (icon) icon.className = 'bi ' + (on ? 'bi-folder2-open' : 'bi-folder2');
        });
        moveTarget = row;
        const el = document.getElementById('dfh-move-modal');
        el.querySelector('[data-dfh-move-to]').textContent = row ? row.dataset.path : 'choose a destination';
        el.querySelector('[data-dfh-move-confirm]').disabled = !row;
    }

    function openMove(items) {
        if (!moveModal || items.length === 0) return;
        const el = document.getElementById('dfh-move-modal');
        moveItems = items;
        el.querySelector('#dfh-move-title').textContent = items.length === 1
            ? 'Move “' + items[0].title + '”'
            : 'Move ' + items.length + ' documents';
        const from = el.querySelector('[data-dfh-move-from]');
        if (from) {
            const places = Array.from(new Set(items.map(function (i) { return i.location || 'No project'; })));
            from.textContent = places.length === 1 ? places[0] : places.length + ' locations';
        }
        const filter = el.querySelector('[data-dfh-move-filter]');
        if (filter) {
            filter.value = '';
            applyFilter('');
        }
        choose(null);
        moveModal.show();
    }

    async function runMove() {
        if (!moveTarget) return;
        const el = document.getElementById('dfh-move-modal');
        const button = el.querySelector('[data-dfh-move-confirm]');
        button.disabled = true;
        button.textContent = 'Moving…';
        const target = moveTarget;

        const result = await D.postJson('/Documents?handler=Move', {
            documentIds: moveItems.map(function (i) { return i.id; }),
            projectId: Number(target.dataset.project),
            folderId: target.dataset.folder ? Number(target.dataset.folder) : null
        });
        button.textContent = 'Move here';
        button.disabled = false;

        if (!result.success) {
            moveModal.hide();
            D.reportFailure(result, 'The documents could not be moved.');
            return;
        }
        moveModal.hide();
        const noun = result.moved === 1 ? 'document' : 'documents';
        if (result.moved > 0) {
            D.toastAfterReload('success', 'Moved ' + result.moved + ' ' + noun + ' to ' + target.dataset.path,
                result.failed ? { meta: result.failed + ' skipped (not owner)' } : undefined);
            window.location.reload();
        } else {
            const reason = (result.results || []).map(function (r) { return r.reason; }).filter(Boolean)[0];
            D.showError('Nothing was moved', { meta: reason || 'You can only move documents you own.' });
        }
    }
})();
