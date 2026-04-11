# Changelog

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
