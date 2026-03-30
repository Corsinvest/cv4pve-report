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

## Features

- **Single `.xlsx` file** — one sheet per node, VM and storage
- **Fully navigable** — summary rows link to detail sheets; detail sheets have a clickable index
- **Cluster** — users, API tokens, TFA, groups, roles, ACL, firewall, domains, backup jobs, replication
- **Nodes** — services, network, disks, SMART, replication, RRD metrics, APT updates, firewall, SSL certificates, tasks
- **VMs/CTs** — config, network, disks, RRD metrics, backups, snapshots, firewall, tags, tasks, QEMU agent info
- **Storages** — content inventory, RRD metrics
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

### Navigation

Every resource reference in the report is a clickable hyperlink:

| Where | What is linked |
|-------|----------------|
| Summary sheet | All sheets (nodes, VMs, storages, cluster) |
| Nodes sheet | Each node row → node detail sheet |
| VMs sheet | Each VM/CT row → VM detail sheet; Node column → node detail sheet |
| Storages sheet | Each storage row → storage detail sheet; Node column → node detail sheet |
| Node detail | Index at top → each table in the sheet; Replication Guest/Source/Target → VM and node sheets |
| VM detail | Index at top → each table in the sheet; Node → node detail sheet |
| Storage detail | Index at top → each table in the sheet; Content VM ID column → VM detail sheet |
| Tasks tables | VM ID column → VM detail sheet |

### Summary Sheet

- Report generation timestamp
- Filters applied (nodes, VMs, storages, RRD settings)
- Hyperlinked table of contents to all sheets

### Cluster Sheet

| Table | Contents |
|-------|----------|
| Status | Cluster nodes, quorum, IP addresses, versions, support level |
| Users | User list with expiry dates |
| API Tokens | Token list with expiry dates |
| Two-Factor Authentication | TFA type and count per user |
| Groups | Group membership |
| Roles | Role privileges |
| ACL | Access control entries |
| Firewall Rules | Cluster-level rules |
| Firewall Options | Global firewall policy |
| Domains | Authentication realms |
| Backup | Backup job configuration |
| Replication | Replication job configuration |
| Pools | Resource pools with member list (VM/CT and storage) |

### Storages Sheet

Overview table → dedicated sheet per storage containing:
- **Content** — all files/images with size, metadata and VM ID links
- **RRD Data** — usage over time *(if enabled)*

### Nodes Sheet

Overview table → dedicated sheet per node containing:
- **Services** — system service status
- **Network** — interface configuration
- **Disks** — physical disk list with health
- **SMART Data** — SMART attributes per disk *(if enabled)*
- **Replication** — per-node replication jobs with links to source/target nodes and VMs
- **RRD Data** — CPU, memory, network, disk metrics over time *(if enabled)*
- **Apt Update** — available package updates
- **Package Version** — installed package versions
- **Firewall Rules** — node-level rules *(if enabled)*
- **Firewall Logs** — node firewall log *(if enabled)*
- **SSL Certificates** — certificate validity and expiry
- **Tasks** — recent task history with VM ID links *(if enabled)*
- **Syslog** — system log *(if enabled)*

### VMs Sheet

Overview table → dedicated sheet per VM/CT containing:
- **Agent OS Info** — OS name, kernel, version from QEMU agent *(if agent active)*
- **Agent Network** — network interfaces reported by QEMU agent *(if agent active)*
- **Agent Disks** — filesystems reported by QEMU agent *(if agent active)*
- **Network** — interface configuration from VM config
- **Disks** — disk list with storage and size
- **RRD Data** — CPU, memory, network, disk metrics over time *(if enabled)*
- **Backup** — backup files found in all storages
- **Snapshots** — snapshot list
- **Firewall Rules** — VM-level rules *(if enabled)*
- **Firewall Logs** — VM firewall log *(if enabled)*
- **Tasks** — recent task history *(if enabled)*

---

## Settings Reference

Generate the default settings file with:

```bash
cv4pve-report create-settings
```

Full `settings.json` structure with all defaults:

