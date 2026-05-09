# Excel format reference (`--format Xlsx`)

`--format Xlsx` (the default) produces a single `.xlsx` workbook with one sheet per section, plus per-resource detail sheets at the end. The network topology diagram is written next to the workbook as a separate `.svg`.

```
Report_20260506_120000.xlsx   ← single workbook with one sheet per section
Report_20260506_120000.svg    ← network topology diagram (next to the .xlsx)
```

Open the `.xlsx` in Excel, LibreOffice Calc or any spreadsheet tool. Open the `.svg` in any browser.

---

## Sheet order

| # | Sheet | Description |
|---|-------|-------------|
| 1 | **Summary** | Report metadata, filters, hyperlinked table of contents |
| 2 | **Cluster** | Cluster-wide configuration and security |
| 3 | **Nodes** | Node overview table → links to node detail sheets |
| 4 | **VMs** | VM overview table → links to VM detail sheets |
| 5 | **Containers** | Container overview table → links to CT detail sheets |
| 6 | **Disks** | Global physical disk inventory across all nodes |
| 7 | **Partitions** | Guest disk partitions via QEMU agent |
| 8 | **Snapshots** | Global snapshot inventory across all VMs/CTs |
| 9 | **Network** | Global network inventory (node interfaces + VM/CT NICs) |
| 10 | **Storages** | Storage overview |
| 11 | **Storage Content** | All storage files/images with size and VM ID links *(if enabled)* |
| 12 | **Backups** | All backup files across all storages *(if enabled)* |
| 13 | **Firewall** | Global firewall rules, aliases and IP sets *(if enabled)* |
| 14 | **Replication** | Global replication job status *(if enabled)* |
| 15 | **RRD Nodes** | Historical metrics for all nodes *(if enabled)* |
| 16 | **RRD Storage** | Historical metrics for all storages *(if enabled)* |
| 17 | **RRD Guests** | Historical metrics for all VMs/CTs *(if enabled)* |
| 18 | **Syslog** | Parsed systemd journal across all nodes *(if enabled)* |
| 19 | **Cluster Log** | Cluster event log *(if enabled)* |
| 20 | **Cluster Tasks** | All recent tasks across the cluster *(if enabled)* |
| … | **Node `<name>`** | Per-node detail sheets (at end) |
| … | **VM `<id>`** | Per-VM detail sheets (at end) |
| … | **CT `<id>`** | Per-CT detail sheets (at end) |

---

## Cluster sheet

| Table | Contents |
|-------|----------|
| Status | Nodes, quorum, IP addresses, versions, support level |
| Users | User list with expiry dates |
| API Tokens | Token list with expiry dates |
| Two-Factor Authentication | TFA type per user |
| Groups | Group membership |
| Roles | Role privileges |
| ACL | Access control entries |
| Firewall Options | Global firewall policy |
| Domains | Authentication realms |
| Backup Jobs | Scheduled backup job configuration |
| HA Resources / Groups / Status | High Availability configuration |
| Metric Servers | External metric server configuration |
| SDN Zones / VNets / Controllers | Software-defined networking |
| Hardware Mappings | Directory, PCI and USB mappings |
| Pools | Resource pools with member list |

---

## Node detail sheet

Per-node sheet (linked from the Nodes list), with `← Back` to Nodes. Skipped entirely if `Node.Detail.Enabled = false`.

| Table | Contents |
|-------|----------|
| Services | System service status |
| Network | Interface configuration with IPv4/IPv6, bond, VLAN, OVS details |
| /etc/hosts | Host name resolution entries |
| Disks | Physical disk list *(if `Node.Detail.Disk.IncludeDiskDetail`)* |
| SMART Data | SMART attributes per disk *(if `Node.Detail.Disk.IncludeSmartData`)* |
| ZFS Pools / ZFS Pool Status | ZFS pool health, usage, vdev tree *(if `Node.Detail.Disk.IncludeDiskDetail`)* |
| Directory | Filesystem mount points *(if `Node.Detail.Disk.IncludeDiskDetail`)* |
| APT Repository / APT Update / Package Versions | APT info *(if `Node.Detail.IncludeApt`)* |
| Firewall Logs | Node firewall log *(if `Firewall.Enabled`)* |
| SSL Certificates | Certificate validity, expiry, fingerprint |
| Tasks | Recent task history *(if `Node.Detail.Tasks.Enabled`)* |

---

## VM / CT detail sheet

Per-VM/CT sheet (linked from VMs/Containers list), with `← Back` to list. Skipped entirely if `Guest.Detail.Enabled = false`.

| Table | Contents |
|-------|----------|
| Agent OS Info | OS name, kernel, version *(QEMU agent, running VMs only)* |
| Agent Network | Network interfaces from QEMU agent |
| Agent Disks | Filesystems from QEMU agent |
| Network | Interface config from VM config |
| Disks | Disk list with storage, size, cache |
| Firewall Logs | VM/CT firewall log *(if `Firewall.Enabled`)* |
| Tasks | Recent task history *(if `Guest.Detail.Tasks.Enabled`)* |

---

## Global sheets

**Network** — one table for node interfaces, one for VM/CT NICs (MAC, bridge, VLAN, IPs, model).

**Disks** — Storage Configuration (cluster-level), Storages (per-node usage), VM Disks (all VM/CT disks).

**Partitions** — guest disk partitions read via QEMU agent (node, VM ID, mount point, filesystem, size, used).

**Snapshots** — all snapshots across all VMs/CTs with RAM flag, size *(if available)*, date.

**Firewall** — three tables: Rules, Aliases, IP Sets — each row has ScopeType (cluster/node/qemu/lxc), Scope, ScopeName *(if enabled)*.

**Syslog** — one unified table, each row parsed: Node, Date, Time, Host, Service, PID, Message *(if enabled)*.

**Cluster Log** — cluster event log with TimeDate, Node, User, Service, Severity, Message *(if enabled)*.

**Cluster Tasks** — all recent tasks across the cluster with Node, Type, User, Status, StartTime, Duration *(if enabled)*.

**RRD Nodes / RRD Storage / RRD Guests** — single table per sheet with resource identifier columns + time-series metrics *(if enabled)*.

---

## Excel-specific behaviour

- **Hyperlinks everywhere** — click a node, VM or storage in any list to jump straight to its detail sheet; every detail sheet has a `← Back` link to its list.
- **Per-sheet index** — every detail sheet starts with a clickable index of its tables so you can jump to the section you need.
- **Native filtering / sorting / pivot** — every table is a real Excel table; use built-in autofilter, sort, slicer or pivot the data without exporting elsewhere.
- **Sheet name length** — Excel limits sheet names to 31 characters. Long names (e.g. `Node verylonghostname.example.com`) are automatically truncated; cross-sheet hyperlinks are rewritten to point at the truncated name.
