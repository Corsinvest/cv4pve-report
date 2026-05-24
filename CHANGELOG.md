# Changelog

---

## [Unreleased]

### Breaking changes (JSON, schema v2)

- **JSON now exposes raw values in table rows.** Size and IO columns no longer carry the GB/MB suffix in the key, and the value is the **raw byte count** from Proxmox; percentage columns no longer carry the `Pct` suffix (the value already was a `0–1` fraction). Examples:
  - `"memorySizeGB": 16.5` → `"memorySize": 17179869184`
  - `"netInMB": 0.02` → `"netIn": 20971`
  - `"cpuUsagePct": 0.45` → `"cpuUsage": 0.45`
  - `"ioWaitPct": 0.000195` → `"ioWait": 0.000195`
  - Excel and HTML are unchanged: they still render `16.50 GB`, `0.02 MB`, `45.00 %`. The conversion moved from the engine into each writer, so JSON consumers get the raw bytes Proxmox returned without rounding noise — snapshot diffs are now stable and lossless.
- `metadata.json` now reports `"schemaVersion": 2`. The `info` blocks inside detail files (`vms/<id>.json` → `info`, etc.) are unchanged and still use human-readable units (`"memoryGB": 16`) — that's a separate follow-up (#46). Closes #45.

---

## [2.3.0] — 2026-05-14

### What's new

- **New JSON output format — `--format Json`.** A third rendering alongside Excel and HTML, designed for automation, scripting and integrations. Produces a single zip containing one file per section (`cluster.json`, `nodes.json`, `vms.json`, `storages.json`, …) plus per-resource detail under `nodes/<name>.json`, `vms/<id>.json` and `containers/<id>.json`, same logical layout as the Excel sheets and HTML pages. A `metadata.json` carries the schema version, timestamp, application info, the filter set and a per-section generation log. The network topology SVG is bundled in the same zip as `network-diagram.svg`. Keys are camelCase with stable shapes (overview files are flat arrays of rows; detail files are objects with a fixed `info` block plus one entry per table), so consumers can navigate the dataset without knowing resource names in advance. Full reference in [`docs/format-json.md`](docs/format-json.md) with file layout, naming conventions, `jq` recipes and a snapshot-diff workflow.
- **CD-ROM and cloud-init drives now listed in the Disks sheet.** The per-VM disk inventory used to skip `ide2`-style CD-ROMs and `cloudinit` drives, only showing real block devices. They now appear alongside the rest with a new `Kind` column (`Disk`, `CDRom`, `CloudInit`) so you can filter on what's actually a backed-up disk vs. a transient mount. Thanks for the report (#39).

---

## [2.2.1] — 2026-05-13

### Fixes

