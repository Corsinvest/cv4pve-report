# Settings reference

The report is shaped by a `Settings` object. Three built-in profiles (`--fast`, default, `--full`) cover the common cases; for fine-grained control pass a JSON settings file with `--settings-file`.

```bash
# Step 1 — generate a settings file (pick your starting profile)
cv4pve-report create-settings          # Standard (default)
cv4pve-report create-settings --fast   # Fast
cv4pve-report create-settings --full   # Full

# Step 2 — edit settings.json to your needs

# Step 3 — run with your custom settings
cv4pve-report --host=YOUR_HOST --api-token=user@realm!token=uuid export --settings-file=settings.json
```

> The profiles comparison table (which flags each profile flips) is in the [README](../README.md#profiles-comparison).

---

## Skip heavy sections

Setting `Enabled = false` or `Include* = false` on any section skips it entirely — useful on large clusters to reduce file size and generation time.

Key flags:

- `Guest.Detail.Enabled = false` — skip all per-VM/CT detail (one entry per VM)
- `Node.Detail.Enabled = false` — skip all per-node detail
- `Cluster.Include = false` — skip the Cluster section
- `Guest.IncludeDisks / IncludeSnapshots / IncludePartitions = false` — skip individual global sections
- `Firewall.Enabled = false` — skip firewall rules and firewall logs in all detail sections

---

## Full settings.json with all defaults

```jsonc
{
  "MaxParallelRequests": 5,        // global parallel API requests (1 = sequential)
  "ApiTimeout": 0,                 // HTTP timeout in seconds (0 = 100s)
  "Cluster": {
    "Include": true,               // cluster overview (users, roles, ACL, backup jobs)
    "Log": {
      "Enabled": false,            // cluster event log
      "MaxCount": 0                // 0 = unlimited
    },
    "IncludeTasks": true           // cluster tasks
  },
  "Node": {
    "Names": "@all",               // @all | pve1 | pve1,pve2 | pve*
    "Detail": {
      "Enabled": true,             // per-node detail (set false to skip all, useful on large clusters)
      "Disk": {
        "IncludeDiskDetail": true, // physical disks, ZFS, directory mount points
        "IncludeSmartData": false  // SMART attributes per disk (one API call per disk — slow)
      },
      "Tasks": {
        "Enabled": true,
        "OnlyErrors": false,       // show only failed tasks
        "MaxCount": 0,             // 0 = unlimited
        "Source": "all"            // all | local | active
      },
      "IncludeApt": true,          // APT repositories, available updates, installed packages
      "IncludeFirewallLog": true   // firewall log in node detail (requires Firewall.Enabled)
    },
    "RrdData": {
      "Enabled": true,
      "TimeFrame": "Day",          // Hour | Day | Week | Month | Year
      "Consolidation": "Average"   // Average | Maximum
    },
    "IncludeReplication": true,    // replication jobs
    "Syslog": {
      "Enabled": false,
      "MaxCount": 500,
      "Since": null,               // DateOnly e.g. "2024-01-01"
      "Until": null
    }
  },
  "Guest": {
    "Ids": "@all",                 // see VM/CT Selection Patterns in the README
    "Detail": {
      "Enabled": true,             // per-VM/CT detail (set false to skip all, useful on large clusters)
      "Tasks": {
        "Enabled": true,
        "OnlyErrors": false,
        "MaxCount": 0,
        "Source": "all"
      },
      "IncludeFirewallLog": true   // firewall log in VM/CT detail (requires Firewall.Enabled)
    },
    "RrdData": {
      "Enabled": false,            // disabled by default — can be large on big clusters
      "TimeFrame": "Day",
      "Consolidation": "Average"
    },
    "IncludeSnapshots": true,      // global snapshots
    "IncludeDisks": true,          // global disks
    "IncludePartitions": true,     // guest disk partitions via QEMU agent
    "IncludeQemuAgent": true,      // OS info, network, filesystems (running VMs with agent only)
    "QemuAgentTimeout": 3          // seconds to wait for QEMU agent response before giving up
  },
  "Storage": {
    "IncludeContent": true,        // storage content (ISO, templates, disk images)
    "IncludeBackups": true,        // backup files
    "RrdData": {
      "Enabled": true,
      "TimeFrame": "Day",
      "Consolidation": "Average"
    }
  },
  "Firewall": {
    "Enabled": true,               // global firewall + firewall logs in detail sections
    "MaxCount": 0,                 // 0 = unlimited firewall log lines
    "Since": null,                 // DateOnly e.g. "2024-01-01"
    "Until": null
  },
  "Compliance": {                  // off by default — see docs/compliance.md
    "ISO27001": false,             // ISO/IEC 27001:2022
    "NIS2": false,                 // NIS2 — Directive (EU) 2022/2555
    "CIS": false,                  // CIS Controls v8
    "AgID": false,                 // AgID Misure Minime ICT (Italian PA baseline)
    "PCIDSS": false,               // PCI DSS v4.0
    "GDPR": false,                 // GDPR — Art. 32 Security of processing
    "DORA": false,                 // DORA — Regulation (EU) 2022/2554
    "NISTCSF": false,              // NIST Cybersecurity Framework 2.0
    "ISO27017": false              // ISO/IEC 27017:2015 cloud security extensions
  }
}
```

---

## Performance tuning

By default the report runs up to **5 parallel API requests** (`MaxParallelRequests = 5`). This works well for most clusters, but you can tune it to match your environment.

### Speed up the report

Increase `MaxParallelRequests` to fetch more data at the same time:

```jsonc
"MaxParallelRequests": 10
```

> **Don't go too high.** Each parallel request is a real HTTP call to Proxmox. Too many at once can slow down the API, increase memory usage on both sides, and make the report less stable. Values between 5 and 15 are a reasonable range.

### Handle slow or high-latency clusters

Parallelism means more simultaneous requests — if your cluster is slow or the network has high latency, some calls may time out. Increase `ApiTimeout` to give them more time:

```jsonc
"ApiTimeout": 300   // seconds (0 = 100s)
```

### Speed up QEMU agent calls

Each running VM with the QEMU agent enabled requires an agent call to collect OS info, network interfaces and disk partitions. If the agent is slow to respond, the report waits up to `QemuAgentTimeout` seconds per VM before giving up:

```jsonc
"QemuAgentTimeout": 3   // default: 3 seconds
```

Lower this value if you have many VMs and the agent is unreliable. Set it to `1` for a quick scan, or raise it if agents are consistently slow.

### Debug API calls

Add `--debug` to see every API call with its duration in milliseconds — useful to identify which calls are slow:

```bash
cv4pve-report @config.rsp export --debug
```

### Summary

| Setting | Effect | Default |
|---------|--------|---------|
| `MaxParallelRequests` ↑ | Faster, but more load on Proxmox and higher memory usage | 5 |
| `ApiTimeout` ↑ | Avoids timeouts on slow/high-latency clusters | 100s |
| `QemuAgentTimeout` ↓ | Less waiting per VM when agent is slow or absent | 3s |
