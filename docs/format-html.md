# HTML format reference (`--format Html`)

`--format Html` produces a self-contained zipped static website. Extract the zip and open `index.html` in any browser — works fully offline, no server needed.

---

## File layout inside the zip

```
Report_20260506_120000.zip
├── index.html                 ← Summary (cover, filters, contents)
├── issues.html                ← only present when at least one collection failure was recorded
├── network-diagram.html       ← network topology page
├── network-diagram.svg        ← raw SVG (embedded by network-diagram.html)
│
├── cluster.html
│
├── storages.html
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
├── network.html
├── storage-content.html
├── backups.html
├── disks.html
├── partitions.html
├── snapshots.html
├── firewall.html
├── replication.html
│
├── rrd-nodes.html
├── rrd-storage.html
├── rrd-guests.html
├── syslog.html
│
├── cluster-access.html        ← Cluster deep-dives
├── cluster-sdn.html
├── cluster-ha.html
├── cluster-pools.html
├── cluster-log.html
├── cluster-tasks.html
│
└── assets/
    ├── style.css              ← stylesheet (light + dark)
    ├── app.js                 ← shared behaviour (theme, sidebar, lazy groups, export)
    ├── table.js               ← per-table search + click-to-sort
    ├── sidebar-data.js        ← node/VM/container links for the lazy sidebar
    └── export-data.js         ← CSS + table.js inlined for the standalone export
```

---

## Sections

Pages appear in this order in the sidebar. Conditional pages (`if …`) are only emitted when the matching setting is enabled or when the underlying data exists.

