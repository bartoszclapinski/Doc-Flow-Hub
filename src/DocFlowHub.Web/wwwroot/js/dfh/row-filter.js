// Filter box for short tables (Projects 8a, Teams): <input data-dfh-row-filter="#table-id">.
// Rows with data-filter (lower-case text) that don't contain the query are hidden; a row with
// .dfh-filter-empty shows when nothing matches. A selectable table moves its selection along.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-dfh-row-filter]').forEach(function (input) {
            const table = document.querySelector(input.getAttribute('data-dfh-row-filter'));
            if (!table) return;
            const rows = Array.from(table.querySelectorAll('tbody tr[data-filter]'));
            const empty = table.querySelector('.dfh-filter-empty');

            input.addEventListener('input', function () {
                const q = input.value.trim().toLowerCase();
                let shown = 0;
                rows.forEach(function (row) {
                    const match = !q || row.dataset.filter.indexOf(q) >= 0;
                    row.hidden = !match;
                    if (match) shown++;
                });
                if (empty) empty.hidden = shown > 0;
                const selected = table.querySelector('tbody tr.is-selected');
                if (table.hasAttribute('data-dfh-select-table') && shown > 0 && (!selected || selected.hidden)) {
                    table.dispatchEvent(new Event('dfh:select-first'));
                }
            });
        });
    });
})();
