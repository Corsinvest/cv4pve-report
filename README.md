# <img src="icon.png" alt="" height="36" align="top"> cv4pve-report

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

> **The RVTools for Proxmox VE** — reads your whole cluster through the API and exports it to Excel, a static HTML site or JSON, with a network diagram.
>
> **[Documentation](https://corsinvest.github.io/cv4pve-report/)**
>
> Prefer a web interface with the history of your reports? cv4pve-report also runs inside [cv4pve-admin](https://github.com/Corsinvest/cv4pve-admin), as its [System Report](https://corsinvest.github.io/cv4pve-admin/modules/system-report/) module.

---

## Why

The Proxmox VE web interface shows one object at a time. When someone asks *how many VMs do we run, on which nodes, with how much memory, and who can log in?* — an auditor, a customer taking over the cluster, a capacity plan, a migration from VMware — there is no single place to look.

cv4pve-report reads the whole cluster — nodes, VMs, containers, storage, network, firewall, replication, HA, SDN, users and tokens — and writes it down in one file you can filter, share and keep. Coming from VMware? See [where each RVTools tab lives](https://corsinvest.github.io/cv4pve-report/coming-from-rvtools/).

It **runs outside the nodes and uses only the Proxmox VE API**: nothing to install on the cluster, no SSH, no root shell.

---

## What the report looks like

A few rows and columns of the `Storages` and `VMs` sections:

```
| Node     | Storage   | Status    | Health | Plugin Type | Shared | Disk Size GB | Disk Usage GB | Disk Usage % |
|----------|-----------|-----------|--------|-------------|--------|--------------|---------------|--------------|
| (shared) | pbs01     | available | 19     | pbs         | X      | 3000.00      | 2427.45       | 80.91%       |
| pve01    | datapool  | available | 80     | zfspool     |        | 5068.38      | 1015.68       | 20.04%       |
| pve02    | local-zfs | available | 64     | zfspool     |        | 214.09       | 77.82         | 36.35%       |
```

```
| Node  | Vm Id | Name        | Status  | Health | Cpu Size | Memory Size GB | Memory Usage % | Os Version                     | Agent Running |
|-------|-------|-------------|---------|--------|----------|----------------|----------------|--------------------------------|---------------|
| pve02 | 203   | test-debian | stopped |        | 2        | 4.00           | 0.00%          | Linux 5.x - 2.6 Kernel         |               |
| pve01 | 1006  | dc01        | running | 66     | 4        | 8.00           | 65.09%         | Windows Server 2022 Datacenter | X             |
| pve01 | 1104  | gitlab      | running | 48     | 4        | 10.00          | 95.03%         | Ubuntu 22.04.5 LTS             | X             |
```

Every node, VM and container links to its own sheet or page. Health is a 0–100 score of how loaded the resource is: higher is healthier. Every table and column is described in [Sections](https://corsinvest.github.io/cv4pve-report/sections/cluster/).

---

## Features

- **Three formats** — an Excel workbook with real Excel tables, an offline HTML site, or one JSON file per section — from one self-contained binary.
- **Network diagram** — every export includes an SVG of each node: NICs, bonds, bridges, the VMs and containers behind them, network storages.
- **Profiles** — `--fast` for a quick inventory of a large cluster, `--full` for audits: SMART, syslog, cluster log, a week of RRD history.
- **Your own selection** — a settings file turns each section on or off; guests by ID, range, name, node, pool or tag.
- **Nothing silently missing** — a call that fails lands on an Issues page with the Proxmox error, and the rest of the report is still written.
- **Diff-friendly** — every table is sorted, so two reports of an unchanged cluster differ only in live values.
- **Tested at scale** — a production cluster of 25 nodes and about 2700 VMs reports in [about 7 minutes](https://github.com/Corsinvest/cv4pve-report/issues/10#issuecomment-4432966790).
- **Keeps running with a node down** — give it more than one host and it uses the first that answers.

---

## Quick start

```bash
# Windows
winget install Corsinvest.cv4pve.report

# Linux (other platforms and packages: see the documentation)
wget https://github.com/Corsinvest/cv4pve-report/releases/latest/download/cv4pve-report-linux-x64.zip
unzip cv4pve-report-linux-x64.zip && chmod +x cv4pve-report

# Run against any node of the cluster, with an API token
./cv4pve-report --host=pve1.local --api-token='report@pve!report=<uuid>' export --format Html
```

The report is a `.zip` in the current folder. The API token needs the privileges listed in [Permissions](https://corsinvest.github.io/cv4pve-report/permissions/) — note that `PVEAuditor` alone cannot list backups.

---

## Documentation

| | |
|---|---|
| [Getting started](https://corsinvest.github.io/cv4pve-report/getting-started/) | Install, connect, run |
| [Coming from RVTools](https://corsinvest.github.io/cv4pve-report/coming-from-rvtools/) | Where each RVTools tab lives in the report |
| [Permissions](https://corsinvest.github.io/cv4pve-report/permissions/) | Creating the user and API token, required privileges |
| [Output](https://corsinvest.github.io/cv4pve-report/output/) | Excel, HTML, JSON, network diagram, Health Score |
| [Sections](https://corsinvest.github.io/cv4pve-report/sections/cluster/) | Every table and column |
| [Settings](https://corsinvest.github.io/cv4pve-report/settings/) | Profiles, settings file, guest selection, performance |
| [Troubleshooting](https://corsinvest.github.io/cv4pve-report/troubleshooting/) | The Issues page, and what the tool is doing when something goes wrong |

---

## Related tools

Use `cv4pve-report` to know *what you have*, [cv4pve-diag](https://github.com/Corsinvest/cv4pve-diag) to know *what is wrong*. The whole suite: [corsinvest.it/cv4pve](https://www.corsinvest.it/en/cv4pve/).

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/en/cv4pve/).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