| # | Page | Description | Condition |
|---|------|-------------|-----------|
| 1 | **Summary** (`index.html`) | Cover with metadata, filters, hyperlinked Contents table | always |
| 2 | **Issues** (`issues.html`) | Diagnostics for collection failures — see [Issues](#issues) below | only when failures recorded |
| 3 | **Network Diagram** (`network-diagram.html`) | Topology SVG with embedded view | always when the SVG was generated |
| 4 | **Cluster** (`cluster.html`) | Cluster status, options, firewall options, backup jobs, replication, storages, metric servers and hardware mappings | `Cluster.Include` |
| 5 | **Storages** (`storages.html`) | Storage list with size, usage, type | always |
| 6 | **Nodes** (`nodes.html` + `nodes/<name>.html`) | Node overview → per-node detail pages | always |
| 7 | **VMs** (`vms.html` + `vms/<id>.html`) | VM overview → per-VM detail pages | always |
| 8 | **Containers** (`containers.html` + `containers/<id>.html`) | Container overview → per-CT detail pages | always |
| 9 | **Network** (`network.html`) | Node interfaces + VM/CT NICs (MAC, bridge, VLAN, IPs, model) | always |
| 10 | **Storage Content** (`storage-content.html`) | Storage files/images with size and VM ID links | `Storage.IncludeContent` |
| 11 | **Backups** (`backups.html`) | Backup files across all storages | `Storage.IncludeBackups` |
| 12 | **Disks** (`disks.html`) | Global VM/CT disk inventory | `Guest.IncludeDisks` |
| 13 | **Partitions** (`partitions.html`) | Guest disk partitions via QEMU agent | `Guest.IncludePartitions` |
| 14 | **Snapshots** (`snapshots.html`) | Global snapshot inventory across all VMs/CTs | `Guest.IncludeSnapshots` |
| 15 | **Firewall** (`firewall.html`) | Cluster + node + VM/CT firewall rules, aliases, IP sets | `Firewall.Enabled` |
| 16 | **Replication** (`replication.html`) | Replication job status across all nodes | `Node.IncludeReplication` |
| 17 | **RRD Nodes** (`rrd-nodes.html`) | Historical performance metrics per node | `Node.RrdData.Enabled` |
| 18 | **RRD Storage** (`rrd-storage.html`) | Historical performance metrics per storage | `Storage.RrdData.Enabled` |
| 19 | **RRD Guests** (`rrd-guests.html`) | Historical performance metrics per VM/CT | `Guest.RrdData.Enabled` |
| 20 | **Syslog** (`syslog.html`) | Parsed systemd journal across all nodes | `Node.Syslog.Enabled` |
| 21 | **Cluster Access** (`cluster-access.html`) | Users, API tokens, two-factor authentication, groups, roles, ACL and domains | `Cluster.Include` |
| 22 | **Cluster SDN** (`cluster-sdn.html`) | SDN zones, vnets, controllers, IPAMs and subnets | `Cluster.Include` |
| 23 | **Cluster HA** (`cluster-ha.html`) | High Availability resources, groups and status | `Cluster.Include` |
| 24 | **Cluster Pools** (`cluster-pools.html`) | Resource pools with member VMs, containers and storages | `Cluster.Include` |
| 25 | **Cluster Log** (`cluster-log.html`) | Cluster event log | `Cluster.Log.Enabled` |
| 26 | **Cluster Tasks** (`cluster-tasks.html`) | Recent tasks across the cluster | `Cluster.IncludeTasks` |
| 27 | **Compliance** (`compliance.html` + `compliance/<pack>.html`) | Compliance overview → per-pack detail pages | any `Compliance.*` flag |

> The contents of each page (which tables, which columns) are emitted by the engine and identical across formats — see [`docs/settings.md`](settings.md) for the toggles.

The sidebar groups Nodes / VMs / Containers under expandable headers, and Storage / Performance under synthetic groups, but the underlying ordering is the table above.

---

## Issues

When one or more Proxmox API calls fail during collection (a corrupt RRD file, a `500` from a node, missing permissions on a sub-resource, …), an extra `issues.html` page is emitted as the **second** sidebar link, right after Summary. It also appears as the **first** row of the Contents table on the cover. On a healthy cluster the page (and the link) are absent.

| Column | Content |
|---|---|
| Severity | `Warning` (default) — `501 Not Implemented` is silent and never appears here. |
| Section | The logical section where the failure happened (e.g. `RRD Storage`, `Firewall Log`, `Cluster`). **Hyperlinked** to the page where the user can investigate — VM / Node / Storage / global section. |
| Message | Diagnostic line built from the Proxmox response: HTTP status, the API error body and the failing endpoint, joined with `—`. Example: `500 Internal Server Error — got wrong time resolution (60 != 1800) — GET /nodes/orion/storage/local-ceph/rrddata`. |
| Timestamp | When the failure was recorded. |

The page uses the same per-table search and click-to-sort as the rest of the report, so filtering by severity or section is immediate.

---

## Compliance

When at least one standard is enabled in `settings.json` under `Compliance`, the report adds a `Compliance` group in the sidebar with an **Overview** entry (`compliance.html`) plus one sub-page per enabled pack under `compliance/<pack>.html`. Each pack page contains Info / Disclaimer / Controls / Checks tables; the **Status** column renders as a coloured badge (✓ PASS / ✗ FAIL / ◐ PARTIAL / — N/A) and the **Score %** column as a colour-coded number. Every table uses the standard per-column filter / sort so isolating `FAIL` rows or `High`/`Critical` severities is immediate. Full reference: [docs/compliance.md](compliance.md).

---

## HTML-specific behaviour

- **Sidebar** — every page shares the same navigation. Top-level entries are organised into expandable groups (Cluster / Nodes / VMs / Containers / Storage / Performance) plus flat links (Network, Firewall, Replication, Issues, Network Diagram). The current page is highlighted; a search box filters all entries by id or name (numeric queries match by id prefix, e.g. `102` matches `102 — opnsense-cc02` but not `1020 — db`).
- **Light & dark theme** — toggle via the sun/moon icon in the top-right of the sidebar; persisted in `localStorage`, defaults to OS preference (`prefers-color-scheme`).
- **Sortable columns** — click any table header to cycle through ascending → descending → original order. The third click restores the order the engine emitted (e.g. nodes by hostname, snapshots by date).
- **Global filter** — every table with at least 5 rows gets a `Filter rows…` box above it. Match defaults to **contains** (`~`); the toggle on the left switches to **exact** (`=`), useful for short numeric ids where `100` would otherwise also match `1014`, `260509100047`, etc.
- **Per-column filter** — column headers backed by short text, flag, or hyperlinked values carry a hover-revealed funnel icon. Click it to reveal a per-column input. Each has its own `~`/`=` toggle. Global + per-column filters combine with AND.
- **Page-level Index** — every detail page starts with anchor links to the tables on that page.
- **Print stylesheet** — `Ctrl+P` produces a clean printout: sidebar, filters and sort controls hidden, tables compacted with repeating headers.
- **Export button** — every node, VM and container detail page has an **Export** button (top right). One click downloads a self-contained `vms-100-standalone.html` (or similar) with CSS and per-table interactions inlined — paste it into a ticket, email it or drop it on a wiki without sending the full report. The exported page has no sidebar and no cross-page links, but keeps the current theme and the table behaviour.
- **Built for large clusters** — the sidebar lazy-loads its long groups (Nodes, VMs, Containers) from a shared `assets/sidebar-data.js`, so even on **2700+ VM clusters** the report stays fast and the zip stays small. Each detail page only carries its own content; navigation chrome is identical across pages and the browser caches the shared assets.
- **Network topology SVG** — bundled in the same zip and shown via `network-diagram.html`. The SVG carries an explicit `viewBox` and scales correctly inside the report and when opened standalone. See the [network diagram guide](network-diagram.md) for the legend and layout.

> Filters are intentionally minimal — `~`/`=` covers the day-to-day cases. For regex, multi-criteria, range filters or pivots, the `report.xlsx` shipped in the same `.zip` has Excel's full autofilter built-in.
