// Document details (design 3b): inspector tabs, delete-version dialog, copy link.
(function () {
    'use strict';

    const D = window.DocFlowHub;

    document.addEventListener('DOMContentLoaded', function () {
        const tabs = Array.from(document.querySelectorAll('[data-dfh-tabs] [role="tab"]'));

        function show(name, focus) {
            const tab = tabs.find(function (t) { return t.dataset.tab === name; });
            if (!tab) return;
            tabs.forEach(function (t) {
                const on = t === tab;
                t.setAttribute('aria-selected', on ? 'true' : 'false');
                t.tabIndex = on ? 0 : -1;
                document.getElementById(t.getAttribute('aria-controls')).hidden = !on;
            });
            if (focus) tab.focus();
            // Keep the tab in the address so a reload (or a form post's redirect) lands on it.
            const url = new URL(window.location.href);
            if (name === 'info') url.searchParams.delete('tab'); else url.searchParams.set('tab', name);
            history.replaceState(null, '', url.pathname + url.search + url.hash);
        }

        tabs.forEach(function (t) {
            t.tabIndex = t.getAttribute('aria-selected') === 'true' ? 0 : -1;
            t.addEventListener('click', function () { show(t.dataset.tab, false); });
            t.addEventListener('keydown', function (e) {
                const index = tabs.indexOf(t);
                if (e.key === 'ArrowRight' || e.key === 'ArrowLeft') {
                    e.preventDefault();
                    const next = tabs[(index + (e.key === 'ArrowRight' ? 1 : tabs.length - 1)) % tabs.length];
                    show(next.dataset.tab, true);
                }
            });
        });

        // Links and buttons that open a tab (toolbar "Share", "Open the AI tab").
        document.addEventListener('click', function (e) {
            const opener = e.target.closest('[data-dfh-tab-open]');
            if (!opener) return;
            e.preventDefault();
            show(opener.getAttribute('data-dfh-tab-open'), true);
        });

        // Delete an old version.
        const versionModal = document.getElementById('dfh-version-delete-modal');
        if (versionModal && window.bootstrap) {
            const modal = bootstrap.Modal.getOrCreateInstance(versionModal);
            document.addEventListener('click', function (e) {
                const button = e.target.closest('[data-dfh-version-delete]');
                if (!button || button.disabled) return;
                versionModal.querySelector('#dfh-version-delete-title').textContent = 'Delete v' + button.dataset.number + '?';
                versionModal.querySelector('input[name="versionId"]').value = button.getAttribute('data-dfh-version-delete');
                modal.show();
            });
            versionModal.addEventListener('shown.bs.modal', function () {
                versionModal.querySelector('[data-bs-dismiss="modal"].btn').focus();
            });
        }

        // Copy link.
        document.addEventListener('click', function (e) {
            if (!e.target.closest('[data-dfh-copy-link]')) return;
            const url = window.location.origin + window.location.pathname;
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(url).then(
                    function () { D.showSuccess('Link copied'); },
                    function () { D.showInfo(url); });
            } else {
                D.showInfo(url);
            }
        });

        // A deleted document leaves the editor tabs.
        const deleteForm = document.querySelector('#dfh-delete-modal form');
        if (deleteForm) {
            deleteForm.addEventListener('submit', function () {
                try {
                    const id = deleteForm.querySelector('input[name="documentId"]').value;
                    const list = JSON.parse(sessionStorage.getItem('dfh-open-docs') || '[]')
                        .filter(function (t) { return t.id !== id; });
                    sessionStorage.setItem('dfh-open-docs', JSON.stringify(list));
                } catch (err) { /* storage unavailable */ }
            });
        }
    });

    // 7d · generating: summaries are made during the post, so show the skeleton card meanwhile.
    document.addEventListener('submit', function (e) {
        const form = e.target.closest && e.target.closest('form[data-dfh-ai-generate]');
        if (!form) return;
        setTimeout(function () {
            if (e.defaultPrevented) return;
            const card = document.querySelector('[data-dfh-ai-generating]');
            if (card) card.hidden = false;
            const idle = document.querySelector('[data-dfh-ai-idle]');
            if (idle) idle.hidden = true;
        }, 0);
    });
})();
