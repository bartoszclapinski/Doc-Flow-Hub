// Command palette (design 7k, S15-1) and global keyboard shortcuts (S15-3).
// Results: documents and folders from /Search?handler=Palette, actions and navigation listed here.
(function () {
    'use strict';

    const root = document.getElementById('dfh-palette');
    if (!root || !window.bootstrap) return;

    const D = window.DocFlowHub;
    const modal = bootstrap.Modal.getOrCreateInstance(root);
    const input = document.getElementById('dfh-palette-q');
    const list = document.getElementById('dfh-palette-list');
    const isAdmin = root.dataset.admin === 'true';
    const isDemo = root.dataset.demo === 'true';
    const shortcuts = document.getElementById('dfh-shortcuts');

    // Navigation and actions. "write" ones are left out in the read-only demo.
    const ACTIONS = [
        { label: 'Upload a document', icon: 'bi-upload', url: '/Documents/Upload', key: 'U', write: true },
        { label: 'New project', icon: 'bi-kanban', url: '/Projects/Create', write: true },
        { label: 'New team', icon: 'bi-people', url: '/Teams/Create', write: true },
        { label: 'Go to Dashboard', icon: 'bi-grid-1x2', url: '/' },
        { label: 'Go to Documents', icon: 'bi-files', url: '/Documents' },
        { label: 'Go to Projects', icon: 'bi-kanban', url: '/Projects' },
        { label: 'Go to Teams', icon: 'bi-people', url: '/Teams' },
        { label: 'Profile', icon: 'bi-person', url: '/Account/Manage' },
        { label: 'Change password', icon: 'bi-key', url: '/Account/Manage/ChangePassword' },
        { label: 'AI settings', icon: 'bi-stars', url: '/Settings/AI' },
        { label: 'Admin · Overview', icon: 'bi-shield-lock', url: '/Admin', admin: true },
        { label: 'Admin · Users', icon: 'bi-people', url: '/Admin/Users', admin: true },
        { label: 'Admin · Analytics', icon: 'bi-graph-up', url: '/Admin/Analytics', admin: true },
        { label: 'Keyboard shortcuts', icon: 'bi-keyboard', run: showShortcuts, key: '?' }
    ].filter(function (a) { return (!a.write || !isDemo) && (!a.admin || isAdmin); });

    let returnFocus = null;
    let timer = null;
    let controller = null;
    let options = [];
    let active = -1;
    let pendingEnter = null;

    // ---------- Open / close ----------
    function open(text) {
        returnFocus = document.activeElement;
        input.value = text || '';
        modal.show();
        search();
    }

    root.addEventListener('shown.bs.modal', function () {
        input.focus();
        input.setSelectionRange(input.value.length, input.value.length);
    });
    root.addEventListener('hidden.bs.modal', function () {
        if (controller) controller.abort();
        loading = false;
        pendingEnter = null;
        if (returnFocus && returnFocus.focus && returnFocus.id !== 'dfh-global-search') returnFocus.focus();
    });

    // ---------- Search ----------
    function tokens(q) {
        return q.toLowerCase().split(/\s+/).filter(Boolean);
    }

    function matches(text, words) {
        const lower = text.toLowerCase();
        return words.every(function (w) { return lower.indexOf(w) >= 0; });
    }

    input.addEventListener('input', function () {
        clearTimeout(timer);
        timer = setTimeout(search, 150);
    });

    let loading = false;
    function search() {
        timer = null;
        loading = true;
        const q = input.value.trim();
        const words = tokens(q);
        if (controller) controller.abort();
        controller = new AbortController();
        const signal = controller.signal;
        render(q, words, null, true);
        fetch('/Search?handler=Palette&q=' + encodeURIComponent(q), { headers: { 'Accept': 'application/json' }, signal: signal })
            .then(function (r) { return r.ok ? r.json() : Promise.reject(new Error('HTTP ' + r.status)); })
            .then(function (data) { if (!signal.aborted) done(q, words, data); })
            .catch(function (e) {
                if (e.name === 'AbortError') return;
                done(q, words, { documents: [], places: [], failed: true });
            });
    }

    function done(q, words, data) {
        loading = false;
        render(q, words, data, false);
        if (pendingEnter) {
            const newTab = pendingEnter.newTab;
            pendingEnter = null;
            choose(active, newTab);
        }
    }

    // ---------- Rendering ----------
    function render(q, words, data, loading) {
        list.textContent = '';
        options = [];

        if (data) {
            if (data.documents.length) {
                group('Documents');
                data.documents.forEach(function (d) {
                    addOption({
                        url: d.url,
                        lead: label(d.type, d.kind),
                        text: d.title,
                        meta: d.meta
                    }, words);
                });
            }
            if (data.places.length) {
                group('Folders and projects');
                data.places.forEach(function (p) {
                    addOption({
                        url: p.url,
                        lead: icon(p.kind === 'project' ? 'bi-kanban' : 'bi-folder2'),
                        text: p.path,
                        meta: p.count + (p.count === 1 ? ' doc' : ' docs')
                    }, words);
                });
            }
        }

        const actions = ACTIONS.filter(function (a) { return words.length === 0 || matches(a.label, words); });
        const top = data && data.places.length && words.length ? data.places[0] : null;
        if (q || actions.length || top) group('Actions');
        if (q) {
            addOption({ url: '/Documents?q=' + encodeURIComponent(q), lead: icon('bi-search'), text: 'Search all documents for “' + q + '”' }, []);
        }
        if (top && !isDemo) {
            addOption({
                url: '/Documents/Upload?projectId=' + top.projectId + (top.folderId ? '&folderId=' + top.folderId : ''),
                lead: icon('bi-upload'),
                text: 'Upload to ' + top.path
            }, words);
        }
        actions.forEach(function (a) {
            addOption({ url: a.url, run: a.run, lead: icon(a.icon), text: a.label, key: a.key }, words);
        });

        if (loading && !q) {
            // First paint for an empty query: actions only, folders follow.
        } else if (data && data.failed) {
            const note = document.createElement('div');
            note.className = 'dfh-palette-note';
            note.textContent = 'Documents and folders could not be searched. Actions still work.';
            list.insertBefore(note, list.firstChild);
        } else if (data && q && !data.documents.length && !data.places.length) {
            const note = document.createElement('div');
            note.className = 'dfh-palette-note';
            note.textContent = 'No documents or folders match “' + q + '”.';
            list.insertBefore(note, list.firstChild);
        }

        setActive(0);
    }

    function group(title) {
        const head = document.createElement('div');
        head.className = 'dfh-palette-group';
        head.setAttribute('role', 'presentation');
        head.textContent = title;
        list.appendChild(head);
    }

    function addOption(o, words) {
        const row = document.createElement('a');
        row.className = 'dfh-palette-row';
        row.id = 'dfh-palette-opt-' + options.length;
        row.setAttribute('role', 'option');
        row.setAttribute('aria-selected', 'false');
        row.href = o.url || '#';
        row.tabIndex = -1;
        row.appendChild(o.lead);
        const text = document.createElement('span');
        text.className = 'dfh-palette-text';
        highlight(text, o.text, words);
        row.appendChild(text);
        if (o.meta || o.key) {
            const meta = document.createElement(o.key ? 'kbd' : 'span');
            meta.className = o.key ? 'dfh-kbd' : 'dfh-palette-meta';
            meta.textContent = o.key || o.meta;
            row.appendChild(meta);
        }
        const index = options.length;
        row.addEventListener('mousemove', function () { if (active !== index) setActive(index); });
        row.addEventListener('click', function (e) {
            e.preventDefault();
            choose(index, e.ctrlKey || e.metaKey);
        });
        list.appendChild(row);
        options.push({ el: row, url: o.url, run: o.run });
    }

    function label(type, kind) {
        const span = document.createElement('span');
        span.className = 'dfh-filetype dfh-filetype--' + kind;
        span.textContent = type;
        return span;
    }

    function icon(name) {
        const i = document.createElement('i');
        i.className = 'bi ' + name + ' dfh-palette-icon';
        i.setAttribute('aria-hidden', 'true');
        return i;
    }

    // Bold the typed words (case-insensitive) without building HTML from the text.
    function highlight(el, text, words) {
        if (!words.length) { el.textContent = text; return; }
        const pattern = new RegExp('(' + words.map(function (w) { return w.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }).join('|') + ')', 'gi');
        let last = 0;
        text.replace(pattern, function (match, _g, offset) {
            if (offset > last) el.appendChild(document.createTextNode(text.slice(last, offset)));
            const b = document.createElement('b');
            b.textContent = match;
            el.appendChild(b);
            last = offset + match.length;
            return match;
        });
        if (last < text.length) el.appendChild(document.createTextNode(text.slice(last)));
    }

    // ---------- Keyboard ----------
    function setActive(index) {
        if (options[active]) {
            options[active].el.classList.remove('is-active');
            options[active].el.setAttribute('aria-selected', 'false');
        }
        active = options.length ? index : -1;
        if (options[active]) {
            const el = options[active].el;
            el.classList.add('is-active');
            el.setAttribute('aria-selected', 'true');
            input.setAttribute('aria-activedescendant', el.id);
            el.scrollIntoView({ block: 'nearest' });
        } else {
            input.removeAttribute('aria-activedescendant');
        }
    }

    function choose(index, newTab) {
        const o = options[index];
        if (!o) return;
        if (o.run) {
            modal.hide();
            o.run();
        } else if (newTab) {
            window.open(o.url, '_blank');
        } else {
            window.location.href = o.url;
        }
    }

    input.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            e.preventDefault();
            if (!options.length) return;
            const step = e.key === 'ArrowDown' ? 1 : -1;
            setActive((active + step + options.length) % options.length);
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (timer || loading) {
                // Enter right after typing: wait for the results of what was typed.
                clearTimeout(timer);
                timer = null;
                pendingEnter = { newTab: e.ctrlKey || e.metaKey };
                if (!loading) search();
                return;
            }
            choose(active, e.ctrlKey || e.metaKey);
        }
    });

    function showShortcuts() {
        if (shortcuts) bootstrap.Modal.getOrCreateInstance(shortcuts).show();
    }

    // ---------- Triggers ----------
    // The title-bar search box opens the palette (it still submits to Documents without JS).
    const trigger = document.getElementById('dfh-global-search');
    if (trigger) {
        trigger.addEventListener('mousedown', function (e) {
            e.preventDefault();
            open(trigger.value);
        });
        trigger.addEventListener('keydown', function (e) {
            if (e.key === 'Tab' || e.key === 'Escape' || e.ctrlKey || e.metaKey || e.altKey) return;
            e.preventDefault();
            open(e.key.length === 1 ? trigger.value + e.key : trigger.value);
        });
    }

    function typing(target) {
        return target && target.closest && target.closest('input, textarea, select, [contenteditable="true"]');
    }

    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && !e.altKey && (e.key === 'k' || e.key === 'K')) {
            e.preventDefault();
            if (root.classList.contains('show')) modal.hide(); else open('');
            return;
        }
        if (e.ctrlKey || e.metaKey || e.altKey || e.defaultPrevented || typing(e.target)) return;
        if (document.querySelector('.modal.show, .dropdown-menu.show, .dfh-menu.show')) return;
        if (e.key === '/') {
            e.preventDefault();
            open('');
        } else if (e.key === '?') {
            e.preventDefault();
            showShortcuts();
        } else if (e.key === 'u' || e.key === 'U') {
            e.preventDefault();
            if (isDemo) {
                D.showInfo('Read-only demo — uploads are disabled.');
            } else {
                window.location.href = '/Documents/Upload';
            }
        }
    });

    D.palette = { open: open };
})();
