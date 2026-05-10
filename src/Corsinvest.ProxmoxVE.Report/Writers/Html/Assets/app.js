/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

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
      // Resolve the *currently visible* mode (system preference counts when no explicit choice).
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
  // Children of cluster-sized groups (Nodes/VMs/Containers) live in
  // assets/sidebar-data.js and are injected on first expand.
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

    // .href (DOM property) resolves <base> + file:// without "Unsafe URL" warnings.
    var currentHref = location.href.split('#')[0].toLowerCase();
    container.querySelectorAll('a').forEach(function (a) {
      if (a.href.split('#')[0].toLowerCase() === currentHref) { a.classList.add('active'); }
    });

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

  // Auto-expand the group owning the current page (/vms/* → "VMs", etc.)
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
  // Lazy-loads assets/export-data.js (window.__REPORT_CSS__ + __REPORT_TABLE_JS__)
  // on the first call, then re-enters to perform the export.
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

    // Standalone file = no other pages: keep external/anchor links, rewrite or strip the rest.
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
    var favicon = document.querySelector('link[rel="icon"]');
    var faviconHtml = favicon ? '  ' + favicon.outerHTML + '\n' : '';
    var html = '<!doctype html>\n<html lang="en"' + themeAttr + '>\n'
      + '<head>\n  <meta charset="utf-8">\n  <meta name="viewport" content="width=device-width, initial-scale=1">\n'
      + '  <title>' + document.title + '</title>\n'
      + faviconHtml
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

  // --- Sidebar: highlight current page + restore scroll ---
  var sidebar = document.querySelector('aside.sidebar');
  if (sidebar) {
    var currentHref = location.href.split('#')[0].toLowerCase();

    sidebar.querySelectorAll('a').forEach(function (a) {
      var resolvedHref = a.href.split('#')[0].toLowerCase();
      if (resolvedHref === currentHref) {
        a.classList.add('active');
        var parent = a.closest('details');
        if (parent) { parent.open = true; }
      }
    });

    var savedScroll = sessionStorage.getItem('cv4pve-sidebar-scroll');
    if (savedScroll) { sidebar.scrollTop = parseInt(savedScroll, 10) || 0; }
    sidebar.addEventListener('scroll', function () {
      sessionStorage.setItem('cv4pve-sidebar-scroll', String(sidebar.scrollTop));
    });

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

    // Filter requires lazy groups already populated so their children are in the DOM.
    if (q) {
      nav.querySelectorAll('details[data-group]').forEach(renderLazyGroup);
    }

    nav.querySelectorAll('a').forEach(function (a) {
      a.hidden = q && a.textContent.toLowerCase().indexOf(q) === -1;
    });

    groups.forEach(function (g) {
      var visibleCount = 0;
      g.querySelectorAll('a').forEach(function (a) { if (!a.hidden) { visibleCount++; } });
      g.hidden = visibleCount === 0;
      if (q && visibleCount > 0) { g.open = true; }
    });
  });
})();
