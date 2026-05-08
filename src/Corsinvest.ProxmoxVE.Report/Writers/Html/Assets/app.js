// cv4pve-report — table sort + sidebar filter + theme toggle
(function () {
  'use strict';

  // --- Theme (light/dark) ---
  // Apply saved choice ASAP to avoid a light-then-dark flash.
  var savedTheme = localStorage.getItem('cv4pve-theme');
  if (savedTheme === 'light' || savedTheme === 'dark') {
    document.documentElement.setAttribute('data-theme', savedTheme);
  }

  var themeBtn = document.getElementById('theme-toggle');
  if (themeBtn) {
    themeBtn.addEventListener('click', function () {
      var current = document.documentElement.getAttribute('data-theme');
      // Determine the *currently visible* mode (taking system preference into account
      // when no explicit choice was made), then switch to the opposite.
      var isDark = current === 'dark'
        || (!current && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
      var next = isDark ? 'light' : 'dark';
      document.documentElement.setAttribute('data-theme', next);
      localStorage.setItem('cv4pve-theme', next);
    });
  }

  // --- Back to top button ---
  var backToTop = document.getElementById('back-to-top');
  if (backToTop) {
    var SHOW_AFTER_PX = 400;
    var toggle = function () {
      if (window.scrollY > SHOW_AFTER_PX) {
        backToTop.classList.add('visible');
      } else {
        backToTop.classList.remove('visible');
      }
    };
    window.addEventListener('scroll', toggle, { passive: true });
    toggle();
    backToTop.addEventListener('click', function () {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    });
  }

  // --- Sidebar lazy groups ---
  // Groups whose children scale with the cluster size (Nodes/VMs/Containers) are
  // emitted as empty skeletons in the per-page HTML; the actual links live in
  // `assets/sidebar-data.js` (loaded once and cached) and are injected into the
  // DOM the first time the user expands the group.
  function renderLazyGroup(details) {
    var container = details.querySelector('.group-children');
    if (!container || container.dataset.loaded === 'true') { return; }
    var items = (window.__SIDEBAR_DATA__ || {})[details.dataset.group] || [];
    var html = '';
    for (var i = 0; i < items.length; i++) {
      var it = items[i];
      var href = it.href || '';
      var label = (it.label || '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
      html += '<a href="' + href + '">' + label + '</a>';
    }
    container.innerHTML = html;
    container.dataset.loaded = 'true';

    // Mark the link to the current page using the DOM-resolved `.href` (the
    // browser already accounts for <base> and file:// origins without emitting
    // any "Unsafe attempt to load URL" warnings).
    var currentHref = location.href.split('#')[0].toLowerCase();
    container.querySelectorAll('a').forEach(function (a) {
      if (a.href.split('#')[0].toLowerCase() === currentHref) { a.classList.add('active'); }
    });

    // Scroll the active link into view so the user immediately sees their
    // current page within long lists (e.g. VMs with thousands of entries).
    var active = container.querySelector('a.active');
    if (active && active.scrollIntoView) {
      active.scrollIntoView({ block: 'nearest', behavior: 'instant' });
    }
  }

  document.querySelectorAll('details[data-group]').forEach(function (d) {
    d.addEventListener('toggle', function () {
      if (d.open) { renderLazyGroup(d); }
    });
  });

  // Auto-expand the group that owns the current page so the user sees the
  // surrounding context without an extra click. Mapping is by sub-directory:
  // /vms/* → "VMs", /containers/* → "Containers", /nodes/* → "Nodes".
  (function () {
    var path = location.pathname;
    var groupName =
      path.indexOf('/vms/') >= 0 ? 'VMs' :
      path.indexOf('/containers/') >= 0 ? 'Containers' :
      path.indexOf('/nodes/') >= 0 ? 'Nodes' : null;
    if (!groupName) { return; }
    var d = document.querySelector('details[data-group="' + groupName + '"]');
    if (d) {
      d.open = true;
      renderLazyGroup(d);
    }
  })();

  // --- Export standalone HTML ---
  // exportPage() is self-contained: it lazy-loads `assets/export-data.js` (which
  // declares window.__REPORT_CSS__ + window.__REPORT_TABLE_JS__ as inlined strings)
  // the first time it's invoked, then runs the export. Subsequent calls go
  // straight to the export logic.
  function exportPage() {
    if (!window.__REPORT_CSS__ || !window.__REPORT_TABLE_JS__) {
      var s = document.createElement('script');
      s.src = 'assets/export-data.js';
      s.onload = function () {
        if (window.__REPORT_CSS__ && window.__REPORT_TABLE_JS__) {
          exportPage();
        } else {
          console.error('export-data.js loaded but data is missing');
        }
      };
      s.onerror = function () { console.error('Failed to load assets/export-data.js'); };
      document.head.appendChild(s);
      return;
    }

    var main = document.querySelector('main');
    if (!main) { return; }

    var clone = main.cloneNode(true);
    clone.querySelectorAll('.page-actions, .table-toolbar').forEach(function (n) { n.remove(); });

    // Neutralise links that would point to other pages of the report (which don't
    // exist in a standalone file). Three cases:
    //   - external (http/https/mailto): keep as-is
    //   - in-document anchors (#…): keep as-is
    //   - everything else (relative paths): if href contains a fragment rewrite
    //     to "#anchor"; otherwise replace the <a> with a plain <span>.
    clone.querySelectorAll('a[href]').forEach(function (a) {
      var href = a.getAttribute('href') || '';
      if (/^(?:[a-z]+:|#)/i.test(href)) { return; }
      var hashAt = href.indexOf('#');
      if (hashAt >= 0) {
        a.setAttribute('href', href.substring(hashAt));
      } else {
        var span = document.createElement('span');
        span.textContent = a.textContent;
        a.parentNode.replaceChild(span, a);
      }
    });

    var theme = document.documentElement.getAttribute('data-theme') || '';
    var themeAttr = theme ? ' data-theme="' + theme + '"' : '';
    var html = '<!doctype html>\n<html lang="en"' + themeAttr + '>\n'
      + '<head>\n  <meta charset="utf-8">\n  <meta name="viewport" content="width=device-width, initial-scale=1">\n'
      + '  <title>' + document.title + '</title>\n'
      + '  <style>' + window.__REPORT_CSS__ + '</style>\n'
      + '</head>\n<body><main>' + clone.innerHTML + '</main>\n'
      + '<script>' + window.__REPORT_TABLE_JS__ + '</' + 'script>\n'
      + '</body>\n</html>';

    var blob = new Blob([html], { type: 'text/html;charset=utf-8' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    var fileName = location.pathname.split('/').pop() || 'page.html';
    a.download = fileName.replace(/\.html?$/, '') + '-standalone.html';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
  }

  var exportBtn = document.getElementById('export-btn');
  if (exportBtn) { exportBtn.addEventListener('click', exportPage); }

  // Per-table filter + sortable headers live in `assets/table.js` so the same
  // code can also be embedded inline into the standalone HTML produced by Export.

  // --- Sidebar: highlight current page + restore scroll ---
  var sidebar = document.querySelector('aside.sidebar');
  if (sidebar) {
    // The `<a>.href` property already returns the fully-resolved absolute URL
    // taking into account any `<base href="../">` on the page — so we can just
    // compare it to location.href directly. (Computing it manually with
    // `new URL(getAttribute('href'), …)` is tricky to get right when a <base>
    // is present, because the second argument has to be document.baseURI, not
    // location.href.)
    var currentHref = location.href.split('#')[0].toLowerCase();

    sidebar.querySelectorAll('a').forEach(function (a) {
      var resolvedHref = a.href.split('#')[0].toLowerCase();
      if (resolvedHref === currentHref) {
        a.classList.add('active');
        // Make sure the parent <details> is open so the active item is visible
        var parent = a.closest('details');
        if (parent) { parent.open = true; }
      }
    });

    // Restore previous sidebar scroll (so clicking a link doesn't reset to top)
    var savedScroll = sessionStorage.getItem('cv4pve-sidebar-scroll');
    if (savedScroll) { sidebar.scrollTop = parseInt(savedScroll, 10) || 0; }
    sidebar.addEventListener('scroll', function () {
      sessionStorage.setItem('cv4pve-sidebar-scroll', String(sidebar.scrollTop));
    });

    // Save scroll position right before navigating away from the page
    sidebar.addEventListener('click', function (e) {
      var link = e.target.closest('a');
      if (link) { sessionStorage.setItem('cv4pve-sidebar-scroll', String(sidebar.scrollTop)); }
    });
  }

  // --- Sidebar filter ---
  var filter = document.getElementById('sidebar-filter');
  if (!filter) { return; }

  var nav = document.getElementById('sidebar-nav');
  var groups = nav.querySelectorAll('details');

  filter.addEventListener('input', function () {
    var q = filter.value.trim().toLowerCase();

    // When filtering, make sure lazy groups are populated so their children
    // are present in the DOM and filterable like any other link.
    if (q) {
      nav.querySelectorAll('details[data-group]').forEach(renderLazyGroup);
    }

    nav.querySelectorAll('a').forEach(function (a) {
      a.hidden = q && a.textContent.toLowerCase().indexOf(q) === -1;
    });

    // Hide groups that have no visible items, expand those that do.
    groups.forEach(function (g) {
      var visibleCount = 0;
      g.querySelectorAll('a').forEach(function (a) { if (!a.hidden) { visibleCount++; } });
      g.hidden = visibleCount === 0;
      if (q && visibleCount > 0) { g.open = true; }
    });
  });
})();
