// Shell behaviour for the IDE redesign: toasts, Ctrl K, opt-in busy forms, server messages.
(function () {
    'use strict';

    // ---------- Toasts (design 7f): max 3, success/info/AI hide after 5s, errors stay ----------
    const MAX_TOASTS = 3;
    const AUTO_HIDE_MS = 5000;
    const ICONS = {
        success: 'bi-check-circle',
        error: 'bi-exclamation-octagon',
        info: 'bi-info-circle',
        ai: 'bi-stars'
    };

    function stack() {
        let el = document.getElementById('dfh-toast-stack');
        if (!el) {
            el = document.createElement('div');
            el.id = 'dfh-toast-stack';
            el.className = 'dfh-toast-stack';
            document.body.appendChild(el);
        }
        return el;
    }

    function showToast(type, message, options) {
        options = options || {};
        const container = stack();
        while (container.children.length >= MAX_TOASTS) {
            container.removeChild(container.firstElementChild);
        }

        const toast = document.createElement('div');
        toast.className = 'dfh-toast dfh-toast--' + type;
        toast.setAttribute('role', type === 'error' ? 'alert' : 'status');

        const icon = document.createElement('i');
        icon.className = 'bi ' + (ICONS[type] || ICONS.info) + ' dfh-toast-icon';
        icon.setAttribute('aria-hidden', 'true');

        const body = document.createElement('div');
        body.className = 'dfh-toast-body';
        const title = document.createElement('span');
        title.textContent = message;
        body.appendChild(title);
        if (options.meta) {
            const meta = document.createElement('span');
            meta.className = 'dfh-toast-meta';
            meta.textContent = options.meta;
            body.appendChild(meta);
        }

        const action = document.createElement('span');
        if (options.actionText && options.actionHref) {
            const link = document.createElement('a');
            link.className = 'dfh-toast-action';
            link.href = options.actionHref;
            link.textContent = options.actionText;
            action.appendChild(link);
        }

        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'dfh-toast-close';
        close.setAttribute('aria-label', 'Dismiss');
        close.innerHTML = '<i class="bi bi-x" aria-hidden="true"></i>';
        close.addEventListener('click', function () { toast.remove(); });

        toast.append(icon, body, action, close);
        container.appendChild(toast);

        if (type !== 'error') {
            setTimeout(function () { toast.remove(); }, AUTO_HIDE_MS);
        }
        return toast;
    }

    // Keep the API that existing pages already call.
    window.DocFlowHub = Object.assign(window.DocFlowHub || {}, {
        toast: showToast,
        showSuccess: function (message, options) { return showToast('success', message, options); },
        showError: function (message, options) { return showToast('error', message, options); },
        showInfo: function (message, options) { return showToast('info', message, options); },
        showAi: function (message, options) { return showToast('ai', message, options); },
        // Spinner overlays are gone in the redesign; kept as no-ops for old callers.
        showLoading: function () { document.body.setAttribute('aria-busy', 'true'); },
        hideLoading: function () { document.body.removeAttribute('aria-busy'); },
        csrfToken: function () {
            const meta = document.querySelector('meta[name="csrf-token"]');
            return meta ? meta.getAttribute('content') : '';
        },
        postJson: postJson,
        reportFailure: reportFailure,
        toastAfterReload: toastAfterReload
    });

    /**
     * POSTs JSON to a page handler with the antiforgery token. Always resolves to an object with
     * `success`; failures carry `message` and `status` (403 from the demo filter: `readOnly`).
     */
    async function postJson(url, body) {
        let response;
        try {
            response = await fetch(url, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': window.DocFlowHub.csrfToken()
                },
                body: JSON.stringify(body)
            });
        } catch (e) {
            return { success: false, message: 'The server could not be reached. Check your connection and try again.' };
        }

        let data = null;
        try { data = await response.json(); } catch (e) { /* not JSON */ }

        if (!response.ok || !data) {
            const message = data && (data.message || data.error);
            return {
                success: false,
                status: response.status,
                readOnly: response.status === 403 && !!message,
                message: message || (response.status === 403
                    ? 'You do not have permission to do this.'
                    : 'Something went wrong (HTTP ' + response.status + '). Try again.')
            };
        }
        if (data.success === false && !data.message) data.message = data.error || 'The request failed.';
        return data;
    }

    /** Shows a failed postJson result: the demo's read-only notice as info, anything else as an error. */
    function reportFailure(result, fallback) {
        const message = (result && result.message) || fallback || 'The request failed.';
        if (result && result.readOnly) showToast('info', message);
        else showToast('error', message);
    }

    /** Queues a toast for the next page load (e.g. after a list reload). */
    function toastAfterReload(type, message, options) {
        try {
            const queue = JSON.parse(sessionStorage.getItem('dfh-toasts') || '[]');
            queue.push({ type: type, message: message, options: options || {} });
            sessionStorage.setItem('dfh-toasts', JSON.stringify(queue.slice(-3)));
        } catch (e) { /* storage unavailable: the toast is skipped */ }
    }

    document.addEventListener('DOMContentLoaded', function () {
        // Messages the server queued for this page (e.g. the demo read-only notice).
        const queued = document.getElementById('dfh-server-toasts');
        if (queued) {
            try {
                JSON.parse(queued.textContent).forEach(function (t) { showToast(t.type, t.message); });
            } catch (e) { /* ignore malformed payload */ }
        }
        // Messages a script queued before reloading the page.
        try {
            const pending = JSON.parse(sessionStorage.getItem('dfh-toasts') || '[]');
            sessionStorage.removeItem('dfh-toasts');
            pending.forEach(function (t) { showToast(t.type, t.message, t.options); });
        } catch (e) { /* storage unavailable */ }

        // Opt-in double-submit protection: <form data-dfh-busy="Saving…">.
        document.querySelectorAll('form[data-dfh-busy]').forEach(function (form) {
            form.addEventListener('submit', function (event) {
                const button = form.querySelector('button[type="submit"], input[type="submit"]');
                if (!button || button.disabled) return;
                setTimeout(function () {
                    // A page script cancelled the submit (e.g. validation): leave the button usable.
                    if (event.defaultPrevented) return;
                    button.disabled = true;
                    const label = form.getAttribute('data-dfh-busy') || 'Working…';
                    if (button.tagName === 'INPUT') button.value = label; else button.textContent = label;
                }, 0);
            });
        });

        // Tooltips on disabled demo controls.
        if (window.bootstrap && bootstrap.Tooltip) {
            document.querySelectorAll('[data-dfh-tooltip]').forEach(function (el) {
                new bootstrap.Tooltip(el, { title: el.getAttribute('data-dfh-tooltip') });
            });
        }
    });

    // Table rows with data-dfh-href open their page; links and buttons inside the row keep their own action.
    document.addEventListener('click', function (e) {
        const row = e.target.closest('tr[data-dfh-href]');
        if (!row || e.target.closest('a, button, input, label, select')) return;
        if (e.ctrlKey || e.metaKey) {
            window.open(row.getAttribute('data-dfh-href'), '_blank');
        } else {
            window.location.href = row.getAttribute('data-dfh-href');
        }
    });

    // Ctrl K / Cmd K: jump to the global search (the command palette arrives in Sprint 15).
    document.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
            const search = document.getElementById('dfh-global-search');
            if (search) {
                e.preventDefault();
                search.focus();
                search.select();
            }
        }
    });
})();
