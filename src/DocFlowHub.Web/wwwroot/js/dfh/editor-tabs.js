// Editor tabs (design 3b/3i, decision S12-1): the documents opened in this browser session, kept in
// sessionStorage. The server renders the active tab; this adds the others and handles ×.
(function () {
    'use strict';

    const KEY = 'dfh-open-docs';
    const MAX = 6;

    function load() {
        try {
            const list = JSON.parse(sessionStorage.getItem(KEY) || '[]');
            return Array.isArray(list) ? list.filter(function (t) { return t && t.id; }) : [];
        } catch (e) { return []; }
    }
    function save(list) {
        try { sessionStorage.setItem(KEY, JSON.stringify(list)); } catch (e) { /* storage unavailable */ }
    }

    function makeTab(t) {
        const tab = document.createElement('div');
        tab.className = 'dfh-editor-tab';
        tab.dataset.tabId = t.id;
        const link = document.createElement('a');
        link.className = 'dfh-editor-tab-link';
        link.href = '/Documents/Details/' + encodeURIComponent(t.id);
        const ext = document.createElement('span');
        ext.className = 'dfh-filetype dfh-filetype--' + (t.kind || 'txt');
        ext.textContent = t.ext || '';
        const title = document.createElement('span');
        title.className = 'dfh-editor-tab-title';
        title.textContent = t.title;
        link.append(ext, title);
        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'dfh-editor-tab-close';
        close.dataset.dfhTabClose = '';
        close.setAttribute('aria-label', 'Close ' + t.title);
        close.innerHTML = '<i class="bi bi-x" aria-hidden="true"></i>';
        tab.append(link, close);
        return tab;
    }

    document.addEventListener('DOMContentLoaded', function () {
        const strip = document.querySelector('[data-dfh-editor-tabs]');
        if (!strip) return;
        const active = strip.querySelector('.dfh-editor-tab.is-active');
        const docId = strip.dataset.docId;
        const auxiliary = strip.dataset.compare === 'true'; // Compare / Edit: a tab beside the document's own

        // Remember this document (newest last), keep at most MAX.
        let list = load();
        const me = { id: docId, title: strip.dataset.title, ext: strip.dataset.ext, kind: strip.dataset.kind };
        const at = list.findIndex(function (t) { return t.id === docId; });
        if (at >= 0) list[at] = me; else list.push(me);
        while (list.length > MAX) list.splice(list[0].id === docId ? 1 : 0, 1);
        save(list);

        // Rebuild the strip in the remembered order.
        list.forEach(function (t) {
            if (t.id === docId && !auxiliary) {
                strip.appendChild(active);
            } else {
                strip.appendChild(makeTab(t));
                if (t.id === docId) strip.appendChild(active); // the Compare/Edit tab sits after its document
            }
        });
        active.scrollIntoView({ block: 'nearest', inline: 'nearest' });

        strip.addEventListener('click', function (e) {
            const close = e.target.closest('[data-dfh-tab-close]');
            if (!close) return;
            e.preventDefault();
            const tab = close.closest('.dfh-editor-tab');
            if (tab === active && auxiliary) {
                window.location.href = '/Documents/Details/' + encodeURIComponent(docId);
                return;
            }
            const id = tab.dataset.tabId;
            const remaining = load();
            const index = remaining.findIndex(function (t) { return t.id === id; });
            if (index >= 0) remaining.splice(index, 1);
            save(remaining);

            const closingCurrent = id === docId;
            if (!closingCurrent) {
                tab.remove();
                return;
            }
            const next = remaining[Math.min(index, remaining.length - 1)];
            window.location.href = next ? '/Documents/Details/' + encodeURIComponent(next.id) : '/Documents';
        });
    });
})();
