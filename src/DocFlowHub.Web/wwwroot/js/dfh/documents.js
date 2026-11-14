// Documents list (design 3h): multi-select + bulk bar. Row selection and the inspector live in
// selectable-table.js, the tree in tree.js, the dialogs in document-dialogs.js.
(function () {
    'use strict';

    const D = window.DocFlowHub;
    document.addEventListener('DOMContentLoaded', function () {
        const table = document.querySelector('[data-dfh-doc-table]');
        if (table) initChecks(table);
    });

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
