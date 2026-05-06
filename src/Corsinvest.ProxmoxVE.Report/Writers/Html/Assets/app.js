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

  // --- Per-table filter ---
  // Inject a small toolbar above each data table with at least 5 rows. The input
  // narrows the visible rows to those matching the query (case-insensitive).
  document.querySelectorAll('table.data').forEach(function (table) {
    var tbody = table.querySelector('tbody');
    if (!tbody) { return; }
    var rows = tbody.querySelectorAll('tr');
    if (rows.length < 5) { return; }

    var toolbar = document.createElement('div');
    toolbar.className = 'table-toolbar';

    var input = document.createElement('input');
    input.type = 'search';
    input.className = 'table-filter';
    input.placeholder = 'Filter rows…';
    input.setAttribute('aria-label', 'Filter rows');

    var count = document.createElement('span');
    count.className = 'table-count';
    count.textContent = rows.length + ' rows';

    // Count first then input → with the toolbar's justify-content: flex-end the
    // count sits left of the input, both pushed to the right edge of the toolbar.
    toolbar.appendChild(count);
    toolbar.appendChild(input);

    // Insert the toolbar BEFORE the .table-scroll wrapper (or, if the table isn't
    // wrapped, before the table itself). This keeps the toolbar fixed while the
    // table inside scrolls horizontally.
    var anchor = table.closest('.table-scroll') || table;
    anchor.parentNode.insertBefore(toolbar, anchor);

    input.addEventListener('input', function () {
      var q = input.value.toLowerCase();
      var visible = 0;
      rows.forEach(function (r) {
        var match = !q || r.textContent.toLowerCase().indexOf(q) !== -1;
        r.hidden = !match;
        if (match) { visible++; }
      });
      count.textContent = (q ? visible + ' / ' + rows.length : rows.length) + ' rows';
    });
  });

  // --- Sortable tables ---
  document.querySelectorAll('table.sortable').forEach(function (table) {
    var headers = table.querySelectorAll('thead th');
    headers.forEach(function (th, idx) {
      th.addEventListener('click', function () {
        var current = th.getAttribute('aria-sort');
        var asc = current !== 'ascending';
        headers.forEach(function (h) { h.removeAttribute('aria-sort'); });
        th.setAttribute('aria-sort', asc ? 'ascending' : 'descending');

        var type = th.dataset.type || 'text';
        var tbody = table.querySelector('tbody');
        var rows = Array.prototype.slice.call(tbody.querySelectorAll('tr'));

        rows.sort(function (a, b) {
          var av = (a.children[idx] && a.children[idx].textContent || '').trim();
          var bv = (b.children[idx] && b.children[idx].textContent || '').trim();
          var cmp;
          if (type === 'number') {
            cmp = parseFloat(av.replace(/[^\d.\-eE]/g, '')) - parseFloat(bv.replace(/[^\d.\-eE]/g, ''));
            if (isNaN(cmp)) { cmp = av.localeCompare(bv); }
          } else if (type === 'date') {
            cmp = (Date.parse(av) || 0) - (Date.parse(bv) || 0);
          } else {
            cmp = av.localeCompare(bv, undefined, { numeric: true });
          }
          return asc ? cmp : -cmp;
        });

        rows.forEach(function (r) { tbody.appendChild(r); });
      });
    });
  });

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
  var items = nav.querySelectorAll('a');
  var groups = nav.querySelectorAll('details');

  filter.addEventListener('input', function () {
    var q = filter.value.toLowerCase();

    items.forEach(function (a) {
      var match = !q || a.textContent.toLowerCase().indexOf(q) !== -1;
      a.hidden = !match;
    });

    // Hide groups that have no visible items
    groups.forEach(function (g) {
      var visibleCount = 0;
      g.querySelectorAll('a').forEach(function (a) { if (!a.hidden) { visibleCount++; } });
      g.hidden = visibleCount === 0;
      if (q && visibleCount > 0) { g.open = true; }
    });
  });
})();
