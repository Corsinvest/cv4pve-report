# HTML format reference (`--format Html`)

`--format Html` produces a self-contained zipped static website. Extract the zip and open `index.html` in any browser — works fully offline, no server needed.

```
Report_20260506_120000.zip    ← static website (extract and open index.html)
```

---

## File layout inside the zip

```
report.zip
├── index.html                 ← cover / home page
├── network-diagram.html       ← network topology page
├── network-diagram.svg        ← raw SVG (embedded by network-diagram.html)
│
├── cluster.html
├── cluster-log.html
├── cluster-tasks.html
├── nodes.html                 ← Nodes overview page
├── nodes/
│   ├── cc01.html              ← per-node detail page
│   └── cc02.html
├── vms.html                   ← VMs overview page
├── vms/
│   ├── 100.html               ← per-VM detail page
│   └── 101.html
├── containers.html
├── containers/
│   └── 200.html
├── storages.html
├── storage-content.html
├── backups.html
├── disks.html
├── partitions.html
├── snapshots.html
├── network.html
├── firewall.html
├── replication.html
├── rrd-nodes.html
├── rrd-storages.html
├── rrd-guests.html
├── syslog.html
│
└── assets/
    ├── style.css              ← stylesheet (light + dark)
    ├── app.js                 ← shared behaviour (theme, sidebar, lazy groups, export)
    ├── table.js               ← per-table search + click-to-sort
    ├── sidebar-data.js        ← node/VM/container links for the lazy sidebar
    └── export-data.js         ← CSS + table.js inlined for the standalone export
```

The same logical sections produced by the XLSX format become **one HTML page each** — see the [Excel format guide](format-xlsx.md) for the per-section table contents (the data is identical, only the rendering differs).

---

## Sidebar

Every page shares the same sidebar. Top-level entries are organised into expandable groups:

- **Cluster** (overview + Cluster Log + Cluster Tasks)
- **Nodes** (overview + per-node detail pages)
- **VMs** (overview + per-VM detail pages)
- **Containers** (overview + per-CT detail pages)
- **Storage** (Storages + Storage Content + Backups + Disks + Partitions + Snapshots)
- **Network**, **Firewall**, **Replication** (single-page sections)
- **Performance** (RRD Nodes + RRD Guests + RRD Storage + Syslog)

The current page is highlighted in blue. A search box at the top of the sidebar filters all entries by id or name (numeric queries match by id prefix, e.g. `102` matches `102 — opnsense-cc02` but not `1020 — db`).

---

## Theme

Light and dark themes are bundled. Click the sun/moon icon in the top-right of the sidebar to toggle. The choice is persisted across pages via `localStorage`. Without an explicit choice, the report follows the OS preference (`prefers-color-scheme`).

---

## Per-page features

- **Sortable columns** — click any table header to cycle through ascending → descending → original order. The "original order" step is the order the engine emitted (e.g. nodes by hostname, snapshots by date), so a third click always returns the table to its initial state.
- **Global filter** — every table with at least 5 rows gets a `Filter rows…` box above it. Match defaults to **contains** (`~`); click the small toggle on the left of the box to switch to **exact** (`=`), which matches only rows where at least one cell equals the typed text — useful for short numeric ids like a VM id where `100` would otherwise also match `1014`, `260509100047` and so on.
- **Per-column filter** — column headers backed by short text, flag, or hyperlinked values (e.g. `Node`, `VmId`, `Status`, `Type`) carry a small funnel icon, hidden until you hover. Click it to reveal a dedicated filter input under the header for that column. Each per-column filter has its own `~`/`=` toggle. Global filter and per-column filters combine with AND.
- **Page-level Index** — the top of every detail page lists the tables on that page as anchor links.
- **Print stylesheet** — `Ctrl+P` produces a clean printout: sidebar, filters and sort controls hidden, tables compacted with repeating headers.

> Filters are intentionally minimal — `~`/`=` covers the day-to-day cases. For regex, multi-criteria, range filters or pivots, the `report.xlsx` shipped in the same `.zip` has Excel's full autofilter built-in.

---

## Export button

Every node, VM and container detail page has an **Export** button (top right). One click downloads a self-contained `vms-100-standalone.html` (or similar) with CSS and per-table interactions inlined — paste it into a ticket, attach it to an email or drop it on a wiki without sending the full report.

The exported page:
- Has no sidebar
- Has no cross-page links (links to other pages are converted to plain text; in-page anchors are preserved)
- Keeps the current theme (light/dark)
- Keeps sort & search behaviour for tables

---

## Built for large clusters

The sidebar lazy-loads its long groups (Nodes, VMs, Containers) from a shared `assets/sidebar-data.js`, so even on **2700+ VM clusters** the report stays fast and the zip stays small. Each detail page only carries its own content; the navigation skeleton is identical across pages and the browser caches the shared assets.

---

## Network topology SVG

Bundled inside the zip as `network-diagram.svg` and shown via `network-diagram.html` (sidebar entry "Network Diagram"). The SVG carries an explicit `viewBox` and scales correctly inside the report and when opened standalone — see the [network diagram guide](network-diagram.md) for the legend and layout.
