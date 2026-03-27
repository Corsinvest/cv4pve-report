# TODO

## Sheets / Data

- [ ] **Snapshot size** — calculate snapshot disk usage (requires SSH/Ceph/ZFS/LVM integration)
- [ ] **Ceph** — dedicated sheet with OSD status, pool usage, health

## Settings / Filtering

- [ ] **Tasks filter** — add options to `SettingsNode` and `SettingsGuest`:
  - `TasksOnlyErrors` (bool) — show only failed tasks
  - `TasksMaxCount` (int) — maximum number of tasks to display
