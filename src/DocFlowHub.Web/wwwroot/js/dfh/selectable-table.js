// Selectable table + inspector (Documents 3h, Projects 8a). A table with [data-dfh-select-table]:
//   data-inspector-url="/Projects?handler=Inspector&id={id}"  partial HTML for the inspector body
//   data-open-url="/Projects/Details/{id}"                    double-click / Enter
// Rows carry data-id. Click selects and loads the inspector (cached, skeleton meanwhile); ↑/↓ move.
(function () {
    'use strict';

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
        document.querySelectorAll('[data-dfh-select-table]').forEach(init);
    });

    function fill(template, id) {
        return template.replace('{id}', encodeURIComponent(id));
    }

    function init(table) {
        const body = document.getElementById('dfh-inspector-body');
        const inspectorUrl = table.dataset.inspectorUrl;
        const openUrl = table.dataset.openUrl;
        const cache = new Map();
        let controller = null;

        if (body && body.dataset.id) cache.set(body.dataset.id, body.innerHTML);

        function rows() { return Array.from(table.tBodies[0].rows).filter(function (r) { return r.dataset.id && !r.hidden; }); }

        function select(row, focus) {
            if (!row || !row.dataset.id) return;
            Array.from(table.tBodies[0].rows).forEach(function (r) {
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
            if (!body || !inspectorUrl || body.dataset.id === id) return;
            body.dataset.id = id;
            if (controller) controller.abort();

            if (cache.has(id)) {
                body.innerHTML = cache.get(id);
                return;
            }
            body.innerHTML = SKELETON;
            controller = new AbortController();
            let url = fill(inspectorUrl, id);
            if (body.dataset.returnUrl) url += '&returnUrl=' + encodeURIComponent(body.dataset.returnUrl);
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
            if (row && openUrl) window.location.href = fill(openUrl, row.dataset.id);
        }

        // Other scripts (row menu, shortcuts) select rows through the table element.
        table.dfhSelect = select;

        const tbody = table.tBodies[0];
        const phone = window.matchMedia('(max-width: 599.98px)');
        tbody.addEventListener('click', function (e) {
            if (e.target.closest('a, button, input, label')) return;
            const row = e.target.closest('tr');
            // Phones have no inspector: a tap opens the item (design 9a).
            if (phone.matches && row && row.dataset.id && openUrl) { open(row); return; }
            select(row, false);
            // ≤1024px the inspector is an overlay: a row click opens it (design Responsive).
            const P = window.DocFlowHub && window.DocFlowHub.panels;
            if (P && P.isOverlay('inspector')) P.open('inspector');
        });
        tbody.addEventListener('dblclick', function (e) {
            if (e.target.closest('a, button, input, label')) return;
            open(e.target.closest('tr'));
        });
        tbody.addEventListener('keydown', function (e) {
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
                const check = row.querySelector('.dfh-row-check');
                if (!check) return;
                e.preventDefault();
                check.checked = !check.checked;
                check.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
        // Focus moving into a row (Tab, click on a checkbox) selects it too.
        tbody.addEventListener('focusin', function (e) {
            const row = e.target.closest('tr');
            if (row && !row.classList.contains('is-selected')) select(row, false);
        });

        // Keep a row reachable with Tab.
        if (!tbody.querySelector('tr.is-selected') && rows().length) rows()[0].tabIndex = 0;

        // Pages that filter rows in the browser can ask for the first visible row.
        table.addEventListener('dfh:select-first', function () { select(rows()[0], false); });
    }
})();
