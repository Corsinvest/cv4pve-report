# JSON format reference (`--format Json`)

`--format Json` produces a multi-file zipped JSON dataset. Extract the zip and consume the files with `jq`, Python, PowerShell, Power BI or any JSON-aware tool.

```
Report_20260506_120000.zip    ← multi-file JSON dataset
```

---

## File layout inside the zip

```
report.zip
├── metadata.json              ← report-wide info (schema version, timestamp, filters, generation stats)
├── network-diagram.svg        ← network topology diagram (raw SVG)
│
├── cluster.json
├── cluster-log.json
├── cluster-tasks.json
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
├── storages.json
├── storage-content.json
├── backups.json
├── disks.json
├── partitions.json
├── snapshots.json
├── network.json
├── firewall.json
├── replication.json
├── rrd-nodes.json
├── rrd-storages.json
├── rrd-guests.json
└── syslog.json
```

Same logical layout as the [Excel sheets](format-xlsx.md) and [HTML pages](format-html.md) — only the rendering differs.

---

## File shapes

### Overview files (single table)

`vms.json`, `nodes.json`, `containers.json`, `storages.json`, `disks.json`, `snapshots.json`, `partitions.json`, `network.json`, `replication.json`, `cluster-log.json`, `cluster-tasks.json`, `syslog.json`, `rrd-*.json`, `backups.json`, `storage-content.json`:

```json
[
  {
    "node": "cc01",
    "vmId": 100,
    "name": "Bookstack",
    "status": "running",
    ...
  },
  ...
]
```

A flat array of row objects.

### Detail files (multiple blocks)

`vms/100.json`, `nodes/cc01.json`, `containers/200.json`, `cluster.json`, `firewall.json`:

```json
{
  "info": { ... },             ← key/value summary block
  "config": { ... },           ← key/value config block
  "agentOSInfo": { ... },      ← optional QEMU-agent block
  "firewallLogs": [ ... ],     ← optional table block
  "tasks": [ ... ]             ← optional table block
}
```

Each top-level key is a *block*; the value is either an object (key/value pairs) or an array of rows.

### `metadata.json`

```json
{
  "schemaVersion": 1,
  "generatedAt": "2026-05-08T17:28:51.8677072Z",
  "applicationName": "cv4pve-report",
  "applicationVersion": "2.0.1",
  "applicationUrl": "https://github.com/Corsinvest/cv4pve-report",
  "filters": {
    "nodes": "@all",
    "guests": "@all",
    "guestRrd": { "timeFrame": "Day", "consolidation": "Average" }
  },
  "sections": [
    { "name": "Cluster", "count": 1, "durationSeconds": 0.18 },
    { "name": "Nodes",   "count": 25, "durationSeconds": 4.69 },
    ...
  ]
}
```

`schemaVersion` lets consumers adapt to future structural changes; `filters` mirrors what the Excel "Summary" sheet and the HTML cover page show; `sections` is a generation log useful for diagnostics.

---

## Naming conventions

- **Property keys** are **camelCase** (`vmId`, `memoryUsageGB`, `cpuUsagePct`).
- **Common acronyms** keep their casing (`vmID`, `memoryGB`, `osType`, `agentOSInfo`).
- **Timestamps** are ISO-8601 in UTC (`"2026-05-08T17:28:51Z"`).
- **`null`** is used for genuinely missing values; null-valued keys are omitted from the JSON to keep the files compact.
- **Suffix conventions** mirror the Excel column suffixes:
  - `…Pct` → percentage as a float (e.g. `0.45` for 45 %)
  - `…GB` / `…MB` → numeric size in the named unit
  - `…Date` → date or datetime ISO-8601 string

---

## `jq` examples

**All running VMs**
```bash
jq '.[] | select(.status == "running") | .name' vms.json
```

**VMs using more than 16 GB of memory**
```bash
jq '.[] | select(.memorySizeGB > 16) | {id: .vmId, name: .name, memoryGB: .memorySizeGB}' vms.json
```

**Snapshot count per VM**
```bash
jq 'group_by(.vmId) | map({vmId: .[0].vmId, count: length})' snapshots.json
```

**SSL certificates expiring within 30 days**
```bash
jq '.sslCertificates[] | select((.notAfter | fromdate) - now < 30*86400) | {file: .fileName, expires: .notAfter}' nodes/cc01.json
```

**Pluck the network diagram out of the zip**
```bash
unzip -p report.zip network-diagram.svg > diagram.svg
```

---

## Snapshot diffs

Stable file paths (`vms/100.json`, `nodes/cc01.json`, …) make the JSON output ideal for snapshot comparison.

```bash
# Take a snapshot every night
cv4pve-report ... --format Json --output /backup/cv4pve/$(date +%F).zip

# Later, compare two snapshots:
unzip /backup/cv4pve/2026-04-01.zip -d old/
unzip /backup/cv4pve/2026-05-01.zip -d new/
diff -r old/ new/
```

The diff identifies exactly which VMs / nodes / storages changed, which were added, which were removed.

For semantic, key-by-key diffs of a single resource:
```bash
diff <(jq -S . old/vms/100.json) <(jq -S . new/vms/100.json)
```

`jq -S` sorts keys, so the diff highlights only real changes — not reordering noise.

---

## Memory and size

Each section is serialised straight into its zip entry without buffering, so peak memory stays bounded. Typical numbers:

| Cluster size | RRD off | RRD on |
|---|---|---|
| Small (5 nodes / 20 VMs) | < 200 KB raw | < 1 MB raw |
| Medium (10 nodes / 200 VMs) | ~ 1.5 MB raw | ~ 10 MB raw |
| Large (25 nodes / 2700 VMs) | ~ 5 MB raw | ~ 60 MB raw |

JSON compresses well; the zip is typically 5–10× smaller than the raw size.

---

## Limitations / known caveats

- Some fields are pre-formatted as multi-line strings (e.g. `tags`, `networks` summary in the overview lists) because the same data flows into Excel and HTML cells. Where this matters, prefer the corresponding *detail* file (e.g. `vms/100.json`), where most fields are exposed in their raw form.
- Percentages are stored as floats (`0.45` for 45 %), not as strings with `%`.
- Boolean flags coming from columns named `…Flag` are emitted as JSON booleans; their suffix is stripped (`isTemplateFlag` → `isTemplate`).
