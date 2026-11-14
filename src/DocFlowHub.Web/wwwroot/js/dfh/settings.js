// Settings shell (designs 3f / 8e): tree filter, "Unsaved changes" tracking and picture auto-upload.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        // Filter the settings tree as the user types; a group stays while one of its items matches.
        const filter = document.querySelector('[data-dfh-settings-filter]');
        if (filter) {
            filter.addEventListener('input', function () {
                const q = filter.value.trim().toLowerCase();
                document.querySelectorAll('.dfh-settings-tree [data-dfh-settings-item]').forEach(function (item) {
                    item.hidden = q.length > 0 && item.getAttribute('data-dfh-settings-item').indexOf(q) < 0;
                });
                document.querySelectorAll('.dfh-settings-tree .dfh-tree > li').forEach(function (group) {
                    const any = group.querySelector('[data-dfh-settings-item]:not([hidden])');
                    group.hidden = !any;
                    if (q) group.setAttribute('aria-expanded', 'true');
                });
            });
        }

        // Dirty tracking: Save / Discard light up only when something changed.
        document.querySelectorAll('form[data-dfh-dirty-form]').forEach(function (form) {
            const label = document.querySelector('[data-dfh-dirty-label]');
            const buttons = Array.from(document.querySelectorAll('[data-dfh-dirty-enable]'));
            const snapshot = serialize(form);
            // After a failed save the page comes back with the user's values: let them save again.
            const failed = !!form.querySelector('.field-validation-error');

            function update() {
                const dirty = failed || serialize(form) !== snapshot;
                if (label) label.hidden = !dirty;
                buttons.forEach(function (b) { b.disabled = !dirty; });
            }
            form.addEventListener('input', update);
            form.addEventListener('change', update);
            form.addEventListener('reset', function () { setTimeout(update, 0); });
            window.addEventListener('beforeunload', function (e) {
                if (!form.dataset.dfhSubmitting && serialize(form) !== snapshot) {
                    e.preventDefault();
                    e.returnValue = '';
                }
            });
            form.addEventListener('submit', function () { form.dataset.dfhSubmitting = 'yes'; });
            update();
        });

        // Sliders show their value next to them.
        document.querySelectorAll('input[type="range"][data-dfh-range-output]').forEach(function (range) {
            const output = range.parentElement.querySelector('output');
            if (!output) return;
            range.addEventListener('input', function () { output.textContent = Number(range.value).toFixed(1); });
        });

        // Picture: choosing a file uploads it.
        document.querySelectorAll('input[type="file"][data-dfh-autosubmit-file]').forEach(function (input) {
            input.addEventListener('change', function () {
                if (input.files && input.files.length) input.form.submit();
            });
        });
    });

    function serialize(form) {
        const parts = [];
        new FormData(form).forEach(function (value, key) {
            if (key !== '__RequestVerificationToken' && typeof value === 'string') parts.push(key + '=' + value);
        });
        return parts.join('&');
    }
})();
