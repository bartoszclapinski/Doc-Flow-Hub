// Documents list (design 3h, state 7c): row selection + inspector, multi-select + bulk bar,
// tree expand/collapse. The dialogs live in document-dialogs.js.
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
                const items = checks().filter(function (c) { return c.checked; }).map(function (c) { return D.documentDialogs.describe(c.closest('tr')); });
                if (kind === 'delete') D.documentDialogs.openBulkDelete(items);
                if (kind === 'move') D.documentDialogs.openMove(items);
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
})();
