# cv4pve-report

```
     ______                _                      __
    / ____/___  __________(_)___ _   _____  _____/ /_
   / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
  / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
  \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

Report Tool for Proxmox VE (Made in Italy)
```

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-report.svg?style=flat-square)](LICENSE.md)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-report.svg?style=flat-square)](https://github.com/Corsinvest/cv4pve-report/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Corsinvest/cv4pve-report/total.svg?style=flat-square&logo=download)](https://github.com/Corsinvest/cv4pve-report/releases)
[![NuGet](https://img.shields.io/nuget/v/Corsinvest.ProxmoxVE.Report.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Corsinvest.ProxmoxVE.Report/)
[![WinGet](https://img.shields.io/winget/v/Corsinvest.cv4pve.report?style=flat-square&logo=windows)](https://winstall.app/apps/Corsinvest.cv4pve.report)
[![AUR](https://img.shields.io/aur/version/cv4pve-report?style=flat-square&logo=archlinux)](https://aur.archlinux.org/packages/cv4pve-report)

> **The RVTools for Proxmox VE** — exports your entire Proxmox VE infrastructure to a single Excel file.

**Fully navigable** — every node, VM and storage in the summary tables is a hyperlink to its dedicated detail sheet. Detail sheets have a clickable index to jump to any table inside. One click, no searching.

---

## Quick Start

```bash
wget https://github.com/Corsinvest/cv4pve-report/releases/download/VERSION/cv4pve-report-linux-x64.zip
unzip cv4pve-report-linux-x64.zip
./cv4pve-report --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD export
```

---

## Where cv4pve-report fits

RVTools is a pure inventory tool for VMware — it exports infrastructure data to Excel, nothing more. The cv4pve suite follows the Unix philosophy — each tool does one thing and does it well. Use them together for complete coverage.

| | RVTools | [**cv4pve-report**](https://github.com/Corsinvest/cv4pve-report) | [cv4pve-diag](https://github.com/Corsinvest/cv4pve-diag) |
|---|---------|:-----------------:|:-----------:|
| **Platform** | VMware vSphere | Proxmox VE | Proxmox VE |
| **Purpose** | Inventory & reporting | **Inventory & reporting** | **Diagnostics & health checks** |
| **Output** | Excel | Excel | Text / HTML / JSON / Markdown / Excel |

### Capabilities

| Feature | RVTools | [cv4pve-report](https://github.com/Corsinvest/cv4pve-report) | [cv4pve-diag](https://github.com/Corsinvest/cv4pve-diag) |
|---------|:-------:|:-------------:|:-----------:|
| VM / CT inventory | ✓ | ✓ | |
| Node / host inventory | ✓ | ✓ | |
| CPU / memory / disk details | ✓ | ✓ | |
| Network inventory (NICs, IPs, MACs) | ✓ | ✓ | |
| Storage / datastore inventory | ✓ | ✓ | |
| Snapshot inventory | ✓ | ✓ | |
| Snapshot with RAM state | ✓ | ✓ | |
| Resource pools | ✓ | ✓ | |
| Cluster configuration | ✓ | ✓ | |
| License / subscription inventory | ✓ | ✓ | |
| SSL certificates | | ✓ | |
| RRD metrics (CPU / memory / disk / net) | | ✓ | |
| Guest disk partitions (via agent) | ✓ | ✓ | |
| Guest OS info / hostname (via agent) | | ✓ | |
| SMART data per disk | | ✓ | |
| Backup job configuration | | ✓ | |
| Replication status | | ✓ | |
| HA configuration | | ✓ | |
| Firewall rules | | ✓ | |
| SDN zones / vnets | | ✓ | |
| Users / roles / ACL / TFA / API tokens | | ✓ | |
| APT packages / updates | | ✓ | |
| Syslog (all nodes, parsed into columns) | | ✓ | |
| Cluster log & cluster tasks | | ✓ | |
| Health checks & diagnostics | | | ✓ |

> **cv4pve-report** shows you *what* is in your infrastructure.
> **[cv4pve-diag](https://github.com/Corsinvest/cv4pve-diag)** tells you *what is wrong* with it.

---

## Features

- **Single `.xlsx` file** — global sheets plus a dedicated detail sheet per node, VM and container
- **Fully navigable** — summary rows link to detail sheets; detail sheets have a `← Back` link and a clickable index
- **Cluster** — users, API tokens, TFA, groups, roles, ACL, firewall options, domains, backup jobs, HA, SDN, pools
- **Nodes** — services, network, disks, SMART, ZFS, APT, SSL certificates, replication, syslog, firewall logs, tasks
- **VMs/CTs** — config, network, disks, snapshots, firewall logs, tasks, QEMU agent info
- **Global sheets** — Firewall (rules/aliases/ipsets), RRD Nodes, RRD Storage, RRD Guests, Syslog, Cluster Log, Cluster Tasks, Replication, Network, Disks, Partitions, Snapshots, Storage Content, Backups
- **Flexible filtering** — `@all`, pools, tags, nodes, ID ranges, wildcards, exclusions (same syntax as cv4pve-autosnap)
- **API token** support, cross-platform (Windows, Linux, macOS), no root access required

---

## Installation

| Platform | Command |
|----------|---------|
| **Linux** | `wget .../cv4pve-report-linux-x64.zip && unzip cv4pve-report-linux-x64.zip && chmod +x cv4pve-report` |
| **Windows WinGet** | `winget install Corsinvest.cv4pve.report` |
| **Windows manual** | Download `cv4pve-report-win-x64.zip` from [Releases](https://github.com/Corsinvest/cv4pve-report/releases) |
| **Arch Linux** | `yay -S cv4pve-report` |
| **Debian/Ubuntu** | `sudo dpkg -i cv4pve-report-VERSION-ARCH.deb` |
| **RHEL/Fedora** | `sudo rpm -i cv4pve-report-VERSION-ARCH.rpm` |
| **macOS** | `wget .../cv4pve-report-osx-x64.zip && unzip cv4pve-report-osx-x64.zip && chmod +x cv4pve-report` |

All binaries on the [Releases page](https://github.com/Corsinvest/cv4pve-report/releases).

---

## Security & Permissions

### Required Permissions

| Permission | Purpose | Scope |
|------------|---------|-------|
| **VM.Audit** | Read VM/CT configuration and status | Virtual machines |
| **Datastore.Audit** | Read storage content and metrics | Storage systems |
| **Pool.Audit** | Access pool information | Resource pools |
| **Sys.Audit** | Node system information, services, disks | Cluster nodes |

### API Token

```bash
cv4pve-report --host=pve.local --api-token=report@pve!token=uuid export
```

---

## Report Contents

### Sheet Order

| # | Sheet | Description |
|---|-------|-------------|
| 1 | **Summary** | Report metadata, filters, hyperlinked table of contents |
| 2 | **Cluster** | Cluster-wide configuration and security |
| 3 | **Nodes** | Node overview table → links to node detail sheets |
| 4 | **Vms** | VM overview table → links to VM detail sheets |
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


### Cluster Sheet

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

### Node Detail Sheet

Per-node sheet (linked from Nodes list), with `← Back` to Nodes:

| Table | Contents |
|-------|----------|
| Services | System service status |
| Network | Interface configuration with IPv4/IPv6, bond, VLAN, OVS details |
| Disks | Physical disk list *(if `Node.Disk.IncludeDiskDetail`)* |
| SMART Data | SMART attributes per disk *(if `Node.Disk.IncludeSmartData`)* |
| ZFS Pools / ZFS Pool Status | ZFS pool health, usage, vdev tree *(if `Node.Disk.IncludeDiskDetail`)* |
| Directory | Filesystem mount points *(if `Node.Disk.IncludeDiskDetail`)* |
| APT Repository / APT Update / Package Versions | APT info *(if `Node.IncludeApt`)* |
| Firewall Logs | Node firewall log *(if `Firewall.Enabled`)* |
| SSL Certificates | Certificate validity, expiry, fingerprint |
| Tasks | Recent task history *(if `Node.Tasks.Enabled`)* |

### VM / CT Detail Sheet

Per-VM/CT sheet (linked from Vms/Containers list), with `← Back` to list:

| Table | Contents |
|-------|----------|
| Agent OS Info | OS name, kernel, version *(QEMU agent, running VMs only)* |
| Agent Network | Network interfaces from QEMU agent |
| Agent Disks | Filesystems from QEMU agent |
| Network | Interface config from VM config |
| Disks | Disk list with storage, size, cache |
| Firewall Logs | VM/CT firewall log *(if `Firewall.Enabled`)* |
| Tasks | Recent task history *(if `Guest.Tasks.Enabled`)* |

### Global Sheets

**Network** — one table for node interfaces, one for VM/CT NICs (MAC, bridge, VLAN, IPs, model)

**Disks** — Storage Configuration (cluster-level), Storages (per-node usage), VM Disks (all VM/CT disks)

**Partitions** — guest disk partitions read via QEMU agent (node, VM ID, mount point, filesystem, size, used)

**Snapshots** — all snapshots across all VMs/CTs with RAM flag, size *(if available)*, date

**Firewall** — three tables: Rules, Aliases, IP Sets — each row has ScopeType (cluster/node/qemu/lxc), Scope, ScopeName *(if enabled)*

**Syslog** — one unified table, each row parsed: Node, Date, Time, Host, Service, PID, Message *(if enabled)*

**Cluster Log** — cluster event log with TimeDate, Node, User, Service, Severity, Message *(if enabled)*

**Cluster Tasks** — all recent tasks across the cluster with Node, Type, User, Status, StartTime, Duration *(if enabled)*

**RRD Nodes / RRD Storage / RRD Guests** — single table per sheet with resource identifier columns + time-series metrics *(if enabled)*

---

## Settings Reference

Generate the default settings file with:

```bash
cv4pve-report create-settings
```

<details>
<summary><strong>Full settings.json with all defaults</strong></summary>

```jsonc
{
  "Cluster": {
    "Log": {
      "Enabled": false,            // cluster event log
      "MaxCount": 0                // 0 = unlimited
    },
    "IncludeTasks": true           // cluster tasks sheet
  },
  "Node": {
    "Names": "@all",               // @all | pve1 | pve1,pve2 | pve*
    "RrdData": {
      "Enabled": true,
      "TimeFrame": "Day",          // Hour | Day | Week | Month | Year
      "Consolidation": "Average",  // Average | Maximum
      "MaxParallelRequests": 3
    },
    "Tasks": {
      "Enabled": true,
      "OnlyErrors": false,           // show only failed tasks
      "MaxCount": 500,
      "Source": "all"                // all | local | active
    },
    "Disk": {
      "IncludeDiskDetail": true,   // physical disks, ZFS, directory mount points
      "IncludeSmartData": false    // SMART attributes per disk (one API call per disk — slow)
    },
    "IncludeApt": true,            // APT repositories, available updates, installed packages
    "IncludeReplication": true,    // replication jobs global sheet
    "Syslog": {
      "Enabled": false,
      "MaxEntries": 500,
      "Since": null,               // DateOnly e.g. "2024-01-01"
      "Until": null
    }
  },
  "Guest": {
    "Ids": "@all",                 // see VM/CT Selection Patterns below
    "RrdData": {
      "Enabled": false,            // disabled by default — can be large on big clusters
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "MaxParallelRequests": 5
    },
    "Tasks": {
      "Enabled": true,
      "OnlyErrors": false,           // show only failed tasks
      "MaxCount": 500,
      "Source": "all"                // all | local | active
    },
    "Snapshots": {
      "Enabled": true,
      "MaxParallelRequests": 5
    },
    "IncludeQemuAgent": true       // OS info, network, filesystems (running VMs with agent only)
  },
  "Storage": {
    "Content": {
      "IncludeContent": true,      // storage content (ISO, templates, disk images)
      "IncludeBackups": true,      // backup files
      "MaxParallelRequests": 5
    },
    "RrdData": {
      "Enabled": true,
      "TimeFrame": "Day",
      "Consolidation": "Average",
      "MaxParallelRequests": 5
    }
  },
  "Firewall": {
    "Enabled": true,               // global firewall sheet (rules, aliases, IP sets) + firewall logs in detail sheets
    "LogMaxCount": 0,              // 0 = unlimited
    "LogSince": null,              // DateOnly e.g. "2024-01-01"
    "LogUntil": null,
    "MaxParallelRequests": 5
  }
}
```

</details>

---

<details>
<summary><strong>VM/CT Selection Patterns</strong></summary>

The `Guest.Ids` setting supports the same powerful pattern matching as [cv4pve-autosnap](https://github.com/Corsinvest/cv4pve-autosnap):

| Pattern | Syntax | Description | Example |
|---------|--------|-------------|---------|
| **All VMs** | `@all` | All VMs/CTs in cluster | `@all` |
| **Single ID** | `ID` | Specific VM/CT by ID | `100` |
| **Single Name** | `name` | Specific VM/CT by name | `web-server` |
| **Multiple** | `ID,ID,ID` | Comma-separated list | `100,101,102` |
| **ID Range** | `start:end` | Range of IDs (inclusive) | `100:110` |
| **Wildcard** | `%pattern%` | Name contains pattern | `%web%` |
| **By Node** | `@node-name` | All VMs on specific node | `@node-pve1` |
| **By Pool** | `@pool-name` | All VMs in pool | `@pool-production` |
| **By Tag** | `@tag-name` | All VMs with tag | `@tag-backup` |
| **Exclusion** | `-ID` or `-name` | Exclude specific VM | `@all,-100` |
| **Tag Exclusion** | `-@tag-name` | Exclude by tag | `@all,-@tag-test` |
| **Node Exclusion** | `-@node-name` | Exclude by node | `@all,-@node-pve2` |

```
@all                          # all VMs/CTs
100,101,102                   # specific IDs
100:200                       # IDs from 100 to 200
@pool-production              # all VMs in pool "production"
@tag-backup                   # all VMs tagged "backup"
@node-pve1                    # all VMs on node pve1
@all,-100,-101                # all except VM 100 and 101
@all,-@tag-test               # all except VMs tagged "test"
%web%                         # VMs whose name contains "web"
```

</details>

## Command Reference

<details>
<summary><strong>Full command reference</strong></summary>

```bash
cv4pve-report [global-options] [command]
```

#### Authentication
| Parameter | Description | Example |
|-----------|-------------|---------|
| `--host` | Proxmox host(s) | `--host=pve.local:8006` |
| `--username` | Username@realm | `--username=root@pam` |
| `--password` | Password or file | `--password=secret` or `--password=file:/path` |
| `--api-token` | API token | `--api-token=user@realm!token=uuid` |
| `--validate-certificate` | Validate SSL certificate | `false` |

#### Global Options
| Parameter | Description |
|-----------|-------------|
| `--settings-file` | Custom settings JSON file |

#### Commands

**`export`** — Generate Excel report

| Option | Description |
|--------|-------------|
| `--fast` | Fast profile (structure only, no heavy data) |
| `--full` | Full profile (everything, RRD on week timeframe) |
| `--output\|-o` | Output file path (default: `Report_YYYYMMDD_HHmmss.xlsx` in current directory) |

Profile priority: `--settings-file` > `--fast` / `--full` > standard (default)

```bash
cv4pve-report --host=pve.local --api-token=report@pve!token=uuid export
cv4pve-report --host=pve.local --api-token=report@pve!token=uuid export --fast
cv4pve-report --host=pve.local --api-token=report@pve!token=uuid export --full --output=/reports/infra.xlsx
cv4pve-report --host=pve.local --api-token=report@pve!token=uuid export --settings-file=my.json
```

**`create-settings`** — Create `settings.json` for the chosen profile

| Option | Description |
|--------|-------------|
| `--fast` | Fast profile |
| `--full` | Full profile |

```bash
cv4pve-report create-settings           # standard (default)
cv4pve-report create-settings --fast
cv4pve-report create-settings --full
```

</details>

---

## Profiles

| Profile | Use case | Speed |
|---------|----------|-------|
| **Fast** | Quick scan, large clusters, CI/CD | fastest |
| **Standard** | Daily reporting, balanced detail | medium |
| **Full** | Audit, compliance, capacity planning | slowest |

<details>
<summary><strong>Profiles comparison</strong></summary>

| Setting | Fast | Standard | Full |
|---------|:----:|:--------:|:----:|
| **Cluster** | | | |
| Log.Enabled | | | ✓ |
| Log.MaxCount | | — | 1000 |
| IncludeTasks | ✓ | ✓ | ✓ |
| **Node** | | | |
| Disk.IncludeDiskDetail | | ✓ | ✓ |
| Disk.IncludeSmartData | | | ✓ |
| IncludeApt | | ✓ | ✓ |
| IncludeReplication | ✓ | ✓ | ✓ |
| Tasks.Enabled | | ✓ | ✓ |
| Syslog.Enabled | | | ✓ |
| Syslog.MaxEntries | | — | 1000 |
| Syslog.Since | | — | last 3 days |
| RrdData.Enabled | | ✓ | ✓ |
| RrdData.TimeFrame | | Day | Week |
| **Guest** | | | |
| Snapshots.Enabled | | ✓ | ✓ |
| IncludeQemuAgent | | ✓ | ✓ |
| Tasks.Enabled | | ✓ | ✓ |
| RrdData.Enabled | | | ✓ |
| RrdData.TimeFrame | | — | Week |
| **Storage** | | | |
| Content.IncludeContent | | ✓ | ✓ |
| Content.IncludeBackups | | ✓ | ✓ |
| RrdData.Enabled | | ✓ | ✓ |
| RrdData.TimeFrame | | Day | Week |
| **Firewall** | | | |
| Enabled | | ✓ | ✓ |
| LogMaxCount | | 0 | 1000 |
| LogSince | | — | last 3 days |

</details>

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