- **EFI disk and TPM state now listed in the Disks sheet.** `efidisk0` and `tpmstate0` were missing from the per-VM disk inventory because the underlying SDK consumed them into dedicated typed properties instead of leaving them in the generic disk dictionary. They now appear alongside `scsi0`, `virtio0`, etc. with the same columns (storage, format, size). Thanks @shaundeeb for the report (#37).
- **SDN vnets no longer make VMs disappear from the network diagram.** When a VM was attached to an SDN vnet that wasn't materialised in the node's `/etc/network/interfaces` (typical of zones whose apply hasn't been propagated, Simple/EVPN zones, or datacenter-only vnets), the diagram had no bridge to draw the VM on and the VM was silently dropped. The diagram now learns about SDN vnets from the cluster-wide config and injects the missing bridges as synthetic boxes on every node where the zone is active. Thanks @janrenard for the report (#36).

### What's new

- **Network sheet now includes an SDN Vnets table.** Between *Nodes Networks* and *VM Networks* there is now a third table listing every SDN vnet with its zone, zone type, parent bridge, VLAN tag, alias and the nodes where it's active. Same data was already available in the *Cluster* sheet but it lives natively here too now, so the network view is self-contained.

---

## [2.2.0] — 2026-05-11

### What's new

- **Resilient collection — broken endpoints no longer abort the report.** A single failing Proxmox API call (a storage with a corrupt RRD file, a node returning `500`, missing permissions on a sub-resource) used to terminate the whole report. Failures are now collected into a dedicated **Issues** page that lists every problem with severity, section, the full Proxmox error message and the API endpoint that returned it. `501 Not Implemented` (endpoint missing in older PVE versions) stays silent — everything else surfaces as a `Warning` you can act on. Thanks @janrenard for the report (#33).
- **Issues page — second tab after Summary / second link after Home.** The Issues page only appears when there is something to report: a dedicated sheet in Excel positioned right after `Summary`, and the second sidebar link after `Home` in HTML. Issue rows are hyperlinked back to the relevant detail page (the failing VM, node, storage, or the global section) so you can jump straight to the context where the failure happened.
- **Cover/Summary now shows a Description column.** Both formats list every section in the Contents table with a one-line description of what's inside — consistent between Excel and HTML. Issue counts and per-section durations are unchanged.

---

## [2.1.0] — 2026-05-10

### Breaking changes

- **All formats now produce a single `.zip`.** Excel output used to be a pair of loose files (`Report_*.xlsx` + `Report_*.svg`); it is now packed into one `Report_*.zip` containing `report.xlsx` and `network-diagram.svg`. Same logical content, more portable: easier to share, attach to email/tickets, archive, or upload as a CI artifact — and ready for the upcoming `--only` flag that will produce multiple workbooks per run. To use the workbook, extract the zip and open `report.xlsx` as before. The `--output` flag now always writes a `.zip`; the extension is appended automatically if missing.
- **Settings renamed — `Sheet` suffix dropped from section toggles.** With three rendering formats (Excel, HTML, JSON-in-progress), the `Sheet` suffix in settings names was misleading: these flags toggle a *section* of the report, not a spreadsheet sheet. Rename your existing `settings.json` if you maintain one:
  - `Cluster.IncludeSheet` → `Cluster.Include`
  - `Cluster.IncludeTasksSheet` → `Cluster.IncludeTasks`
  - `Node.IncludeReplicationSheet` → `Node.IncludeReplication`
  - `Guest.IncludeSnapshotsSheet` → `Guest.IncludeSnapshots`
  - `Guest.IncludeDisksSheet` → `Guest.IncludeDisks`
  - `Guest.IncludePartitionsSheet` → `Guest.IncludePartitions`
  - `Storage.IncludeContentSheet` → `Storage.IncludeContent`
  - `Storage.IncludeBackupsSheet` → `Storage.IncludeBackups`

### What's new

- **HTML — per-column filter.** Headers backed by short text, flag or hyperlinked values (`Node`, `VmId`, `Status`, `Type`, …) now carry a hover-revealed funnel icon. Click it to open a dedicated filter input under the column header — the global filter and the per-column filters combine with AND. The big winner is high-cardinality pages like *Snapshots* (~250 rows for 23 VMs) and *RRD Guests* (~1600 rows): drilling down to a single VM no longer requires scanning through false positives.
- **HTML — match-mode toggle (`~` / `=`).** Both the global filter and each per-column filter expose a small toggle on the left of the input. `~` is the existing case-insensitive *contains* match (default). `=` is *exact whole-cell* match — essential for short numeric ids like a VM id where `100` would otherwise also match `1014`, `260509100047`, IP suffixes, ports, etc. The toggle re-applies the filter immediately so you can flip mode mid-query.
- **HTML — three-state sort.** Clicking a sortable header now cycles `original → ascending → descending → original`. The third click restores the order the engine emitted (e.g. nodes by hostname, snapshots by date) instead of leaving the table sorted by whichever column you last touched.

### Fixes

- **Per-sheet Index restored on Excel detail sheets.** The clickable index of tables at the top of every node/VM/container detail sheet (lost during the 2.0.0 writers refactor) is back. It now uses a 2-column compact layout — much friendlier on resources with many tables (VM detail with QEMU agent, node detail with full disk/SMART/APT data) — and sits right after the resource identity block instead of at the very top of the sheet.
- **HTML — header alignment matches column type.** Numeric column headers (`VmId`, `Size GB`, `MaxCount`, …) are now right-aligned like their cells; flag headers are centred. A specificity bug introduced with the writers refactor was forcing every header to the left, breaking the visual axis on data-heavy pages.
- **HTML — global filter input on the cover page.** The "Contents" table on `index.html` lacked the `.table-scroll` wrapper used elsewhere, which collapsed the gap between the row count and the filter input above it. Wrapped consistently with the rest of the report.

### Documentation

- **Settings reference moved to a dedicated guide.** The annotated `settings.json` example, the skip-heavy-sections cheatsheet and the performance tuning notes (`MaxParallelRequests`, `ApiTimeout`, `QemuAgentTimeout`) are now in [`docs/settings.md`](docs/settings.md). The README keeps the profile comparison matrix inline (it's the decisional content readers actually want at a glance) and links to the full reference.
- **Per-format reference guides.** [`docs/format-xlsx.md`](docs/format-xlsx.md) and [`docs/format-html.md`](docs/format-html.md) now host the long-form details (sheet order, page layout, sidebar behaviour, export button, hyperlinking rules, …) so the README can stay focused on quickstart and overview.

---

## [2.0.1] — 2026-05-08

### Fixes

- **Reports no longer crash on VM/CT detail pages** — a leftover code path in v2.0.0 still failed when a VM or container had a minimal config without extra fields. The detail page now renders cleanly in those cases. Thanks again @janrenard for spotting it (#24).

---

## [2.0.0] — 2026-05-08

### What's new

- **HTML output** — pick `--format Html` at the command line to get the report as a self-contained zipped website. Extract the zip and open `index.html` in any browser — works fully offline, no server needed. Built for large clusters (tested with **2700+ VMs**), with a sidebar to navigate every node/VM/container, click-to-sort and per-table search, light/dark theme, and an **Export** button on every detail page that lets you share a single page as a stand-alone file (ready to email, paste into a ticket or drop on a wiki). The Excel output (`--format Xlsx`, the default) is unchanged.

### Fixes

- **Reports no longer crash on VMs with empty cloud-init values** — a VM whose config has an empty `cicustom: ` field used to abort the whole report. The failing VM is now skipped cleanly. Thanks @janrenard for the report (#24).
- **Reports no longer crash on user accounts with very long expiration dates** — accounts set to expire after the year 2038 caused the report to fail. All time-based fields (account expiry, scheduled next run, replication times, RRD timestamps, cluster log time…) now handle dates well into the future. Thanks @LordXearo for the report (#25).
- **Reports no longer crash when the firewall log is empty** — an empty firewall log used to abort the report. Empty logs (firewall, system journal, task log, replication log) now produce an empty section instead. Thanks @LordXearo for the report (#26).
- **Clearer error messages** — when a VM or container config fails to load, the error now identifies the offending guest (id, node and name) instead of a bare crash.

### Documentation

- README reorganised with a dedicated **Output Formats** section: Excel and HTML side-by-side, so you can pick the one that fits your workflow at a glance.

---

## [1.8.1] — 2026-05-04

### Fixes

- **Compatibility with older PVE clusters** — the `/cluster/mapping/dir` endpoint was added in newer Proxmox VE releases; older clusters (e.g. 8.3.x) returned `501 Not Implemented` and crashed the report. The fetch now degrades gracefully: the **Mapping Dir** sheet is simply empty when the endpoint isn't available, and the rest of the report is unaffected. Thanks @janrenard for the report (#20).

---

## [1.8.0] — 2026-04-15

### What's new

- **Network topology diagram** — every export now produces an SVG diagram next to the Excel file, with the same name. For each Proxmox node it shows the full network path from physical NICs to bonds, bridges, firewall/router VMs, internal bridges and the VMs/CTs they serve, plus a dedicated row for network storage (NFS, CIFS, PBS, iSCSI, Ceph, RBD, GlusterFS). Open it in any browser — colours and arrows make routing, multi-homed gateways and inactive interfaces immediately visible.

### Fixes

- **VM data more resilient** — when a single QEMU agent call fails (e.g. `get-fsinfo` on OPNsense), the rest of the agent data (hostname, OS info, network) is still collected instead of being lost.
- **Clearer error messages** — when the QEMU agent fails the `Hostname` column now shows the actual reason, not just "Agent not running".

### Documentation

- New page **[docs/network-diagram.md](docs/network-diagram.md)** with the legend, layout and how to read the diagram.
- A sample diagram is included in the repo at **[docs/network-diagram.svg](docs/network-diagram.svg)** (cluster anonymised with example data).
- README updated with a preview of the new diagram.

---

## [1.7.0] — 2026-04-13

### What's new

- **Host file in node detail** — the host name resolution table (`/etc/hosts`) is now included in each node's detail sheet, right after the network interfaces
- **Offline nodes** — nodes that are offline are now skipped gracefully instead of producing errors in the report

---

## [1.6.0] — 2026-04-11

### Fixes

- **Disks sheet** — storage type and usage now show correctly for VMs with disks spread across multiple storages
- **Nodes Networks sheet** — the Node column was always empty; it now shows the correct node for each network interface
- **SSL certificate expiry** — days until expiry no longer crashes for certificates with no expiry date
- **Replication sheet** — the Summary sheet now shows the correct number of replication jobs instead of the number of nodes
- **Cluster sheet** — fixed a duplicate API call when loading the Two-Factor Authentication table

### Performance

- **Firewall** — rules, aliases and IP sets are now fetched in parallel for both cluster and each VM/CT, reducing report generation time on large clusters
- **Replication** — replication status is now fetched from all nodes in parallel instead of one at a time
- **Storage Content** — content and backup lists are now fetched and written in a single parallel pass

---

## [1.5.0] — 2026-04-10

### What's new

- **Response files** — host, username and password can be saved in a `config.rsp` file and passed as `@config.rsp`, so you don't have to repeat credentials on every run
- **Generation stats in Summary** — the Summary sheet now shows how many rows each section produced and how long it took, plus a total duration at the bottom
- **`ApiTimeout` setting** — new option to set the HTTP timeout in seconds for slow or high-latency clusters; leave at `0` to use the default (100 s)
- **`QemuAgentTimeout` setting** — new option to control how long to wait for the QEMU guest agent before giving up; default is 3 seconds
- **Firewall log now opt-in per section** — `Node.Detail.IncludeFirewallLog` and `Guest.Detail.IncludeFirewallLog` let you enable firewall logs independently for nodes and VMs/CTs

### Fixes

- Firewall log no longer crashes the report when the API returns no data
- `Sys.Modify` permission added to the required permissions documentation (needed for APT repositories, updates and package versions)

### Performance

- **Nodes fetched in parallel** — all nodes are now queried at the same time (status, version, subscription, DNS, time, network), the same way VMs and containers already worked
- **Node detail queries parallelized** — for each node: services and SSL certificates, APT repositories/updates/versions, and directory/ZFS pools are now fetched simultaneously instead of one after another
- **Network data written once** — node network interface rows are collected during node processing and written to the Network sheet in a single pass, instead of incrementally

---

## [1.4.0] — 2026-04-08

### What's new

- **Custom settings** — generate a `settings.json` with `create-settings`, edit it, and pass it with `--settings-file` to fully control which sheets are generated
- **Cluster sheet can be disabled** — new `Cluster.IncludeSheet` flag to skip the Cluster sheet entirely
- **Per-node detail sheets can be disabled** — `Node.Detail.Enabled = false` skips all per-node detail sheets, useful on large clusters
- **Per-VM/CT detail sheets can be disabled** — `Guest.Detail.Enabled = false` skips all per-VM/CT detail sheets, the most impactful option for clusters with hundreds of VMs
- **Individual global sheets can be toggled** — `Guest.IncludeDisksSheet`, `Guest.IncludeSnapshotsSheet`, `Guest.IncludePartitionsSheet` to skip specific sheets

### Changes

- Settings restructured: node detail options moved under `Node.Detail` (disk, tasks, APT)
- Settings restructured: guest detail options moved under `Guest.Detail` (tasks)
- `MaxParallelRequests` unified to a single global setting (previously split per-section)
- `IncludeReplication` renamed to `IncludeReplicationSheet`, `IncludeTasks` renamed to `IncludeTasksSheet` for consistency
- Firewall settings simplified: `LogMaxCount` → `MaxCount`, `LogSince` → `Since`, `LogUntil` → `Until`
- **Fast profile** now disables detail sheets and firewall — significantly faster on large clusters
- README reorganized: profiles and customization are now front and center

---

## [1.3.0] — 2026-04-07

### What's new

- **Syslog** — new global sheet with syslog from all nodes combined in one place
- **Cluster Log** — cluster-wide event log (replaces "Audit Log")
- **Cluster Tasks** — new sheet with all cluster-level tasks
- **Firewall** — new global sheet with rules, aliases and IP sets across cluster, nodes, VMs and CTs
- **Replication** — new global sheet with all replication jobs and status
- **RRD Guests** — new global performance metrics sheet for VMs and CTs (disabled by default)
- **Storage Content / Backups** — storage files and backups moved to dedicated global sheets
- **Back links** — every node, VM and CT detail sheet now has a `← Back` link to its list
- **More hyperlinks** — RRD, Replication, Firewall, Storage Content and Backups sheets now have clickable node, VM and storage links

### Changes

- RRD data removed from individual VM/CT detail sheets — now in the global RRD Guests sheet
- Each section (Node, Storage, Guest) has its own parallel request limit for RRD data
- Settings reorganized: storage content options grouped under `Storage.Content`
- `Guest.RrdData` disabled by default — can produce very large sheets on big clusters

### Fixes

- Fixed a crash when generating the report with many sheets
- Fixed wrong column headers in Storage Content / Backups sheets in certain configurations
- Fixed empty rows in the Firewall sheet when a node or VM had no rules

---

## [1.2.0] — 2026-04-03

### What's new

- **Network sheet** — new dedicated sheet with a complete network inventory across the entire cluster: all node interfaces and all VM/CT network cards with MAC address, bridge, VLAN, IP addresses and OS info in one place
- **Disks sheet** — new dedicated sheet with VM/CT disk inventory: storage, size, cache, backup flag, unused flag, mount point, passthrough
- **VM IP addresses** — IP addresses from the QEMU agent are now visible directly in the VM overview and network tables
- **VM overview** — new columns: Networks (MAC + bridge/VLAN), IP Addresses, Hostname
- **VM network detail** — additional fields: Trunks, Disconnect, LinkDown, Gateway, Gateway6
- **VM disk detail** — additional fields: MountPoint, MountSourcePath, Passthrough
- **Node network detail** — full IPv6 fields now included: Cidr6, Address6, Netmask6, Gateway6, Method6
- **Node disks** — reorganized settings: `SettingsDisk` class with `Enabled`, `IncludeSmartData`, `IncludeZfs`, `IncludeDirectory`
- **Node detail** — new optional tables: Directory mount points, ZFS pools with vdev tree
- **APT Repositories** — new `IncludeAptRepositories` option to show configured APT repositories in node detail
- **Cluster Audit Log** — cluster event log can now be included in the Cluster sheet with `OnlyErrors` and `MaxCount` filters
- **SkipEmptyCollections** — new global setting to skip empty collections (e.g., no snapshots) from the report
- **README** — added "Where cv4pve-report fits" section with RVTools comparison table

### Changes

- Version bumped to 1.2.0
- Settings reorganization: disk-related settings moved from `SettingsNode` to dedicated `SettingsDisk` class
- Audit Log settings enhanced with `OnlyErrors` filter for severity-based filtering

### Fixes

- HA Groups correctly skipped on PVE 9 and later where the API endpoint was removed

---

## [1.1.0] — 2026-03-30

### What's new

- **HA (High Availability)** — the Cluster sheet now includes HA resources, groups and current status
- **Resource Pools** — the Cluster sheet now lists all pools with their members (VMs, CTs and storages)
- **SDN Ipams and Subnets** — SDN section now also covers IP address management and subnets per vnet
- **Syslog** — each node detail sheet can now include the system log (disabled by default, enabled in Full profile)
- **Firewall logs** — node and VM/CT firewall logs now support date range and line count filters
- **Task filters** — tasks on nodes and VMs/CTs can now be filtered by errors only, limited by count, and filtered by source (node only, all, active)
- **Full profile** — now includes syslog and firewall logs limited to the last 7 days (1000 lines max)

---

## [1.0.0] — 2026-03-27

### Initial release

- Export Proxmox VE infrastructure inventory to Excel (`.xlsx`)
- **Summary** sheet with report metadata, table of contents and link to GitHub
- **Cluster** sheet: status, options, users, API tokens, TFA, groups, roles, ACL, firewall rules/options, domains, backup jobs, replication, storages, metric servers, SDN zones/vnets/controllers, hardware mappings (dir/PCI/USB), resource pools with member list (VM/CT and storage)
- **Storages** sheet: storage list with links to per-storage detail sheets (content, RRD data)
- **Nodes** sheet: node list with links to per-node detail sheets (services, network, disks, SMART data, replication, RRD data, APT updates, package versions, firewall rules, SSL certificates, tasks)
- **Vms** sheet: VM/CT list with links to per-VM detail sheets (network, disks, RRD data, backups, snapshots, firewall, tasks, QEMU agent OS info/network/disks)
- QEMU agent data (hostname, OS info, network interfaces, filesystems) read once per VM — no duplicate API calls
- Agent status shown in overview: `Agent not enabled!`, `Agent not running!`, `Error Agent data!`
- `--fast` / `--full` as options on `export` and `create-settings` subcommands (not global)
- `--output|-o` option on `export` to specify output file path
- Filter support for nodes, VMs/CTs and storages (`@all`, comma-separated, wildcards, pools, tags, nodes, exclusions)
- Configurable RRD time frame and consolidation function
- Three profiles: Fast, Standard (default), Full
- Settings via `settings.json` (`create-settings` command)
- Fully navigable — every node, VM and storage is a clickable hyperlink
- Cross-platform (Windows, Linux, macOS)
- API-based, no root/SSH access required
