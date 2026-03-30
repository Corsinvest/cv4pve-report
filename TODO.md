# TODO

## Sheets / Data

- [ ] **Snapshot size** — calculate snapshot disk usage (requires SSH/Ceph/ZFS/LVM integration)
- [ ] **Ceph** — dedicated sheet with OSD status, pool usage, health

## Settings / Filtering

- [ ] **Audit log** — add `IncludeAuditLog` to `SettingsCluster`:
  - `AuditLogMaxEntries` (int, default 500) — limit entries via `client.Cluster.Log.Log(max)`
  - `AuditLogOnlyErrors` (bool) — filter by severity (Error/Critical/Alert/Panic)
