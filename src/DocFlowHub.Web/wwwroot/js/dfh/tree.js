// Project / folder tree (3h, 8b): chevrons expand and collapse. On the project pages a folder row
// can be dragged onto another folder or the project root to move it (Folders/Index MoveFolder).
(function () {
    'use strict';

    const D = window.DocFlowHub;

    document.addEventListener('click', function (e) {
        const toggle = e.target.closest('[data-dfh-tree-toggle]');
        if (!toggle) return;
        const item = toggle.closest('[aria-expanded]');
        if (!item) return;
        item.setAttribute('aria-expanded', item.getAttribute('aria-expanded') === 'true' ? 'false' : 'true');
    });

    document.addEventListener('DOMContentLoaded', function () {
        const tree = document.querySelector('[data-dfh-tree-move]');
        if (!tree) return;
        let dragged = null;

        tree.addEventListener('dragstart', function (e) {
            const row = e.target.closest('[data-dfh-drag-folder]');
            if (!row) return;
            dragged = row;
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', row.dataset.dfhDragFolder);
            row.classList.add('is-dragging');
        });
        tree.addEventListener('dragend', function () {
            if (dragged) dragged.classList.remove('is-dragging');
            dragged = null;
            tree.querySelectorAll('.is-drop-target').forEach(function (el) { el.classList.remove('is-drop-target'); });
        });

        function target(e) {
            const row = e.target.closest('[data-dfh-drop-project]');
            if (!row || !dragged) return null;
            // Same project only, and not onto itself or one of its own subfolders.
            if (row.dataset.dfhDropProject !== dragged.dataset.project) return null;
            if (row === dragged || dragged.closest('li').contains(row)) return null;
            return row;
        }

        tree.addEventListener('dragover', function (e) {
            const row = target(e);
            tree.querySelectorAll('.is-drop-target').forEach(function (el) { if (el !== row) el.classList.remove('is-drop-target'); });
            if (!row) return;
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            row.classList.add('is-drop-target');
        });

        tree.addEventListener('drop', async function (e) {
            const row = target(e);
            if (!row) return;
            e.preventDefault();
            const folderId = Number(dragged.dataset.dfhDragFolder);
            const parent = row.dataset.dfhDropFolder ? Number(row.dataset.dfhDropFolder) : null;
            const name = dragged.querySelector('.dfh-tree-name').textContent;
            const into = row.querySelector('.dfh-tree-name').textContent;
            const result = await D.postJson('/Folders?handler=MoveFolder', { folderId: folderId, newParentFolderId: parent });
            if (!result.success) {
                D.reportFailure(result, 'The folder could not be moved.');
                return;
            }
            D.toastAfterReload('success', 'Moved “' + name + '” into ' + into);
            window.location.reload();
        });
    });
})();
