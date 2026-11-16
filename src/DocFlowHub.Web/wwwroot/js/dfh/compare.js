// Version compare (design 3i): picking another version is a plain GET of the same page.
(function () {
    'use strict';
    document.querySelectorAll('[data-dfh-compare-pickers] select').forEach(function (select) {
        select.addEventListener('change', function () { select.form.submit(); });
    });

    // Line diff (Sprint 21): "change 2 of 5" and jumps between changes with the buttons or N / P.
    var diff = document.querySelector('[data-dfh-diff]');
    var changes = diff ? Array.prototype.slice.call(diff.querySelectorAll('[data-dfh-change]')) : [];
    if (!changes.length) return;

    var position = document.querySelector('[data-dfh-diff-pos]');
    var prev = document.querySelector('[data-dfh-diff-prev]');
    var next = document.querySelector('[data-dfh-diff-next]');
    var current = -1;

    function show(index, scroll) {
        if (current >= 0) changes[current].classList.remove('is-current');
        current = (index + changes.length) % changes.length;
        var change = changes[current];
        change.classList.add('is-current');
        if (position) position.textContent = 'change ' + (current + 1) + ' of ' + changes.length;
        if (scroll) {
            var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            change.scrollIntoView({ block: 'center', behavior: reduce ? 'auto' : 'smooth' });
        }
    }

    [[prev, -1], [next, 1]].forEach(function (pair) {
        if (!pair[0]) return;
        pair[0].hidden = false;
        pair[0].addEventListener('click', function () { show(current + pair[1], true); });
    });
    document.addEventListener('keydown', function (e) {
        if (e.ctrlKey || e.metaKey || e.altKey) return;
        var target = e.target;
        if (target && (target.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName))) return;
        if (document.querySelector('.modal.show')) return;
        if (e.key === 'n' || e.key === 'N') show(current + 1, true);
        else if (e.key === 'p' || e.key === 'P') show(current - 1, true);
    });

    // Start at the change the address points to (#change-3), otherwise at the first one without scrolling.
    var hash = /^#change-(\d+)$/.exec(window.location.hash);
    var start = hash ? Number(hash[1]) - 1 : 0;
    show(start >= 0 && start < changes.length ? start : 0, !!hash);
})();
