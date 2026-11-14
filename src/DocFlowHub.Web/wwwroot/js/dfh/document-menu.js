// Documents table: row menu (design 7j, S15-2) and row shortcuts (S15-3).
// Right click or the ⋯ button opens the menu; on a focused row M = move, F2 = edit, Del = delete,
// Shift F10 / the context-menu key = menu. Uses the dialogs from document-dialogs.js.
(function () {
    'use strict';

    const D = window.DocFlowHub;

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-dfh-doc-table]').forEach(init);
    });

    function init(table) {
        const demo = table.dataset.demo === 'true';
        const demoTip = 'Read-only demo — changes are disabled';
        const ownerTip = 'Only the owner can move or delete this document';

        function info(row) {
            return {
                id: Number(row.dataset.id),
                title: row.dataset.title,
                ext: row.dataset.ext,
                kind: row.dataset.kind,
                location: row.dataset.location,
                owner: row.dataset.owner === '1',
                version: Number(row.dataset.version || 0),
                hasSummary: row.dataset.ai === '1',
                size: row.dataset.size
            };
        }

        function canChange(doc) { return doc.owner && !demo; }
        function changeTip(doc) { return demo ? demoTip : (doc.owner ? null : ownerTip); }

        function move(doc) {
            if (!canChange(doc)) { D.showInfo(changeTip(doc)); return; }
            D.documentDialogs.openMove([doc]);
        }

        function remove(doc) {
            if (!canChange(doc)) { D.showInfo(changeTip(doc)); return; }
            D.documentDialogs.openDelete({ id: doc.id, title: doc.title, location: doc.location, size: doc.size, versions: doc.version });
        }

        function edit(doc) {
            if (demo) { D.showInfo(demoTip); return; }
            window.location.href = '/Documents/Edit/' + doc.id;
        }

        function download(doc) {
            post('/Documents?handler=Download', { documentId: doc.id, returnUrl: location.pathname + location.search });
        }

        function summarize(doc) {
            post('/Documents/Details/' + doc.id + '?handler=GenerateSummary', {});
        }

        function post(url, fields) {
            const form = document.createElement('form');
            form.method = 'post';
            form.action = url;
            form.hidden = true;
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            fields.__RequestVerificationToken = token ? token.value : '';
            Object.keys(fields).forEach(function (name) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = name;
                input.value = fields[name];
                form.appendChild(input);
            });
            document.body.appendChild(form);
            form.submit();
        }

        function copyLink(doc) {
            const url = location.origin + '/Documents/Details/' + doc.id;
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(url).then(
                    function () { D.showSuccess('Link copied'); },
                    function () { D.showInfo(url); });
            } else {
                D.showInfo(url);
            }
        }

        function entries(doc) {
            const tip = changeTip(doc);
            return [
                { label: 'Open', icon: 'bi-eye', hint: '↵', href: '/Documents/Details/' + doc.id },
                { label: 'Download', icon: 'bi-download', run: function () { download(doc); } },
                { separator: true },
                { label: 'Upload new version…', icon: 'bi-file-earmark-plus', href: '/Documents/Edit/' + doc.id + '#new-version', disabled: demo, title: demo ? demoTip : null },
                doc.version > 1
                    ? { label: 'Compare versions…', icon: 'bi-arrow-left-right', href: '/Documents/Compare/' + doc.id }
                    : { label: 'Compare versions…', icon: 'bi-arrow-left-right', disabled: true, title: 'Only one version so far' },
                doc.hasSummary
                    ? { label: 'Open AI summary', icon: 'bi-stars', ai: true, href: '/Documents/Details/' + doc.id + '?tab=ai' }
                    : { label: 'Generate summary', icon: 'bi-stars', ai: true, run: function () { summarize(doc); }, disabled: demo, title: demo ? demoTip : null },
                { separator: true },
                { label: 'Move to…', icon: 'bi-arrows-move', hint: 'M', run: function () { move(doc); }, disabled: !canChange(doc), title: tip },
                { label: 'Edit details…', icon: 'bi-pencil', hint: 'F2', href: '/Documents/Edit/' + doc.id, disabled: demo, title: demo ? demoTip : null },
                { label: 'Copy link', icon: 'bi-link-45deg', run: function () { copyLink(doc); } },
                { separator: true },
                { label: 'Delete…', icon: 'bi-trash', hint: 'Del', danger: true, run: function () { remove(doc); }, disabled: !canChange(doc), title: tip }
            ];
        }

        function openFor(row, position) {
            if (table.dfhSelect) table.dfhSelect(row, false);
            const doc = info(row);
            D.menu.open(entries(doc), position, 'Actions for ' + doc.title);
        }

        const tbody = table.tBodies[0];
        tbody.addEventListener('contextmenu', function (e) {
            const row = e.target.closest('tr[data-id]');
            // Keep the browser menu on links and form controls (open in new tab, copy…).
            if (!row || e.target.closest('a, input')) return;
            e.preventDefault();
            openFor(row, { x: e.clientX, y: e.clientY });
        });

        tbody.addEventListener('click', function (e) {
            const button = e.target.closest('[data-dfh-row-menu]');
            if (!button) return;
            e.preventDefault();
            openFor(button.closest('tr'), { anchor: button });
        });

        tbody.addEventListener('keydown', function (e) {
            const row = e.target.closest('tr[data-id]');
            if (!row || e.target !== row || e.ctrlKey || e.metaKey || e.altKey) return;
            const doc = info(row);
            if (e.key === 'ContextMenu' || (e.key === 'F10' && e.shiftKey)) {
                e.preventDefault();
                const button = row.querySelector('[data-dfh-row-menu]');
                openFor(row, button ? { anchor: button } : { x: row.getBoundingClientRect().left + 40, y: row.getBoundingClientRect().bottom });
            } else if (e.shiftKey) {
                return;
            } else if (e.key === 'm' || e.key === 'M') {
                e.preventDefault();
                move(doc);
            } else if (e.key === 'F2') {
                e.preventDefault();
                edit(doc);
            } else if (e.key === 'Delete') {
                e.preventDefault();
                remove(doc);
            }
        });
    }
})();
