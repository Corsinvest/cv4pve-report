---
name: cv4pve-report
description: Export an inventory of a Proxmox VE cluster with cv4pve-report and answer questions about it from the JSON files — nodes, VMs, containers, storage, snapshots, backups, disks, network, firewall, certificates, HA, pools, tasks. Use it for inventory, audit and capacity questions answered from a dated export; for a live value or a change, use the Proxmox VE API instead. It only reads.
---

# cv4pve-report

`cv4pve-report` reads a Proxmox VE cluster through its API and writes a `.zip` report; with `--format Json`
the zip holds one JSON file per section. It sends only read requests. The connection options (host and API
token) are in a file the user names: if you do not know its path, ask.

## Rules

- Connect only with that file, passed with `@`. Do not print it or copy the token anywhere.
- Answer from the files of a report, and say which one: `generatedAt` in `metadata.json`, in UTC.
- Reuse the latest report; export a new one when the user asks for current data or a section is missing.
- Read `issues.json` first. It exists only when an API call failed: tell the user which sections are
  incomplete, and why.
- A missing guest, storage, pool or backup is not proof that it does not exist: Proxmox VE leaves out what
  the token may not see, without an error.
- Sizes are bytes and percentages are fractions from 0 to 1: convert them for the user.
- If an option is refused, check `cv4pve-report export --help`: this skill can be newer than the tool.
- Exit code 0 means success; any other code a failure, with a line starting `ERROR:` or the help.

## Export

Run it in the folder that keeps the reports: without `-o` the zip is `Report_YYYYMMDD_HHmmss.zip` in the
current folder, and the last line is `Report generated: <path>`.

```bash
cv4pve-report @<options-file> export --format Json          # standard profile
cv4pve-report @<options-file> export --format Json --fast   # overview tables only, for a large cluster
cv4pve-report @<options-file> export --format Json --full   # adds SMART, syslog, cluster log, a week of RRD
unzip -o <zip> -d <dir>                                     # PowerShell: Expand-Archive <zip> <dir> -Force
```

## Read

Start from `metadata.json` (time, filters, sections with row count) and `issues.json`. Then:

- a section with one table is an array of rows: `vms.json`, `containers.json`, `nodes.json`,
  `storages.json`, `snapshots.json`, `backups.json`, `disks.json`, …;
- a section with several blocks is an object: `nodes/<node>.json`, `vms/<id>.json`, `containers/<id>.json`,
  `cluster.json`, `network.json`, `firewall.json`, `cluster-access.json`, `cluster-sdn.json`,
  `cluster-ha.json`. The first key/value block is `info`; the others are under
  their title in camelCase (`SSL Certificates` → `sslCertificates`); a table without a title is under `rows`.

Keys are column names in camelCase without the unit: `Vm Id` → `vmId`, `Memory Size GB` → `memorySize`.
Look before you query:

```bash
jq '.[0]' vms.json                                            # keys of a table
jq 'keys' nodes/<node>.json                                   # blocks of a detail file
jq -r '.[] | select(.status == "running") | .name' vms.json
jq '.sslCertificates[] | select(.daysUntilExpiry < 30) | {fileName, notAfter}' nodes/<node>.json
```

Every file, table and key: https://corsinvest.github.io/cv4pve-report/output/json/ and
https://corsinvest.github.io/cv4pve-report/sections/cluster/
