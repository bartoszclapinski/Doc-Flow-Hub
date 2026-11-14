// Sign in and Register (designs 3g, 8d): demo click-to-fill and live password rules.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        // Demo box: clicking an account fills the sign-in form.
        document.querySelectorAll('.dfh-demo-fill').forEach(function (button) {
            button.addEventListener('click', function () {
                const email = document.getElementById('Input_Email');
                const password = document.getElementById('Input_Password');
                if (!email || !password) return;
                email.value = button.getAttribute('data-email');
                password.value = button.getAttribute('data-password');
                document.getElementById('login-submit')?.focus();
            });
        });

        // Register: rules, strength meter, and a submit that waits for a valid password.
        const password = document.querySelector('[data-dfh-password]');
        if (!password) return;
        const confirm = document.querySelector('[data-dfh-password-confirm]');
        const form = password.form;
        const submit = form.querySelector('button[type="submit"]');
        const meter = form.querySelector('.dfh-strength');
        const rules = {
            length: function (v) { return v.length >= 8; },
            digit: function (v) { return /\d/.test(v); },
            upper: function (v) { return /[A-Z]/.test(v); },
            symbol: function (v) { return /[^A-Za-z0-9]/.test(v); }
        };

        function update() {
            const value = password.value;
            let met = 0;
            Object.keys(rules).forEach(function (name) {
                const ok = rules[name](value);
                const item = form.querySelector('[data-rule="' + name + '"]');
                item.classList.toggle('is-met', ok);
                item.querySelector('i').className = 'bi ' + (ok ? 'bi-check-lg' : 'bi-dash');
                if (ok) met++;
            });
            meter.setAttribute('data-score', value.length ? String(met) : '0');
            const ready = met === 4 && (!confirm || confirm.value === value);
            if (submit) submit.disabled = !ready; // no submit button in the read-only demo
        }

        password.addEventListener('input', update);
        if (confirm) confirm.addEventListener('input', update);
        update();
    });
})();