```jsonc
{
  "Cluster": {
    "IncludeOptions": true,        // cluster options and configuration
    "IncludeSecurity": true,       // users, API tokens, TFA, groups, roles, ACL, domains
    "IncludeFirewall": true,       // cluster-level firewall rules and options
    "IncludeBackupJobs": true,     // scheduled backup job configuration
    "IncludeReplication": true,    // replication job configuration
    "IncludeStorages": true,       // cluster-level storage list
    "IncludeMetricServers": true,  // metric server configuration
    "IncludeSdn": true,            // SDN zones, vnets and controllers
    "IncludeMapping": true,        // hardware mappings (directory, PCI, USB)
    "IncludePools": true,          // resource pools with member list
    "IncludeHa": true              // HA resources, groups and status
  },
  "Node": {
    "Names": "@all",               // @all | pve1 | pve1,pve2 | pve*
    "RrdData": {
      "Enabled": true,
      "TimeFrame": "Day",          // Hour | Day | Week | Month | Year
      "Consolidation": "Average"   // Average | Maximum
    },
    "Tasks": {
      "Enabled": true,
      "OnlyErrors": false,         // show only failed tasks
      "MaxCount": 0,               // 0 = unlimited
      "Source": "all"              // all | local | active
    },
    "Firewall": {
      "Enabled": true,
      "LogMaxCount": 0,            // 0 = unlimited
      "LogSince": null,            // DateOnly e.g. "2024-01-01"
      "LogUntil": null
    },
    "Syslog": {
      "Enabled": false,
      "MaxCount": 500,
      "Service": "",               // filter by service e.g. pvedaemon
      "Since": null,               // DateOnly e.g. "2024-01-01"
      "Until": null
    },
    "IncludeNetwork": true,        // network interface configuration
    "IncludeDisks": true,          // physical disk list
    "IncludeSmartData": true,      // SMART health data per disk (one API call per disk)
    "IncludeServices": true,       // system service status
    "IncludeSslCertificates": true,// SSL certificate validity and expiry
    "IncludeAptUpdates": true,     // available APT package updates
    "IncludeAptVersions": true,    // installed APT package versions
    "IncludeReplication": true     // per-node replication jobs
  },
  "Guest": {
    "Ids": "@all",                 // see VM/CT Selection Patterns below
    "RrdData": {
      "Enabled": true,
      "TimeFrame": "Day",          // Hour | Day | Week | Month | Year
      "Consolidation": "Average"   // Average | Maximum
    },
    "Tasks": {
      "Enabled": true,
      "OnlyErrors": false,         // show only failed tasks
      "MaxCount": 0                // 0 = unlimited
    },
    "Firewall": {
      "Enabled": true,
      "LogMaxCount": 0,            // 0 = unlimited
      "LogSince": null,            // DateOnly e.g. "2024-01-01"
      "LogUntil": null
    },
    "IncludeBackups": true,        // backup files found in all storages
    "IncludeSnapshots": true,      // snapshot list
    "IncludeQemuAgent": true       // OS info, network interfaces, filesystems (only running QEMU VMs with agent enabled)
  },
  "Storage": {
    "Names": "@all",               // @all | local | local*
    "RrdData": {
      "Enabled": true,
      "TimeFrame": "Day",          // Hour | Day | Week | Month | Year
      "Consolidation": "Average"   // Average | Maximum
    }
  }
}
```

---

## VM/CT Selection Patterns

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

**Examples:**

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

---

## Command Reference

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

---

## Profiles

| Profile | Use case | Speed |
|---------|----------|-------|
| **Fast** | Quick scan, large clusters, CI/CD | fastest |
| **Standard** | Daily reporting, balanced detail | medium |
| **Full** | Audit, compliance, capacity planning | slowest |

| Setting | Fast | Standard | Full |
|---------|:----:|:--------:|:----:|
| **Cluster** | | | |
| IncludeOptions | ✓ | ✓ | ✓ |
| IncludeSecurity | ✓ | ✓ | ✓ |
| IncludeFirewall | | ✓ | ✓ |
| IncludeBackupJobs | ✓ | ✓ | ✓ |
| IncludeReplication | ✓ | ✓ | ✓ |
| IncludeStorages | ✓ | ✓ | ✓ |
| IncludeMetricServers | | ✓ | ✓ |
| IncludeSdn | | ✓ | ✓ |
| IncludeMapping | | ✓ | ✓ |
| IncludePools | ✓ | ✓ | ✓ |
| IncludeHa | ✓ | ✓ | ✓ |
| **Node** | | | |
| IncludeNetwork | ✓ | ✓ | ✓ |
| IncludeDisks | ✓ | ✓ | ✓ |
| IncludeSmartData | | | ✓ |
| IncludeServices | | ✓ | ✓ |
| IncludeReplication | ✓ | ✓ | ✓ |
| Firewall.Enabled | | ✓ | ✓ |
| Firewall.LogMaxCount | | 0 | 1000 |
| Firewall.LogSince | | — | last 7 days |
| IncludeSslCertificates | | ✓ | ✓ |
| IncludeAptUpdates | | ✓ | ✓ |
| IncludeAptVersions | | | ✓ |
| Tasks.Enabled | | ✓ | ✓ |
| Tasks.OnlyErrors | | false | false |
| Tasks.MaxCount | | 0 | 0 |
| Tasks.Source | | all | all |
| Syslog.Enabled | | | ✓ |
| Syslog.MaxCount | | — | 1000 |
| Syslog.Since | | — | last 7 days |
| RrdData | | ✓ | ✓ (Week) |
| **Guest** | | | |
| Firewall.Enabled | | ✓ | ✓ |
| Firewall.LogMaxCount | | 0 | 1000 |
| Firewall.LogSince | | — | last 7 days |
| IncludeSnapshots | | ✓ | ✓ |
| IncludeBackups | | ✓ | ✓ |
| IncludeQemuAgent | | ✓ | ✓ |
| Tasks.Enabled | | ✓ | ✓ |
| Tasks.OnlyErrors | | false | false |
| Tasks.MaxCount | | 0 | 0 |
| RrdData | | ✓ | ✓ (Week) |
| **Storage** | | | |
| RrdData | | ✓ | ✓ (Week) |

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
