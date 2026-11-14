// Admin · Users (design 3e): bulk-bar actions. The checkboxes and the bar itself are documents.js.
// Every action posts the hidden #dfh-users-bulk-form; some ask for a role, a message or a confirmation first.
(function () {
    'use strict';

    const ACTIONS = {
        'assign-role': { operation: 'AssignRole', title: 'Assign a role', button: 'Assign role', fields: ['role'],
            text: function (n) { return 'Give a role to ' + n + '. Their other roles stay.'; } },
        message: { operation: 'SendMessage', title: 'Send a message', button: 'Send', fields: ['subject', 'message'],
            text: function (n) { return 'The message is saved for ' + n + '. Users have no inbox and no e-mail is sent yet, so they will not see it.'; } },
        Delete: { operation: 'Delete', title: 'Delete users?', button: 'Delete', danger: true, fields: [],
            text: function (n) { return 'Delete ' + n + ' and their data. Your own account is left out. It can\'t be undone.'; } },
        lock: { operation: 'LockAccount', title: 'Lock users?', button: 'Lock', danger: true, fields: [],
            text: function (n) { return 'Lock ' + n + '. They can\'t sign in until an admin unlocks them. Your own account is left out.'; } }
    };

    document.addEventListener('DOMContentLoaded', function () {
        const form = document.getElementById('dfh-users-bulk-form');
        const dialogEl = document.getElementById('dfh-users-dialog');
        if (!form || !dialogEl || !window.bootstrap) return;
        const dialog = bootstrap.Modal.getOrCreateInstance(dialogEl);
        let pending = null;

        function checkedIds() {
            return Array.from(document.querySelectorAll('.dfh-row-check:checked')).map(function (c) { return c.value; });
        }

        function submit(operation, ids, extra) {
            form.querySelectorAll('[data-dfh-dynamic]').forEach(function (el) { el.remove(); });
            form.querySelector('input[name="operation"]').value = operation;
            function add(name, value) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = name;
                input.value = value;
                input.setAttribute('data-dfh-dynamic', '');
                form.appendChild(input);
            }
            ids.forEach(function (id) { add('userIds', id); });
            Object.keys(extra || {}).forEach(function (key) { add(key, extra[key]); });
            form.submit();
        }

        document.addEventListener('click', function (e) {
            const button = e.target.closest('[data-dfh-users-bulk]');
            if (!button) return;
            const ids = checkedIds();
            if (ids.length === 0) return;
            const kind = button.getAttribute('data-dfh-users-bulk');
            const spec = ACTIONS[kind];
            if (!spec) {
                submit(kind, ids);
                return;
            }
            const who = ids.length === 1 ? '1 user' : ids.length + ' users';
            pending = { spec: spec, ids: ids };
            dialogEl.querySelector('#dfh-users-dialog-title').textContent = spec.title;
            dialogEl.querySelector('[data-dfh-users-text]').textContent = spec.text(who);
            dialogEl.querySelectorAll('[data-dfh-users-field]').forEach(function (f) {
                f.hidden = spec.fields.indexOf(f.getAttribute('data-dfh-users-field')) < 0;
            });
            const ok = dialogEl.querySelector('[data-dfh-users-ok]');
            ok.textContent = spec.button;
            ok.className = 'btn ' + (spec.danger ? 'btn-danger' : 'btn-primary');
            dialog.show();
        });

        dialogEl.querySelector('[data-dfh-users-ok]').addEventListener('click', function () {
            if (!pending) return;
            const extra = {};
            for (const name of pending.spec.fields) {
                const input = dialogEl.querySelector('[data-dfh-users-input="' + name + '"]');
                if (!input.value.trim()) {
                    input.focus();
                    return;
                }
                extra[name] = input.value.trim();
            }
            dialog.hide();
            submit(pending.spec.operation, pending.ids, extra);
        });
    });
})();
