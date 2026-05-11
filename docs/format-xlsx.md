# Excel format reference (`--format Xlsx`)

`--format Xlsx` (the default) produces a `.zip` containing a workbook with one sheet per section (plus per-resource detail sheets at the end) and the network topology diagram as a separate `.svg`.

Extract the zip and open `report.xlsx` in Excel, LibreOffice Calc or any spreadsheet tool. Open `network-diagram.svg` in any browser.

---

## File layout inside the zip

```
Report_20260506_120000.zip
├── report.xlsx              ← single workbook with one sheet per section
└── network-diagram.svg      ← network topology diagram
```

---

## Sections

Sheets are written in this order. Conditional sheets (`if …`) are only present when the matching setting is enabled or when the underlying data exists.

| # | Sheet | Description | Condition |
|---|-------|-------------|-----------|
| 1 | **Summary** | Report metadata, filters, hyperlinked table of contents | always |
| 2 | **Issues** | Diagnostics for collection failures — see [Issues](#issues) below | only when failures recorded |
| 3 | **Cluster** | Cluster-wide configuration and security (users, ACL, firewall options, backup jobs, HA, SDN, pools, hardware mappings) | `Cluster.Include` |
| 4 | **Nodes** | Node overview → links to per-node detail sheets | always |
| 5 | **VMs** | VM overview → links to per-VM detail sheets | always |
| 6 | **Containers** | Container overview → links to per-CT detail sheets | always |
| 7 | **Network** | Node interfaces + VM/CT NICs (MAC, bridge, VLAN, IPs, model) | always |
| 8 | **Storages** | Storage list with size, usage, type | always |
| 9 | **Storage Content** | Storage files/images with size and VM ID links | `Storage.IncludeContent` |
| 10 | **Backups** | Backup files across all storages | `Storage.IncludeBackups` |
| 11 | **Disks** | Global VM/CT disk inventory | `Guest.IncludeDisks` |
| 12 | **Partitions** | Guest disk partitions via QEMU agent | `Guest.IncludePartitions` |
| 13 | **Snapshots** | Global snapshot inventory across all VMs/CTs | `Guest.IncludeSnapshots` |
| 14 | **Firewall** | Cluster + node + VM/CT firewall rules, aliases, IP sets | `Firewall.Enabled` |
| 15 | **Replication** | Replication job status across all nodes | `Node.IncludeReplication` |
| 16 | **RRD Nodes** | Historical performance metrics per node | `Node.RrdData.Enabled` |
| 17 | **RRD Storage** | Historical performance metrics per storage | `Storage.RrdData.Enabled` |
| 18 | **RRD Guests** | Historical performance metrics per VM/CT | `Guest.RrdData.Enabled` |
| 19 | **Syslog** | Parsed systemd journal across all nodes | `Node.Syslog.Enabled` |
| 20 | **Cluster Log** | Cluster event log | `Cluster.Log.Enabled` |
| 21 | **Cluster Tasks** | Recent tasks across the cluster | `Cluster.IncludeTasks` |
| … | **Node `<name>`** | Per-node detail (services, network, disks, SMART, ZFS, APT, certificates, tasks) | `Node.Detail.Enabled` |
| … | **VM `<id>`** | Per-VM detail (agent OS info, network, disks, firewall logs, tasks) | `Guest.Detail.Enabled` |
| … | **CT `<id>`** | Per-CT detail (same as VM) | `Guest.Detail.Enabled` |

> The contents of each sheet (which tables, which columns) are emitted by the engine and identical across formats — see [`docs/settings.md`](settings.md) for the toggles.

---

## Issues

When one or more Proxmox API calls fail during collection (a corrupt RRD file, a `500` from a node, missing permissions on a sub-resource, …), an extra **Issues** sheet is emitted as the **second** tab right after `Summary` and is also listed at the **top** of the Contents table on Summary itself. On a healthy cluster the sheet (and the Contents row) are absent.

| Column | Content |
|---|---|
| Severity | `Warning` (default) — `501 Not Implemented` is silent and never appears here. |
| Section | The logical section where the failure happened (e.g. `RRD Storage`, `Firewall Log`, `Cluster`). **Hyperlinked** to the relevant sheet — VM / Node / Storage / global section — so you can jump from the error to its context. |
| Message | Diagnostic line built from the Proxmox response: HTTP status, the API error body and the failing endpoint, joined with `—`. Example: `500 Internal Server Error — got wrong time resolution (60 != 1800) — GET /nodes/orion/storage/local-ceph/rrddata`. |
| Timestamp | When the failure was recorded. |

---

## Excel-specific behaviour

- **Hyperlinks everywhere** — click a node, VM or storage in any list to jump straight to its detail sheet. Every detail sheet has a `← Back` link to its overview.
- **Per-sheet Index** — each detail sheet starts with a clickable index of its tables so you can jump within the sheet (2-column compact layout for resources with many tables).
- **Native filtering / sorting / pivot** — every table is a real Excel table: built-in autofilter, sort, slicer and pivot work without exporting elsewhere.
- **Sheet name length** — Excel limits sheet names to 31 characters. Long names (e.g. `Node verylonghostname.example.com`) are automatically truncated; cross-sheet hyperlinks are rewritten to point at the truncated name.
- **Network topology** — bundled in the same zip as `network-diagram.svg`; open it in any browser. See the [network diagram guide](network-diagram.md) for the legend and layout.
