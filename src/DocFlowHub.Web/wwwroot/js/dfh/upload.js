// Upload and Edit (design 3c): dropzone + file row, title from the file name, project → folder
// filtering and the "Destination" tree. Works on any form with [data-dfh-upload-form].
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
        let autoTitle = title && !title.value;

        function showFile() {
            const file = input.files && input.files[0];
            if (!file) {
                list.hidden = true;
                if (summary) summary.textContent = 'No file chosen';
                if (submit) submit.disabled = true;
                return;
            }
            const dot = file.name.lastIndexOf('.');
            const ext = dot >= 0 ? file.name.slice(dot + 1).toLowerCase() : '';
            const extEl = list.querySelector('[data-dfh-file-ext]');
            extEl.textContent = (ext || 'file').slice(0, 4).toUpperCase();
            extEl.className = 'dfh-filetype dfh-filetype--' + kindOf(ext);
            list.querySelector('[data-dfh-file-name]').textContent = file.name;
            list.querySelector('[data-dfh-file-size]').textContent = formatSize(file.size);

            // The server checks again (size, type and content); this only saves a pointless upload.
            let problem = null;
            if (types.length && types.indexOf('.' + ext) < 0) problem = '.' + ext + ' not allowed';
            else if (maxBytes && file.size > maxBytes) problem = 'over ' + formatSize(maxBytes);
            const status = list.querySelector('[data-dfh-file-status]');
            status.textContent = problem || 'ready';
            status.classList.toggle('is-error', !!problem);
            list.hidden = false;

            if (summary) summary.textContent = '1 file · ' + formatSize(file.size);
            if (submit) submit.disabled = !!problem;
            if (title && autoTitle) title.value = dot > 0 ? file.name.slice(0, dot) : file.name;
        }

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
