// PDF pages on the Details page (Sprint 19, design 3b). PDF.js is self-hosted under /lib/pdfjs/ and
// loaded only here. Pages scroll in one column and render when they come near the view; a strip of
// thumbnails, page navigation and zoom sit next to them. When PDF.js cannot open the file, the pane
// says so and links to the text view.

const ZOOM_STEPS = [0.5, 0.75, 1, 1.25, 1.5, 2, 3];
const FIT_MAX = 1.5;
const FIT_MIN = 0.25;
const MAX_CANVAS_PIXELS = 16777216; // 4096 x 4096
const THUMB_WIDTH = 48;
const PHONE = window.matchMedia('(max-width: 599.98px)');

const root = document.querySelector('[data-dfh-pdf]');
if (root) {
    start(root).catch(function (err) {
        console.error('PDF preview failed', err);
    });
}

async function start(root) {
    const lib = new URL(root.dataset.lib, window.location.href).href;
    const pagesEl = root.querySelector('.dfh-pdf-pages');
    const thumbsEl = root.querySelector('.dfh-pdf-thumbs');
    const controls = document.querySelector('[data-dfh-pdf-controls]');
    const ui = controls && {
        prev: controls.querySelector('[data-dfh-pdf-prev]'),
        next: controls.querySelector('[data-dfh-pdf-next]'),
        page: controls.querySelector('[data-dfh-pdf-page]'),
        count: controls.querySelector('[data-dfh-pdf-count]'),
        zoomOut: controls.querySelector('[data-dfh-pdf-zoom-out]'),
        zoomIn: controls.querySelector('[data-dfh-pdf-zoom-in]'),
        zoom: controls.querySelector('[data-dfh-pdf-zoom]'),
        fit: controls.querySelector('[data-dfh-pdf-fit]'),
    };

    const status = document.createElement('p');
    status.className = 'dfh-pdf-status';
    status.setAttribute('role', 'status');
    status.textContent = 'Loading pages…';
    pagesEl.appendChild(status);

    function fail(message) {
        root.dataset.state = 'error';
        status.textContent = message + ' ';
        const link = document.createElement('a');
        link.href = root.dataset.textUrl;
        link.textContent = 'Show the text';
        status.appendChild(link);
        status.appendChild(document.createTextNode(' or download the file.'));
        if (controls) controls.hidden = true;
    }

    let pdfjs;
    try {
        pdfjs = await import(lib + 'build/pdf.min.mjs');
    } catch (err) {
        console.error(err);
        fail('The PDF viewer could not be loaded in this browser.');
        return;
    }
    pdfjs.GlobalWorkerOptions.workerSrc = lib + 'build/pdf.worker.min.mjs';
    const CSS_UNITS = pdfjs.PixelsPerInch.PDF_TO_CSS_UNITS;

    let pdf;
    try {
        pdf = await pdfjs.getDocument({
            url: root.dataset.src,
            cMapUrl: lib + 'cmaps/',
            cMapPacked: true,
            standardFontDataUrl: lib + 'standard_fonts/',
            wasmUrl: lib + 'wasm/',
            iccUrl: lib + 'iccs/',
            isEvalSupported: false,
            enableXfa: false,
        }).promise;
    } catch (err) {
        console.error(err);
        fail(err && err.name === 'PasswordException'
            ? 'This PDF is password-protected, so it can\'t be shown here.'
            : 'This PDF could not be shown here (the file may be damaged).');
        return;
    }

    // Page sizes up front, so the scrollbar is right before anything is drawn.
    const pages = [];
    for (let n = 1; n <= pdf.numPages; n++) {
        const page = await pdf.getPage(n);
        const box = page.getViewport({ scale: 1 });
        pages.push({ n: n, page: page, width: box.width, height: box.height, userUnit: page.userUnit || 1 });
    }

    const state = { scale: 1, fit: true, current: 1 };
    // Resolved when the first page has been drawn (or failed), so the thumbnails start after it.
    let firstDrawn;
    const firstDraw = new Promise(function (resolve) { firstDrawn = resolve; });
    status.remove();
    root.dataset.pages = String(pages.length);

    pages.forEach(function (pg) {
        const el = document.createElement('div');
        el.className = 'dfh-pdf-page';
        el.dataset.page = String(pg.n);
        el.setAttribute('role', 'group');
        el.setAttribute('aria-label', 'Page ' + pg.n + ' of ' + pages.length);
        el.style.setProperty('--user-unit', String(pg.userUnit));
        pagesEl.appendChild(el);
        pg.el = el;
    });

    // ---------- Zoom ----------

    function fitScale() {
        const style = getComputedStyle(pagesEl);
        const room = pagesEl.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
        const widest = Math.max.apply(null, pages.map(function (pg) { return pg.width; }));
        return Math.min(FIT_MAX, Math.max(FIT_MIN, room / (widest * CSS_UNITS)));
    }

    function layout() {
        const factor = state.scale * CSS_UNITS;
        pages.forEach(function (pg) {
            pg.el.style.width = Math.floor(pg.width * factor) + 'px';
            pg.el.style.height = Math.floor(pg.height * factor) + 'px';
            pg.el.style.setProperty('--scale-factor', String(factor));
        });
        root.dataset.scale = state.scale.toFixed(2);
        if (ui) {
            ui.zoom.textContent = Math.round(state.scale * 100) + '%';
            ui.zoomOut.disabled = state.scale <= ZOOM_STEPS[0] + 0.001;
            ui.zoomIn.disabled = state.scale >= ZOOM_STEPS[ZOOM_STEPS.length - 1] - 0.001;
            ui.fit.setAttribute('aria-pressed', state.fit ? 'true' : 'false');
        }
    }

    function setScale(scale, fit) {
        scale = Math.round(scale * 1000) / 1000;
        if (scale === state.scale && fit === state.fit) return;
        // Keep the same spot of the current page in view.
        const anchor = pages[state.current - 1];
        const view = viewTop();
        const before = anchor.el.getBoundingClientRect();
        const offset = (view - before.top) / Math.max(1, before.height);
        state.fit = fit;
        state.scale = scale;
        layout();
        const after = anchor.el.getBoundingClientRect();
        scrollBy(after.top - view + offset * after.height);
        pages.forEach(function (pg) { if (pg.task) pg.task.cancel(); });
        visible.forEach(function (pg) { draw(pg); });
    }

    function step(direction) {
        const s = state.scale;
        const next = direction > 0
            ? ZOOM_STEPS.find(function (z) { return z > s + 0.001; })
            : ZOOM_STEPS.slice().reverse().find(function (z) { return z < s - 0.001; });
        if (next) setScale(next, false);
    }

    // ---------- Scrolling: the pages scroll inside the pane, on phones with the page ----------

    function inPane() { return !PHONE.matches; }
    function viewTop() { return inPane() ? pagesEl.getBoundingClientRect().top : 0; }
    function viewHeight() { return inPane() ? pagesEl.clientHeight : window.innerHeight; }
    function scrollBy(dy) {
        if (inPane()) pagesEl.scrollTop += dy; else window.scrollBy(0, dy);
    }
    function scrollPos() { return inPane() ? pagesEl.scrollTop : window.scrollY; }

    // After "go to page n" the scroll may stop short of it (the last pages cannot reach the top),
    // so keep n as the current page until the user scrolls again.
    let pinnedAt = null;

    function goTo(n) {
        n = Math.min(pages.length, Math.max(1, n));
        const gap = inPane() ? parseFloat(getComputedStyle(pagesEl).paddingTop) : 12;
        scrollBy(pages[n - 1].el.getBoundingClientRect().top - viewTop() - gap);
        pinnedAt = scrollPos();
        setCurrent(n);
    }

    function setCurrent(n) {
        if (n === state.current && root.dataset.current) return;
        state.current = n;
        root.dataset.current = String(n);
        if (ui) {
            ui.page.textContent = String(n);
            ui.prev.disabled = n <= 1;
            ui.next.disabled = n >= pages.length;
        }
        thumbs.forEach(function (t, i) {
            if (i + 1 === n) {
                t.button.setAttribute('aria-current', 'page');
                // Keep the current thumbnail in the strip's view.
                const top = t.button.offsetTop - thumbsEl.offsetTop;
                if (top < thumbsEl.scrollTop || top + t.button.offsetHeight > thumbsEl.scrollTop + thumbsEl.clientHeight) {
                    thumbsEl.scrollTop = top - 14;
                }
            } else {
                t.button.removeAttribute('aria-current');
            }
        });
    }

    // The current page is the last one whose top is above a third of the view.
    // At the very end of a scrolled document it is the last page.
    function trackCurrent() {
        if (pinnedAt !== null && Math.abs(scrollPos() - pinnedAt) < 2) return;
        pinnedAt = null;
        const line = viewTop() + viewHeight() / 3;
        let n = 1;
        for (const pg of pages) {
            if (pg.el.getBoundingClientRect().top <= line) n = pg.n; else break;
        }
        const end = inPane()
            ? pagesEl.scrollHeight - pagesEl.clientHeight
            : document.documentElement.scrollHeight - window.innerHeight;
        if (scrollPos() > 0 && scrollPos() >= end - 2) n = pages.length;
        setCurrent(n);
    }

    let ticking = false;
    function onScroll() {
        if (ticking) return;
        ticking = true;
        requestAnimationFrame(function () { ticking = false; trackCurrent(); });
    }
    pagesEl.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('scroll', onScroll, { passive: true });

    // ---------- Drawing pages (lazy) ----------

    async function draw(pg) {
        const scale = state.scale;
        if (pg.drawn === scale || pg.drawing === scale) return;
        if (pg.task) pg.task.cancel();
        pg.drawing = scale;

        const viewport = pg.page.getViewport({ scale: scale * CSS_UNITS });
        let ratio = window.devicePixelRatio || 1;
        if (viewport.width * viewport.height * ratio * ratio > MAX_CANVAS_PIXELS) {
            ratio = Math.sqrt(MAX_CANVAS_PIXELS / (viewport.width * viewport.height));
        }
        const canvas = document.createElement('canvas');
        canvas.width = Math.floor(viewport.width * ratio);
        canvas.height = Math.floor(viewport.height * ratio);
        canvas.setAttribute('aria-hidden', 'true');
        const task = pg.page.render({
            canvasContext: canvas.getContext('2d', { alpha: false }),
            canvas: canvas,
            viewport: viewport,
            transform: ratio !== 1 ? [ratio, 0, 0, ratio, 0, 0] : null,
        });
        pg.task = task;
        try {
            await task.promise;
        } catch (err) {
            if (pg.drawing === scale) pg.drawing = 0;
            if (!err || err.name !== 'RenderingCancelledException') {
                console.error(err);
                pg.el.dataset.error = 'true';
                firstDrawn();
            }
            return;
        }
        if (pg.drawing !== scale) return; // the zoom changed meanwhile

        if (pg.canvas) pg.canvas.replaceWith(canvas); else pg.el.prepend(canvas);
        pg.canvas = canvas;
        pg.drawn = scale;
        pg.drawing = 0;
        pg.task = null;
        pg.el.dataset.rendered = 'true';
        firstDrawn();

        // Selectable text over the drawing.
        const layer = document.createElement('div');
        layer.className = 'textLayer';
        try {
            await new pdfjs.TextLayer({
                textContentSource: pg.page.streamTextContent(),
                container: layer,
                viewport: viewport,
            }).render();
        } catch (err) {
            return; // no text layer: the page still shows
        }
        if (pg.drawn !== scale) return;
        if (pg.textLayer) pg.textLayer.replaceWith(layer); else pg.el.appendChild(layer);
        pg.textLayer = layer;
    }

    const visible = new Set();
    let pageObserver = null;
    function observePages() {
        if (pageObserver) pageObserver.disconnect();
        visible.clear();
        pageObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                const pg = pages[Number(entry.target.dataset.page) - 1];
                if (entry.isIntersecting) {
                    visible.add(pg);
                    draw(pg);
                } else {
                    visible.delete(pg);
                }
            });
        }, { root: inPane() ? pagesEl : null, rootMargin: '100% 0px' });
        pages.forEach(function (pg) { pageObserver.observe(pg.el); });
    }

    // ---------- Thumbnails (S19-7) ----------

    const thumbs = [];
    if (pages.length > 1 && thumbsEl) {
        root.classList.add('has-thumbs');
        thumbsEl.hidden = false;
        pages.forEach(function (pg) {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'dfh-pdf-thumb';
            button.setAttribute('aria-label', 'Page ' + pg.n);
            const frame = document.createElement('span');
            frame.className = 'dfh-pdf-thumb-frame';
            frame.style.height = Math.round(THUMB_WIDTH * pg.height / pg.width) + 'px';
            const number = document.createElement('span');
            number.textContent = String(pg.n);
            button.append(frame, number);
            button.addEventListener('click', function () { goTo(pg.n); });
            thumbsEl.appendChild(button);
            thumbs.push({ pg: pg, button: button, frame: frame, drawn: false });
        });

        // Thumbnails draw one at a time, after the first page, so they never hold the pages up.
        let queue = Promise.resolve();
        const thumbObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                const t = thumbs.find(function (x) { return x.button === entry.target; });
                if (!t || t.drawn) return;
                t.drawn = true;
                thumbObserver.unobserve(entry.target);
                queue = queue.then(function () { return drawThumb(t); });
            });
        }, { root: thumbsEl, rootMargin: '200px 0px' });
        firstDraw.then(function () {
            thumbs.forEach(function (t) { thumbObserver.observe(t.button); });
        });
    }

    async function drawThumb(t) {
        const ratio = Math.min(2, window.devicePixelRatio || 1);
        const viewport = t.pg.page.getViewport({ scale: THUMB_WIDTH / t.pg.width * ratio });
        const canvas = document.createElement('canvas');
        canvas.width = Math.floor(viewport.width);
        canvas.height = Math.floor(viewport.height);
        canvas.setAttribute('aria-hidden', 'true');
        try {
            await t.pg.page.render({ canvasContext: canvas.getContext('2d', { alpha: false }), canvas: canvas, viewport: viewport }).promise;
            t.frame.appendChild(canvas);
            t.button.dataset.rendered = 'true';
        } catch (err) {
            console.error(err);
        }
    }

    // ---------- Controls ----------

    if (ui) {
        ui.count.textContent = String(pages.length);
        ui.prev.addEventListener('click', function () { goTo(state.current - 1); });
        ui.next.addEventListener('click', function () { goTo(state.current + 1); });
        ui.zoomOut.addEventListener('click', function () { step(-1); });
        ui.zoomIn.addEventListener('click', function () { step(1); });
        ui.fit.addEventListener('click', function () { setScale(fitScale(), true); });
        controls.hidden = false;
    }

    // Fit width follows the pane's width (window resize, inspector shown or hidden, phone rotation).
    let resizeTimer = 0;
    new ResizeObserver(function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () { if (state.fit) setScale(fitScale(), true); }, 120);
    }).observe(pagesEl);
    PHONE.addEventListener('change', function () { observePages(); trackCurrent(); });

    state.scale = fitScale();
    layout();
    setCurrent(1);
    observePages();
    root.dataset.state = 'ready';
}
