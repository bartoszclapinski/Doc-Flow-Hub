// Context menu (design 7j, S15-2): one menu at a time, opened at the pointer (right click) or under a
// button (⋯, Shift F10). Styled like the Bootstrap dropdowns. Items:
//   { label, icon, hint, href, run, danger, ai, disabled, title } or { separator: true }
(function () {
    'use strict';

    let menu = null;
    let returnFocus = null;

    function close(restoreFocus) {
        if (!menu) return;
        menu.remove();
        menu = null;
        document.removeEventListener('mousedown', outside, true);
        window.removeEventListener('resize', closeQuiet);
        window.removeEventListener('blur', closeQuiet);
        document.removeEventListener('scroll', closeQuiet, true);
        if (restoreFocus && returnFocus && returnFocus.focus) returnFocus.focus();
        returnFocus = null;
    }

    function closeQuiet() { close(false); }

    function outside(e) {
        if (menu && !menu.contains(e.target)) close(false);
    }

    function items() {
        return menu ? Array.prototype.slice.call(menu.querySelectorAll('.dropdown-item:not(.disabled)')) : [];
    }

    function open(entries, position, label) {
        close(false);
        returnFocus = document.activeElement;
        menu = document.createElement('ul');
        menu.className = 'dropdown-menu dfh-menu show';
        menu.setAttribute('role', 'menu');
        if (label) menu.setAttribute('aria-label', label);

        entries.forEach(function (entry) {
            const li = document.createElement('li');
            li.setAttribute('role', 'none');
            if (entry.separator) {
                const hr = document.createElement('hr');
                hr.className = 'dropdown-divider';
                li.appendChild(hr);
                menu.appendChild(li);
                return;
            }
            const el = document.createElement(entry.href && !entry.disabled ? 'a' : 'button');
            el.className = 'dropdown-item' + (entry.danger ? ' text-danger' : '') + (entry.ai ? ' is-ai' : '') + (entry.disabled ? ' disabled' : '');
            el.setAttribute('role', 'menuitem');
            el.tabIndex = -1;
            if (el.tagName === 'BUTTON') el.type = 'button';
            if (entry.href && !entry.disabled) el.href = entry.href;
            if (entry.disabled) el.setAttribute('aria-disabled', 'true');
            if (entry.title) el.title = entry.title;
            const icon = document.createElement('i');
            icon.className = 'bi ' + (entry.icon || 'bi-dot');
            icon.setAttribute('aria-hidden', 'true');
            el.appendChild(icon);
            el.appendChild(document.createTextNode(entry.label));
            if (entry.hint) {
                const hint = document.createElement('span');
                hint.className = 'dfh-menu-meta';
                hint.textContent = entry.hint;
                el.appendChild(hint);
            }
            el.addEventListener('click', function (e) {
                if (entry.disabled) { e.preventDefault(); return; }
                if (entry.run) {
                    e.preventDefault();
                    close(false);
                    entry.run();
                } else {
                    close(false);
                }
            });
            li.appendChild(el);
            menu.appendChild(li);
        });

        menu.addEventListener('keydown', function (e) {
            const list = items();
            const index = list.indexOf(document.activeElement);
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                e.preventDefault();
                const step = e.key === 'ArrowDown' ? 1 : -1;
                list[(index + step + list.length) % list.length].focus();
            } else if (e.key === 'Home' || e.key === 'End') {
                e.preventDefault();
                list[e.key === 'Home' ? 0 : list.length - 1].focus();
            } else if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();
                close(true);
            } else if (e.key === 'Tab') {
                close(true);
            }
        });

        document.body.appendChild(menu);
        place(position);
        document.addEventListener('mousedown', outside, true);
        window.addEventListener('resize', closeQuiet);
        window.addEventListener('blur', closeQuiet);
        document.addEventListener('scroll', closeQuiet, true);
        const first = items()[0];
        if (first) first.focus({ preventScroll: true });
    }

    // position: { x, y } (pointer) or { anchor: element } (below its right edge). Kept inside the viewport.
    function place(position) {
        let x = position.x;
        let y = position.y;
        if (position.anchor) {
            const r = position.anchor.getBoundingClientRect();
            x = r.right - menu.offsetWidth;
            y = r.bottom + 2;
        }
        const maxX = window.innerWidth - menu.offsetWidth - 8;
        const maxY = window.innerHeight - menu.offsetHeight - 8;
        if (y > maxY && position.anchor) y = position.anchor.getBoundingClientRect().top - menu.offsetHeight - 2;
        menu.style.left = Math.max(8, Math.min(x, maxX)) + 'px';
        menu.style.top = Math.max(8, Math.min(y, maxY)) + 'px';
    }

    window.DocFlowHub = Object.assign(window.DocFlowHub || {}, {
        menu: { open: open, close: function () { close(true); }, isOpen: function () { return !!menu; } }
    });
})();
