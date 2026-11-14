// Version compare (design 3i): picking another version is a plain GET of the same page.
(function () {
    'use strict';
    document.querySelectorAll('[data-dfh-compare-pickers] select').forEach(function (select) {
        select.addEventListener('change', function () { select.form.submit(); });
    });
})();
