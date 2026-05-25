# JSON format reference (`--format Json`)

`--format Json` produces a multi-file zipped JSON dataset. Extract the zip and consume the files with `jq`, Python, PowerShell, Power BI or any JSON-aware tool.

---

## File layout inside the zip

```
Report_20260506_120000.zip
├── metadata.json              ← report-wide info (schema version, timestamp, filters, generation stats)
├── issues.json                ← only present when at least one collection failure was recorded
├── network-diagram.svg        ← network topology diagram (raw SVG)
│
├── cluster.json
│
├── storages.json
├── nodes.json                 ← Nodes overview (compact list)
├── nodes/
│   ├── cc01.json              ← per-node detail
│   └── cc02.json
├── vms.json                   ← VMs overview (compact list)
├── vms/
│   ├── 100.json               ← per-VM detail
│   └── 101.json
├── containers.json
├── containers/
│   └── 200.json
├── network.json
├── storage-content.json
├── backups.json
├── disks.json
├── partitions.json
├── snapshots.json
├── firewall.json
├── replication.json
│
├── rrd-nodes.json
├── rrd-storage.json
├── rrd-guests.json
├── syslog.json
│
├── cluster-access.json        ← Cluster deep-dives
├── cluster-sdn.json
├── cluster-ha.json
├── cluster-pools.json
├── cluster-log.json
└── cluster-tasks.json
```

---

## Sections

Files are written in this order. Conditional files (`if …`) are only present when the matching setting is enabled or when the underlying data exists.

