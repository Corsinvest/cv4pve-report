// cv4pve-report — table interactions (per-table filter + sortable headers).
// Loaded by every report page AND embedded inline into the standalone HTML
// produced by the Export button (see export.js / __REPORT_TABLE_JS__).
(function () {
  'use strict';

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
})();
