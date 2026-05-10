/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

// cv4pve-report — table interactions: global filter, per-column filter, sortable headers.
// Loaded by every report page AND inlined into the standalone HTML export.
(function () {
  'use strict';

  // Precompiled: the sort comparator runs O(n log n) regex applications on large tables.
  var NUM_CLEAN = /[^\d.\-eE]/g;

  function matchCell(cellText, query, mode) {
    return mode === 'exact' ? cellText.trim() === query : cellText.indexOf(query) !== -1;
  }

  // Exact mode on a row matches when at least one cell equals the query
  // (substring-on-joined-text is meaningless in exact mode).
  function matchRow(row, query, mode) {
    if (!query) { return true; }
    if (mode === 'exact') {
      const cells = row.children;
      for (let i = 0; i < cells.length; i++) {
        if (cells[i].textContent.trim().toLowerCase() === query) { return true; }
      }
      return false;
    }
    return row.textContent.toLowerCase().indexOf(query) !== -1;
  }

  // [mode-toggle | input] wrapper. Toggle cycles ~ (contains) ↔ = (exact).
  // Returns { element, input, getQuery, getMode, onChange }.
  function createFilterBox(opts) {
    const box = document.createElement('span');
    box.className = 'filter-box';

    const toggle = document.createElement('button');
    toggle.type = 'button';
    toggle.className = 'filter-mode';
    toggle.textContent = '~';
    toggle.title = 'Match: contains (click to toggle exact)';
    toggle.setAttribute('aria-label', 'Match mode: contains');

    const input = document.createElement('input');
    input.type = 'search';
    input.className = opts.inputClass;
    input.placeholder = opts.placeholder;
    input.setAttribute('aria-label', opts.ariaLabel);

    box.appendChild(toggle);
    box.appendChild(input);

    let mode = 'contains';
    const listeners = [];
    function fire() { listeners.forEach(function (cb) { cb(); }); }

    toggle.addEventListener('click', function (ev) {
      ev.stopPropagation();
      mode = mode === 'contains' ? 'exact' : 'contains';
      toggle.textContent = mode === 'exact' ? '=' : '~';
      toggle.title = 'Match: ' + mode + ' (click to toggle ' + (mode === 'exact' ? 'contains' : 'exact') + ')';
      toggle.setAttribute('aria-label', 'Match mode: ' + mode);
      box.classList.toggle('exact', mode === 'exact');
      input.focus();
      fire();
    });
    input.addEventListener('input', fire);
    input.addEventListener('click', function (ev) { ev.stopPropagation(); });

    return {
      element: box,
      input: input,
      getQuery: function () { return input.value.toLowerCase(); },
      getMode: function () { return mode; },
      onChange: function (cb) { listeners.push(cb); }
    };
  }

  document.querySelectorAll('table.data').forEach(function (table) {
    const tbody = table.querySelector('tbody');
    if (!tbody) { return; }
    const rows = tbody.querySelectorAll('tr');
    if (rows.length === 0) { return; }

    const toolbar = document.createElement('div');
    toolbar.className = 'table-toolbar';

    const count = document.createElement('span');
    count.className = 'table-count';
    count.textContent = rows.length + ' rows';

    const globalBox = createFilterBox({
      inputClass: 'table-filter',
      placeholder: 'Filter rows…',
      ariaLabel: 'Filter rows'
    });

    toolbar.appendChild(count);
    toolbar.appendChild(globalBox.element);

    const anchor = table.closest('.table-scroll') || table;
    anchor.parentNode.insertBefore(toolbar, anchor);

    const colFilters = {};
    const headers = table.querySelectorAll('thead > tr:first-child > th');
    const thead = table.querySelector('thead');
    let filterRow = null;

    headers.forEach(function (th, idx) {
      if (th.dataset.filterable !== 'true') { return; }

      const toggle = document.createElement('button');
      toggle.type = 'button';
      toggle.className = 'col-filter-toggle';
      toggle.setAttribute('aria-label', 'Filter ' + (th.textContent || 'column').trim());
      th.appendChild(toggle);

      if (!filterRow) {
        filterRow = document.createElement('tr');
        filterRow.className = 'col-filter-row';
        filterRow.hidden = true;
        for (let i = 0; i < headers.length; i++) {
          filterRow.appendChild(document.createElement('td'));
        }
        thead.appendChild(filterRow);
      }

      const colBox = createFilterBox({
        inputClass: 'col-filter',
        placeholder: 'Filter…',
        ariaLabel: 'Filter ' + (th.textContent || 'column').trim() + ' values'
      });
      filterRow.children[idx].appendChild(colBox.element);

      toggle.addEventListener('click', function (ev) {
        ev.stopPropagation();
        filterRow.hidden = !filterRow.hidden;
        if (!filterRow.hidden) { colBox.input.focus(); }
      });
      colBox.onChange(function () {
        if (colBox.input.value) { th.classList.add('col-filtered'); }
        else { th.classList.remove('col-filtered'); }
        applyFilters();
      });

      colFilters[idx] = colBox;
    });

    function applyFilters() {
      const q = globalBox.getQuery();
      const qMode = globalBox.getMode();
      const colQs = Object.keys(colFilters).map(function (k) {
        return { idx: +k, q: colFilters[k].getQuery(), mode: colFilters[k].getMode() };
      }).filter(function (c) { return c.q; });
      let visible = 0;
      rows.forEach(function (r) {
        const match = matchRow(r, q, qMode) && colQs.every(function (c) {
          const cell = r.children[c.idx];
          return cell && matchCell(cell.textContent.toLowerCase(), c.q, c.mode);
        });
        r.hidden = !match;
        if (match) { visible++; }
      });
      const hasFilter = q || colQs.length > 0;
      count.textContent = (hasFilter ? visible + ' / ' + rows.length : rows.length) + ' rows';
    }

    globalBox.onChange(applyFilters);
  });

  // Header click cycles: original → asc → desc → original.
  document.querySelectorAll('table.sortable').forEach(function (table) {
    const headers = table.querySelectorAll('thead > tr:first-child > th');
    const tbody = table.querySelector('tbody');
    if (!tbody) { return; }
    let originalOrder = null;

    headers.forEach(function (th, idx) {
      th.addEventListener('click', function (ev) {
        if (ev.target.closest('.col-filter, .col-filter-toggle, .filter-box')) { return; }

        if (originalOrder === null) {
          originalOrder = [...tbody.querySelectorAll('tr')];
        }

        const current = th.getAttribute('aria-sort');
        const next = current === 'ascending' ? 'descending'
          : current === 'descending' ? null
            : 'ascending';
        headers.forEach(function (h) { h.removeAttribute('aria-sort'); });

        if (next === null) {
          originalOrder.forEach(function (r) { tbody.appendChild(r); });
          return;
        }

        th.setAttribute('aria-sort', next);
        const asc = next === 'ascending';
        const type = th.dataset.type || 'text';
        const rows = [...tbody.querySelectorAll('tr')];

        rows.sort(function (a, b) {
          const av = (a.children[idx] && a.children[idx].textContent || '').trim();
          const bv = (b.children[idx] && b.children[idx].textContent || '').trim();
          let cmp;
          if (type === 'number') {
            cmp = parseFloat(av.replace(NUM_CLEAN, '')) - parseFloat(bv.replace(NUM_CLEAN, ''));
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
