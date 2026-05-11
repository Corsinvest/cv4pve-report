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

> **The RVTools for Proxmox VE** — exports your entire Proxmox VE infrastructure as a single [Excel workbook](#excel-output---format-xlsx-default) **or** a [self-contained HTML site](#html-output---format-html), plus a network topology diagram (SVG).

**Fully navigable** — every node, VM and storage in the overview tables is a hyperlink to its dedicated detail page. Click and you're there.

**Network Diagram** — each export also produces an SVG showing the full network topology per node (physical NICs → bonds → bridges → gateway VMs → internal bridges → leaf VMs) plus a dedicated strip for network-backed storage. Open it in any browser — see the [guide](docs/network-diagram.md) and [sample](docs/network-diagram.svg).

**Resilient by design** — a single broken endpoint (a storage with a corrupt RRD file, a node returning 500, missing permissions on a sub-resource) no longer aborts the report. Failed calls are collected into a dedicated **Issues** page that lists severity, section, the full Proxmox error message and the API endpoint that failed — each row is hyperlinked to the relevant detail page (VM, node, cluster, …). 501 *Not Implemented* on endpoints missing in older PVE versions is silent, everything else surfaces as a `Warning` you can act on. The Issues page only appears when there is something to report.

<p align="center">
  <a href="docs/network-diagram.svg"><img src="docs/network-diagram.png" alt="Network Diagram preview" width="75%"></a>
</p>

---

## Where cv4pve-report fits

RVTools is a pure inventory tool for VMware — it exports infrastructure data to Excel, nothing more. The cv4pve suite follows the Unix philosophy — each tool does one thing and does it well. Use them together for complete coverage.

| | RVTools | [**cv4pve-report**](https://github.com/Corsinvest/cv4pve-report) | [cv4pve-diag](https://github.com/Corsinvest/cv4pve-diag) |
|---|---------|:-----------------:|:-----------:|
| **Platform** | VMware vSphere | Proxmox VE | Proxmox VE |
| **Purpose** | Inventory & reporting | **Inventory & reporting** | **Diagnostics & health checks** |
| **Output** | Excel | [Excel](#excel-output---format-xlsx-default) **or** [static HTML site](#html-output---format-html), plus [SVG network diagram](docs/network-diagram.md) | Text / HTML / JSON / Markdown / Excel |

### Capabilities

| Feature | RVTools | [cv4pve-report](https://github.com/Corsinvest/cv4pve-report) | [cv4pve-diag](https://github.com/Corsinvest/cv4pve-diag) |
|---------|:-------:|:-------------:|:-----------:|
| VM / CT inventory | ✓ | ✓ | |
| Node / host inventory | ✓ | ✓ | |
| CPU / memory / disk details | ✓ | ✓ | |
| Network inventory (NICs, IPs, MACs) | ✓ | ✓ | |
| [Network topology diagram (SVG)](docs/network-diagram.md) | | ✓ | |
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
| Resilient collection (skip & report broken endpoints) | | ✓ | |
| Health checks & diagnostics | | | ✓ |

> **cv4pve-report** shows you *what* is in your infrastructure.
> **[cv4pve-diag](https://github.com/Corsinvest/cv4pve-diag)** tells you *what is wrong* with it.

---

## Quick Start

```bash
wget https://github.com/Corsinvest/cv4pve-report/releases/download/VERSION/cv4pve-report-linux-x64.zip
unzip cv4pve-report-linux-x64.zip
./cv4pve-report --host=YOUR_HOST --username=root@pam --password=YOUR_PASSWORD export
```

With API token (recommended):

```bash
./cv4pve-report --host=YOUR_HOST --api-token=user@realm!token=uuid export
```

Pick the output format that fits your workflow:

```bash
./cv4pve-report ... export                  # Excel (default)
./cv4pve-report ... export --format Html    # HTML zipped site
```

With `--output` / `-o` you choose the output path. **All formats now produce a single `.zip`** — extract it to access the files inside (a `.xlsx` for Excel, an `index.html` plus assets for HTML). The network topology SVG (`network-diagram.svg`) is bundled in the same zip. If the path you pass doesn't end in `.zip`, the extension is appended automatically.

---

## Output Formats

Both formats expose **the same data** using the same logical layout — one section per topic (Cluster, Nodes, VMs, Containers, Storages, …) plus per-resource detail (one per node, VM and container). Only the rendering differs: a sheet per section in Excel, a page per section in HTML. Pick the one that matches what you need to do with the report — each format has its own full reference under [`docs/`](docs/).

### Excel output `(--format Xlsx, default)`

```
Report_20260506_120000.zip    ← contains report.xlsx + network-diagram.svg
```

For analysts and capacity planning. Extract the zip and open `report.xlsx` in Excel, LibreOffice Calc or any spreadsheet tool.

- **One workbook**, one sheet per section (Cluster, Nodes, VMs, Containers, Storages, …) plus per-node and per-VM detail sheets at the end
- **Hyperlinks everywhere** — click a node, VM or storage in any list to jump straight to its detail sheet; every detail sheet has a `← Back` link to its overview
- **Per-sheet index** — every detail sheet starts with a clickable index of its tables so you can jump to the section you need
- **Native filtering / sorting / pivot** — every table is a real Excel table; use built-in autofilter, sort, slicer or pivot the data without exporting elsewhere
- **Network topology SVG** — bundled inside the zip as `network-diagram.svg`; open in any browser to see the full network map (NICs → bonds → bridges → VMs → storages) — [guide](docs/network-diagram.md)

> Full reference: **[Excel format guide](docs/format-xlsx.md)** — sheet order, per-sheet contents, hyperlinks and Excel-specific behaviour.

### HTML output `(--format Html)`

```
Report_20260506_120000.zip    ← static website (extract and open index.html)
```

For sharing on a wiki, ticket system or with non-technical stakeholders. Extract the zip and open `index.html` in any browser — works fully offline, no server needed.

- **Static website** — `index.html` plus one page per section, all linked together; navigate with the sidebar like any wiki
- **Search & sort** — global sidebar filter (find any node, VM or container by id or name), per-table search and click-to-sort headers on every column
- **Light & dark theme** — toggle in the top corner; auto-follows your OS preference
- **Share a single page** — every node/VM/container detail page has an Export button (top right) that bundles the page as a stand-alone `.html` file (CSS and table interactions inline) — paste it into a ticket or attach it to an email without sending the whole report
- **Built for large clusters** — sidebar groups (Nodes, VMs, Containers) load their entries on demand, so the report stays fast and the zip stays small even with thousands of guests; tested up to 2700+ VMs
- **Print-friendly** — dedicated print stylesheet hides chrome and lays tables out for paper / PDF export from the browser
- **Network topology SVG** — bundled inside the zip and accessible from the sidebar — [guide](docs/network-diagram.md)

> Full reference: **[HTML format guide](docs/format-html.md)** — page layout, sidebar behaviour, theme, export button and lazy-loading for large clusters.

---

## Response Files

Arguments can be stored in a response file and referenced with `@filename`. This is useful to avoid repeating connection parameters on every run.

```text
# config.rsp
--host
192.168.1.1
--api-token
user@pam!report=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

```bash
cv4pve-report @config.rsp export
cv4pve-report @config.rsp --settings-file=settings.json export
cv4pve-report @config.rsp export --full
```

- One token per line (option name and value on separate lines)
- Lines starting with `#` are comments
- Response files can be nested: a line starting with `@` references another file

---

## Profiles

| Profile | Use case | Speed |
|---------|----------|-------|
| **Fast** | Quick scan, large clusters, CI/CD | fastest |
| **Standard** | Daily reporting, balanced detail | medium |
| **Full** | Audit, compliance, capacity planning | slowest |

```bash
cv4pve-report --host=YOUR_HOST --api-token=user@realm!token=uuid export           # Standard (default)
cv4pve-report --host=YOUR_HOST --api-token=user@realm!token=uuid export --fast    # Fast
cv4pve-report --host=YOUR_HOST --api-token=user@realm!token=uuid export --full    # Full
```

### Profiles comparison

<details>
<summary>Click to expand the per-flag matrix</summary>

| Setting | Fast | Standard | Full |
|---------|:----:|:--------:|:----:|
| **Cluster** | | | |
| Include | ✓ | ✓ | ✓ |
| Log.Enabled | | | ✓ |
| Log.MaxCount | | — | 1000 |
| IncludeTasks | ✓ | ✓ | ✓ |
| **Node** | | | |
| Detail.Enabled | | ✓ | ✓ |
| Detail.Disk.IncludeDiskDetail | | ✓ | ✓ |
| Detail.Disk.IncludeSmartData | | | ✓ |
| Detail.IncludeApt | | ✓ | ✓ |
| Detail.Tasks.Enabled | | ✓ | ✓ |
| Detail.IncludeFirewallLog | | ✓ | ✓ |
| IncludeReplication | ✓ | ✓ | ✓ |
| Syslog.Enabled | | | ✓ |
| Syslog.MaxCount | | — | 1000 |
| Syslog.Since | | — | last 3 days |
| RrdData.Enabled | | ✓ | ✓ |
| RrdData.TimeFrame | | Day | Week |
| **Guest** | | | |
| Detail.Enabled | | ✓ | ✓ |
| Detail.Tasks.Enabled | | ✓ | ✓ |
| Detail.IncludeFirewallLog | | ✓ | ✓ |
| IncludeSnapshots | | ✓ | ✓ |
| IncludeDisks | | ✓ | ✓ |
| IncludePartitions | | ✓ | ✓ |
| IncludeQemuAgent | | ✓ | ✓ |
| RrdData.Enabled | | | ✓ |
| RrdData.TimeFrame | | — | Week |
| **Storage** | | | |
| IncludeContent | | ✓ | ✓ |
| IncludeBackups | | ✓ | ✓ |
| RrdData.Enabled | | ✓ | ✓ |
| RrdData.TimeFrame | | Day | Week |
| **Firewall** | | | |
| Enabled | | ✓ | ✓ |
| MaxCount | | 0 | 1000 |
| Since | | — | last 3 days |

> Fast profile skips all detail sections, RRD data, firewall and storage content — designed for quick inventory on large clusters.

</details>

---

## Features

What's collected:

- **Cluster** — users, API tokens, TFA, groups, roles, ACL, firewall options, domains, backup jobs, HA, SDN, pools
- **Nodes** — services, network, disks, SMART, ZFS, APT, SSL certificates, replication, syslog, firewall logs, tasks
- **VMs/CTs** — config, network, disks, snapshots, firewall logs, tasks, QEMU agent info
- **Global sections** — Firewall (rules/aliases/ipsets), RRD Nodes/Storage/Guests, Syslog, Cluster Log, Cluster Tasks, Replication, Network, Disks, Partitions, Snapshots, Storage Content, Backups
- **Issues** — diagnostic page that aggregates any per-resource failure encountered while collecting data; appears only when there is at least one issue and is linked from the Summary/cover and the sidebar so it's the first thing you see when something didn't work
- **Network topology** — auto-generated SVG diagram of physical NICs, bonds, bridges, gateway VMs and network-backed storage — [guide](docs/network-diagram.md)

How you can shape it:

- **Three profiles** — `--fast` for a quick scan on large clusters, default Standard for daily reporting, `--full` for audits and capacity planning
- **`settings.json`** — bring your own config to enable/disable exactly the sections you want — [see Settings Reference](#settings-reference)
- **Flexible target selection** — `@all`, pools, tags, nodes, ID ranges, wildcards, exclusions — [see VM/CT Selection Patterns](#vmct-selection-patterns)
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

<details>
<summary><strong>Security &amp; Permissions</strong></summary>

### Required Permissions

| Permission | Purpose | Scope |
|------------|---------|-------|
| **VM.Audit** | Read VM/CT configuration and status | Virtual machines |
| **Datastore.Audit** | Read storage content and metrics | Storage systems |
| **Pool.Audit** | Access pool information | Resource pools |
| **Sys.Audit** | Node system information, services, disks | Cluster nodes |
| **Sys.Modify** | APT repositories, available updates and installed package versions | Cluster nodes |


</details>

---

## Settings Reference

Three built-in profiles (`--fast`, default, `--full`) cover the common cases — see the [profiles comparison](#profiles-comparison) above. For fine-grained control bring your own settings file:

```bash
cv4pve-report create-settings --full > settings.json   # generate from a profile
cv4pve-report ... export --settings-file=settings.json  # use it
```

> Full reference: **[Settings guide](docs/settings.md)** — all properties, annotated JSON example, skip-heavy-section flags, and performance tuning (`MaxParallelRequests`, `ApiTimeout`, `QemuAgentTimeout`).

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

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
