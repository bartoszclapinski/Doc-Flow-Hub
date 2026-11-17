// Upload and Edit (design 3c): dropzone + file row, title from the file name, project → folder
// filtering and the "Destination" tree. Works on any form with [data-dfh-upload-form].
// Sprint 23 (Upload only): the "AI on upload" panel (switch, model, detail, estimate), the
// "What will happen" checks for the picked file, and an upload with real progress.
(function () {
    'use strict';

    function formatSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return Math.max(1, Math.round(bytes / 1024)) + ' KB';
        if (bytes < 1024 * 1024 * 1024) return (bytes / 1024 / 1024).toFixed(1).replace(/\.0$/, '') + ' MB';
        return (bytes / 1024 / 1024 / 1024).toFixed(2) + ' GB';
    }

    function kindOf(ext) {
        switch (ext) {
            case 'pdf': return 'pdf';
            case 'md': case 'markdown': return 'md';
            case 'doc': case 'docx': case 'odt': case 'rtf': return 'docx';
            case 'xls': case 'xlsx': case 'csv': case 'ods': return 'xlsx';
            default: return 'txt';
        }
    }

    function extOf(file) {
        if (!file) return '';
        const dot = file.name.lastIndexOf('.');
        return dot >= 0 ? file.name.slice(dot + 1).toLowerCase() : '';
    }

    function isImage(ext) { return ['jpg', 'jpeg', 'png', 'gif'].indexOf(ext) >= 0; }

    // About how many characters of text a file of this size holds (for the estimate only).
    function textChars(file) {
        if (!file) return 2500;
        const ext = extOf(file);
        const ratio = ext === 'pdf' ? 0.3 : ext === 'docx' ? 0.8 : 1;
        return Math.max(200, Math.round(file.size * ratio));
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-dfh-upload-form]').forEach(initForm);
    });

    function initForm(form) {
        const input = form.querySelector('[data-dfh-file]');
        const zone = form.querySelector('[data-dfh-dropzone]');
        const list = form.querySelector('[data-dfh-file-list]');
        const submit = form.querySelector('[data-dfh-upload-submit]');
        const summary = form.querySelector('[data-dfh-upload-summary]');
        const title = form.querySelector('[data-dfh-title]');
        const maxBytes = Number(form.dataset.maxBytes || 0);
        const types = (form.dataset.types || '').split(',').filter(Boolean);
        const ai = initAI(form);
        const checks = form.querySelector('[data-dfh-upload-checks]');
        let autoTitle = title && !title.value;
        let problem = null;

        function current() { return input && input.files && input.files[0]; }

        function showFile() {
            const file = current();
            problem = null;
            if (!file) {
                list.hidden = true;
                if (checks) checks.hidden = true;
                if (summary) summary.textContent = 'No file chosen';
                if (submit) submit.disabled = true;
                ai.update(null);
                return;
            }
            const dot = file.name.lastIndexOf('.');
            const ext = extOf(file);
            const extEl = list.querySelector('[data-dfh-file-ext]');
            extEl.textContent = (ext || 'file').slice(0, 4).toUpperCase();
            extEl.className = 'dfh-filetype dfh-filetype--' + kindOf(ext);
            list.querySelector('[data-dfh-file-name]').textContent = file.name;
            list.querySelector('[data-dfh-file-size]').textContent = formatSize(file.size);

            // The server checks again (size, type and content); this only saves a pointless upload.
            if (types.length && types.indexOf('.' + ext) < 0) problem = '.' + ext + ' not allowed';
            else if (maxBytes && file.size > maxBytes) problem = 'over ' + formatSize(maxBytes);
            const status = list.querySelector('[data-dfh-file-status]');
            status.textContent = problem || 'ready';
            status.classList.toggle('is-error', !!problem);
            list.hidden = false;

            if (summary) summary.textContent = '1 file · ' + formatSize(file.size);
            if (submit) submit.disabled = !!problem;
            if (title && autoTitle) title.value = dot > 0 ? file.name.slice(0, dot) : file.name;
            ai.update(file);
            renderChecks();
        }

        // "What will happen" for the picked file (S23-4).
        function renderChecks() {
            const file = current();
            if (!checks || !file) return;
            const ext = extOf(file);
            const set = function (name, state, text) {
                const li = checks.querySelector('[data-check="' + name + '"]');
                li.className = 'is-' + state;
                li.querySelector('i').className = 'bi ' + (state === 'ok' ? 'bi-check-circle-fill' : state === 'bad' ? 'bi-x-circle-fill' : 'bi-dash-circle');
                li.querySelector('span').textContent = text;
            };
            if (problem) {
                set('file', 'bad', types.indexOf('.' + ext) < 0
                    ? '.' + ext + ' files are not accepted. Allowed: ' + types.map(function (t) { return t.slice(1); }).join(', ') + '.'
                    : 'The file is larger than the ' + formatSize(maxBytes) + ' limit.');
            } else {
                set('file', 'ok', ext.toUpperCase() + ', ' + formatSize(file.size) + ' of the ' + formatSize(maxBytes) + ' allowed. The server checks the content matches the type.');
            }
            if (ext === 'doc') set('text', 'skip', 'Old .doc files can’t be read: no preview and no AI summary. Re-save as .docx to get both.');
            else if (isImage(ext)) set('text', 'skip', 'An image: it is previewed as a picture, but has no text for the AI.');
            else if (ai.summarisable(ext)) set('text', 'ok', 'Its text can be read for the preview and the AI.');
            else set('text', 'skip', 'No text can be read from this type.');
            const plan = ai.plan(file);
            set('ai', plan.state, plan.text);
            checks.hidden = false;
        }
        ai.onChange(renderChecks);

        if (input) {
            input.addEventListener('change', showFile);
            if (title) title.addEventListener('input', function () { autoTitle = !title.value; });

            ['dragenter', 'dragover'].forEach(function (name) {
                zone.addEventListener(name, function (e) {
                    e.preventDefault();
                    zone.classList.add('is-dragover');
                });
            });
            ['dragleave', 'drop'].forEach(function (name) {
                zone.addEventListener(name, function () { zone.classList.remove('is-dragover'); });
            });
            zone.addEventListener('drop', function (e) {
                e.preventDefault();
                if (!e.dataTransfer || !e.dataTransfer.files.length) return;
                const transfer = new DataTransfer();
                transfer.items.add(e.dataTransfer.files[0]);
                input.files = transfer.files;
                showFile();
            });
            form.querySelector('[data-dfh-file-remove]').addEventListener('click', function () {
                input.value = '';
                if (autoTitle && title) title.value = '';
                showFile();
            });
            showFile();
        }

        initDestination(form);
        if (form.getAttribute('data-dfh-upload-progress') === 'on' && window.XMLHttpRequest && window.FormData) {
            initProgress(form, current, ai);
        }
    }

    // ------------------------------------------------------------------ AI panel (S23-2, S23-3)
    function initAI(form) {
        const none = {
            update: function () {}, onChange: function () {},
            summarisable: function () { return false; },
            plan: function () { return { state: 'skip', text: 'AI summaries are turned off in Settings · AI.' }; },
            willSummarise: function () { return false; },
        };
        const on = form.querySelector('[data-dfh-ai-on]');
        const summarisableTypes = (form.dataset.summarisable || '').split(',').filter(Boolean);
        const summarisable = function (ext) { return summarisableTypes.indexOf('.' + ext) >= 0; };
        if (!on) {
            none.summarisable = summarisable;
            return none;
        }

        const models = JSON.parse(form.dataset.models || '[]');
        const outputTokens = JSON.parse(form.dataset.details || '{}');
        const model = form.querySelector('[data-dfh-ai-model]');
        const options = form.querySelector('[data-dfh-ai-options]');
        const reason = form.querySelector('[data-dfh-ai-reason]');
        const strengths = form.querySelector('[data-dfh-ai-strengths]');
        const detailNote = form.querySelector('[data-dfh-ai-detail]');
        const est = {
            cost: form.querySelector('[data-dfh-est-cost]'),
            time: form.querySelector('[data-dfh-est-time]'),
            tokens: form.querySelector('[data-dfh-est-tokens]'),
            note: form.querySelector('[data-dfh-est-note]'),
        };
        const defaultReason = reason ? reason.textContent : '';
        const listeners = [];
        let wanted = on.checked;   // the user's own choice, kept while a file blocks the summary
        let file = null;

        function detail() {
            const checked = form.querySelector('input[name="SummaryDetail"]:checked');
            return checked ? checked : null;
        }

        function estimate() {
            const m = models.find(function (x) { return x.id === model.value; }) || models[0];
            const d = detail();
            const name = d ? d.value : 'Standard';
            const input = Math.round(textChars(file) / 4) + 200;
            const output = outputTokens[name] || 350;
            const cost = (input * m.input + output * m.output) / 1e6;
            const factor = name === 'Short' ? 0.7 : name === 'Detailed' ? 1.6 : 1;
            const seconds = Math.max(2, Math.round(m.seconds * factor + input / 4000));
            return {
                model: m,
                detail: name,
                cost: cost < 0.01 ? '< $0.01' : '≈ $' + cost.toFixed(2),
                time: seconds < 90 ? '≈ ' + seconds + ' s' : '≈ ' + (seconds / 60).toFixed(1) + ' min',
                tokens: '≈ ' + (input + output).toLocaleString('en-US'),
            };
        }

        function blockedReason() {
            if (!file) return null;
            const ext = extOf(file);
            if (summarisable(ext)) return null;
            if (ext === 'doc') return 'Old .doc files can’t be read, so there is nothing to summarise. Re-save the file as .docx to get a summary.';
            if (isImage(ext)) return 'Images have no text to summarise.';
            return 'This file type has no text to summarise.';
        }

        function render() {
            const blocked = blockedReason();
            on.disabled = !!blocked;
            on.checked = blocked ? false : wanted;
            if (reason) reason.textContent = blocked || defaultReason;
            options.classList.toggle('is-muted', !on.checked);
            const e = estimate();
            if (strengths) strengths.textContent = e.model.strengths;
            const d = detail();
            if (detailNote && d) detailNote.textContent = d.getAttribute('data-describe');
            est.cost.textContent = e.cost;
            est.time.textContent = e.time;
            est.tokens.textContent = e.tokens;
            est.note.textContent = file
                ? 'For this file (' + formatSize(file.size) + '), estimated from its size.'
                : 'For a typical one-page document. Pick a file for a closer estimate.';
            listeners.forEach(function (fn) { fn(); });
        }

        on.addEventListener('change', function () { wanted = on.checked; render(); });
        model.addEventListener('change', render);
        form.querySelectorAll('input[name="SummaryDetail"]').forEach(function (r) { r.addEventListener('change', render); });

        return {
            update: function (f) { file = f; render(); },
            onChange: function (fn) { listeners.push(fn); },
            summarisable: summarisable,
            willSummarise: function () { return on.checked && !on.disabled; },
            plan: function () {
                if (blockedReason()) return { state: 'skip', text: 'No AI summary for this file.' };
                if (!on.checked) return { state: 'skip', text: 'No AI summary: the switch is off.' };
                const e = estimate();
                const label = model.options[model.selectedIndex].textContent.replace(/\s*★$/, '');
                return { state: 'ok', text: 'An AI summary with ' + label + ', ' + e.detail.toLowerCase() + ': ' + e.cost + ', ' + e.time + '.' };
            },
        };
    }

    // ------------------------------------------------------------------ progress (S23-5)
    function initProgress(form, current, ai) {
        const box = form.querySelector('[data-dfh-progress]');
        const bar = form.querySelector('[data-dfh-progress-bar]');
        const pct = form.querySelector('[data-dfh-progress-pct]');
        const aiText = form.querySelector('[data-dfh-progress-ai]');
        const submit = form.querySelector('[data-dfh-upload-submit]');
        if (!box || !submit) return;
        const label = submit.innerHTML;

        function step(name, state) {
            const li = box.querySelector('[data-step="' + name + '"]');
            if (li) li.className = state ? 'is-' + state : '';
        }

        function fail(message) {
            box.hidden = true;
            submit.disabled = false;
            submit.innerHTML = label;
            let error = form.querySelector('.dfh-form-error');
            if (!error) {
                error = document.createElement('div');
                error.className = 'dfh-form-error dfh-upload-error';
                error.setAttribute('role', 'alert');
                error.innerHTML = '<i class="bi bi-exclamation-octagon" aria-hidden="true"></i><span></span>';
                form.querySelector('.dfh-form-body').prepend(error);
                form.querySelector('.dfh-form-body').classList.add('has-error');
            }
            error.querySelector('span').textContent = message;
            error.scrollIntoView({ block: 'nearest' });
        }

        form.addEventListener('submit', function (e) {
            if (e.defaultPrevented || !current()) return;
            e.preventDefault();
            const summarise = ai.willSummarise();
            box.hidden = false;
            bar.style.width = '0%';
            pct.textContent = '0%';
            step('upload', 'active');
            step('save', '');
            step('ai', summarise ? '' : 'skipped');
            aiText.textContent = summarise ? 'continues in the background' : 'not requested';
            submit.disabled = true;
            submit.textContent = 'Uploading…';

            const xhr = new XMLHttpRequest();
            xhr.open('POST', form.getAttribute('action') || window.location.href);
            xhr.setRequestHeader('X-DFH-Upload', '1');
            xhr.responseType = 'json';
            xhr.upload.addEventListener('progress', function (ev) {
                if (!ev.lengthComputable) return;
                const value = Math.min(100, Math.round(ev.loaded / ev.total * 100));
                bar.style.width = value + '%';
                pct.textContent = value + '%';
            });
            xhr.upload.addEventListener('load', function () {
                bar.style.width = '100%';
                pct.textContent = '100%';
                step('upload', 'done');
                step('save', 'active');
            });
            xhr.addEventListener('load', function () {
                const body = xhr.response || {};
                if (xhr.status === 200 && body.url) {
                    step('save', 'done');
                    if (summarise) step('ai', 'queued');
                    window.location.assign(body.url);
                    return;
                }
                fail(body.error || 'The file could not be uploaded. Try again.');
            });
            xhr.addEventListener('error', function () { fail('The upload was interrupted. Check the connection and try again.'); });
            xhr.send(new FormData(form));
        });
    }

    // Project → folder options, and the Destination tree in the inspector (both directions).
    function initDestination(form) {
        const project = form.querySelector('[data-dfh-project]');
        const folder = form.querySelector('[data-dfh-folder]');
        const tree = document.querySelector('[data-dfh-destination]');
        if (!project || !folder) return;

        function filterFolders() {
            Array.from(folder.options).forEach(function (o) {
                if (!o.value) return;
                const visible = o.dataset.project === project.value;
                o.hidden = !visible;
                o.disabled = !visible;
            });
            if (folder.selectedOptions[0] && folder.selectedOptions[0].disabled) folder.value = '';
            folder.disabled = !project.value;
        }

        function markTree() {
            if (!tree) return;
            tree.querySelectorAll('.dfh-picker-row').forEach(function (row) {
                const on = row.dataset.project === project.value && row.dataset.folder === (folder.value || '');
                row.classList.toggle('is-selected', on);
                row.setAttribute('aria-pressed', on ? 'true' : 'false');
                const count = row.querySelector('.dfh-tree-count');
                if (count && row.dataset.count !== undefined) {
                    const n = Number(row.dataset.count);
                    count.textContent = on ? n + ' → ' + (n + 1) : String(n);
                }
                const icon = row.querySelector('i.bi-folder2, i.bi-folder2-open');
                if (icon) icon.className = 'bi ' + (on ? 'bi-folder2-open' : 'bi-folder2') + ' dfh-tree-icon';
            });
        }

        project.addEventListener('change', function () { filterFolders(); markTree(); });
        folder.addEventListener('change', markTree);
        if (tree) {
            tree.addEventListener('click', function (e) {
                const row = e.target.closest('.dfh-picker-row');
                if (!row) return;
                project.value = row.dataset.project;
                filterFolders();
                folder.value = row.dataset.folder;
                markTree();
            });
        }
        filterFolders();
        markTree();
    }
})();