| # | File | Description | Condition |
|---|------|-------------|-----------|
| 1 | **metadata.json** | Report-wide info: schema version, timestamp, filters, per-section generation log | always |
| 2 | **issues.json** | Diagnostics for collection failures — see [Issues](#issues) below | only when failures recorded |
| 3 | **cluster.json** | Cluster status, options, firewall options, backup jobs, replication, storages, metric servers and hardware mappings | `Cluster.Include` |
| 4 | **storages.json** | Storage list with size, usage, type | always |
| 5 | **nodes.json** + **nodes/`<name>`.json** | Node overview list + per-node detail file | always |
| 6 | **vms.json** + **vms/`<id>`.json** | VM overview list + per-VM detail file | always |
| 7 | **containers.json** + **containers/`<id>`.json** | Container overview list + per-CT detail file | always |
| 8 | **network.json** | Node interfaces + VM/CT NICs (MAC, bridge, VLAN, IPs, model) | always |
| 9 | **storage-content.json** | Storage files/images with size and VM ID links | `Storage.IncludeContent` |
| 10 | **backups.json** | Backup files across all storages | `Storage.IncludeBackups` |
| 11 | **disks.json** | Global VM/CT disk inventory | `Guest.IncludeDisks` |
| 12 | **partitions.json** | Guest disk partitions via QEMU agent | `Guest.IncludePartitions` |
| 13 | **snapshots.json** | Global snapshot inventory across all VMs/CTs | `Guest.IncludeSnapshots` |
| 14 | **firewall.json** | Cluster + node + VM/CT firewall rules, aliases, IP sets | `Firewall.Enabled` |
| 15 | **replication.json** | Replication job status across all nodes | `Node.IncludeReplication` |
| 16 | **rrd-nodes.json** | Historical performance metrics per node | `Node.RrdData.Enabled` |
| 17 | **rrd-storage.json** | Historical performance metrics per storage | `Storage.RrdData.Enabled` |
| 18 | **rrd-guests.json** | Historical performance metrics per VM/CT | `Guest.RrdData.Enabled` |
| 19 | **syslog.json** | Parsed systemd journal across all nodes | `Node.Syslog.Enabled` |
| 20 | **cluster-access.json** | Users, API tokens, two-factor authentication, groups, roles, ACL and domains | `Cluster.Include` |
| 21 | **cluster-sdn.json** | SDN zones, vnets, controllers, IPAMs and subnets | `Cluster.Include` |
| 22 | **cluster-ha.json** | High Availability resources, groups and status | `Cluster.Include` |
| 23 | **cluster-pools.json** | Resource pools with member VMs, containers and storages | `Cluster.Include` |
| 24 | **cluster-log.json** | Cluster event log | `Cluster.Log.Enabled` |
| 25 | **cluster-tasks.json** | Recent tasks across the cluster | `Cluster.IncludeTasks` |
| 26 | **compliance.json** + **compliance/`<pack>`.json** | Compliance overview + per-pack detail file | any `Compliance.*` flag |

> The contents of each file (which keys, which values) are emitted by the engine and identical across formats — see [`docs/settings.md`](settings.md) for the toggles.

---

## Issues

When one or more Proxmox API calls fail during collection (a corrupt RRD file, a `500` from a node, missing permissions on a sub-resource, …), an extra `issues.json` file is emitted. On a healthy cluster the file is absent.

```json
{
  "issues": [
    {
      "severity": "Warning",
      "section": "RRD Storage",
      "message": "500 Internal Server Error — got wrong time resolution (60 != 1800) — GET /nodes/orion/storage/local-ceph/rrddata",
      "timestamp": "2026-05-11T12:30:14.1234567Z",
      "linkKey": "node:orion"
    }
  ]
}
```

| Field | Content |
|---|---|
| `severity` | `Warning` (default) — `501 Not Implemented` is silent and never appears here. |
| `section` | The logical section where the failure happened (e.g. `RRD Storage`, `Firewall Log`, `Cluster`). |
| `message` | Diagnostic line built from the Proxmox response: HTTP status, the API error body and the failing endpoint, joined with `—`. |
| `timestamp` | ISO-8601 UTC. |
| `linkKey` | Opaque key pointing to the relevant resource: `vm:<id>`, `node:<name>`, `section:<name>`. Useful for cross-referencing other JSON files. |

CI snapshots can fail/warn the build by checking `jq '.issues | length' issues.json` or `jq '[.issues[] | select(.severity == "Error")] | length'`.

---

## Compliance

When at least one standard is enabled in `settings.json` under `Compliance`, the report emits:

- `compliance.json` — flat array of one row per enabled pack with `pack`, `title`, `controls`, `findings`, severity counters (`critical`/`high`/`medium`/`low`/`info`), `skipped` and `score` (0–1 fraction).
- `compliance/<pack>.json` — per-pack object keyed by block title: `info` (pack metadata + score), `disclaimer`, `controls` (array of control status rows), `checks` (array of one row per check outcome, PASS / FAIL / N/A).

CI/automation usage examples (single pack failing the build when score drops below threshold):

```bash
jq '.[] | select(.pack == "ISO27001") | .score' compliance.json     # → 0.77
jq '.checks[] | select(.status == "FAIL")' compliance/iso27001.json
```

Full reference: [docs/compliance.md](compliance.md).

---

## JSON-specific behaviour

### File shapes

**Overview files** — `vms.json`, `nodes.json`, `containers.json`, `storages.json`, `disks.json`, `snapshots.json`, `partitions.json`, `network.json`, `replication.json`, `cluster-log.json`, `cluster-tasks.json`, `syslog.json`, `rrd-*.json`, `backups.json`, `storage-content.json` are flat arrays of row objects:

```json
[
  { "node": "cc01", "vmId": 100, "name": "Bookstack", "status": "running", "...": "..." },
  ...
]
```

**Detail files** — `vms/100.json`, `nodes/cc01.json`, `containers/200.json`, `cluster.json`, `firewall.json` are objects keyed by block title; each value is either key/value pairs or an array of rows:

```json
{
  "info": { ... },             ← key/value summary block
  "config": { ... },           ← key/value config block
  "agentOSInfo": { ... },      ← optional QEMU-agent block
  "firewallLogs": [ ... ],     ← optional table block
  "tasks": [ ... ]             ← optional table block
}
```

**`metadata.json`** carries the report-wide info:

```json
{
  "schemaVersion": 2,
  "generatedAt": "2026-05-08T17:28:51.8677072Z",
  "applicationName": "cv4pve-report",
  "applicationVersion": "2.4.0",
  "applicationUrl": "https://github.com/Corsinvest/cv4pve-report",
  "filters": {
    "nodes": "@all",
    "guests": "@all",
    "nodeRrd":    { "timeFrame": "Day", "consolidation": "Average" },
    "guestRrd":   { "timeFrame": "Day", "consolidation": "Average" },
    "storageRrd": { "timeFrame": "Day", "consolidation": "Average" }
  },
  "sections": [
    { "name": "Cluster", "count": 1, "durationSeconds": 0.18 },
    { "name": "Nodes",   "count": 25, "durationSeconds": 4.69 },
    ...
  ]
}
```

`schemaVersion` lets consumers adapt to future structural changes; `filters` mirrors the Excel `Summary` sheet and the HTML cover page; `sections` is a generation log useful for diagnostics.

### Naming conventions

Overview tables, RRD tables and any other multi-row tables expose **raw values** with **bare keys**:

- **Sizes / IO** — raw **bytes** (`ulong`). The C# field name in the engine carries a `GB` / `MB` hint for Excel/HTML rendering; the JSON output strips that suffix.
- **Percentages** — raw fractions in **`[0, 1]`**. The C# `Pct` suffix is stripped in JSON.
- **Booleans** — real JSON `true` / `false`. The C# `Flag` suffix is stripped.
- **Multi-line text** — kept as-is (string with `\n`). The C# `Wrap` suffix is stripped.
- **Dates** — ISO-8601 strings (UTC where applicable).
- **`null`** values are omitted to keep files compact.
- **Property keys** are **camelCase** (`vmId`, `memorySize`, `cpuUsage`); common acronyms keep their casing (`vmID`, `osType`, `agentOSInfo`).

| Kind | Key example | Value example | Notes |
|---|---|---|---|
| Text | `name`, `node`, `status` | `"vm01"` | Plain string. |
| Boolean | `isTemplate`, `enabled` | `true` / `false` | Real JSON booleans, not `"X"` / `""`. |
| Number — count | `cpu`, `cores`, `count` | `8` | Integer or float, unit-less. |
| Number — bytes | `memorySize`, `diskRead` | `17179869184` | Raw bytes (`ulong`). Divide by `1024**3` for GiB, `1024**2` for MiB. |
| Number — fraction | `cpuUsage`, `memoryUsage`, `ioWait` | `0.45` | Float **0–1**. Multiply by 100 for the "%" form. |
| Datetime | `lastSync`, `startTime` | `"2026-05-10T14:30:00Z"` | ISO-8601, UTC. |
| Date only | `expiry`, `validUntil` | `"2026-05-10"` | ISO-8601 date. |

> **`info` blocks in detail files** (e.g. `vms/100.json` > `info`) are an exception: they're authored as a human-readable snapshot with display labels (`"memoryGB": 16`) and decimal values, kept for readability when opening a single detail file by hand. The raw-bytes contract above applies to **table rows**, which is where snapshot diffs live.

### `jq` recipes

```bash
# All running VMs
jq '.[] | select(.status == "running") | .name' vms.json

# VMs using more than 16 GiB of memory (raw bytes, divide by 1024^3)
jq '.[] | select(.memorySize > 16 * 1024 * 1024 * 1024) | {id: .vmId, name: .name, memoryGB: (.memorySize / 1073741824)}' vms.json

# Snapshot count per VM
jq 'group_by(.vmId) | map({vmId: .[0].vmId, count: length})' snapshots.json

# SSL certificates expiring within 30 days
jq '.sslCertificates[] | select((.notAfter | fromdate) - now < 30*86400) | {file: .fileName, expires: .notAfter}' nodes/cc01.json

# Pluck the network diagram out of the zip
unzip -p Report_*.zip network-diagram.svg > diagram.svg
```

### Snapshot diffs

Stable file paths (`vms/100.json`, `nodes/cc01.json`, …) make the JSON output ideal for snapshot comparison.

```bash
# Take a snapshot every night
cv4pve-report ... --format Json --output /backup/cv4pve/$(date +%F).zip

# Compare two snapshots later
unzip /backup/cv4pve/2026-04-01.zip -d old/
unzip /backup/cv4pve/2026-05-01.zip -d new/
diff -r old/ new/

# Semantic, key-by-key diff of a single resource
diff <(jq -S . old/vms/100.json) <(jq -S . new/vms/100.json)
```

`jq -S` sorts keys, so the diff highlights only real changes — not reordering noise.

### Memory and size

Each section is serialised straight into its zip entry without buffering, so peak memory stays bounded. Typical numbers:

| Cluster size | RRD off | RRD on |
|---|---|---|
| Small (5 nodes / 20 VMs) | < 200 KB raw | < 1 MB raw |
| Medium (10 nodes / 200 VMs) | ~ 1.5 MB raw | ~ 10 MB raw |
| Large (25 nodes / 2700 VMs) | ~ 5 MB raw | ~ 60 MB raw |

JSON compresses well; the zip is typically 5–10× smaller than the raw size.

### Known caveats

- Some fields are pre-formatted as multi-line strings (e.g. `tags`, `networks` summary in the overview lists) because the same data flows into Excel and HTML cells. Where this matters, prefer the corresponding *detail* file (e.g. `vms/100.json`), where most fields are exposed in their raw form.
- Percentages are stored as floats (`0.45` for 45 %), not as strings with `%`.
- Network topology — bundled in the same zip as `network-diagram.svg`. See the [network diagram guide](network-diagram.md) for the legend and layout.
