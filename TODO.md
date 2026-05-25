## Sheets / Data

- [ ] **Snapshot size** — calculate snapshot disk usage (requires SSH/Ceph/ZFS/LVM integration)
- [ ] **Ceph** — dedicated sheet with OSD status, pool usage, health

## VM / CT Inventory

- [ ] **Thin/Thick per VM** — show disk provisioning type per disk in VM detail sheet
- [ ] **Charts** *(idea)* — add Excel charts for CPU/memory/disk usage trends from RRD data

## HTML

- [ ] **Mini RRD charts inline in detail pages** — server-generated SVG line charts above the tables on VM/CT/Node detail pages (CPU, memory, disk I/O, network). Same data as the RRD tables but visual, like the Proxmox web UI Summary tab. ~150-200 lines, no JS dependency.
- [ ] **Print polish** — refine `@media print` rules so the sidebar collapses, tables expand to full width, page-actions hide, and long tables break across pages cleanly.
- [ ] **Sidebar deep search** — extend the "Find section…" box to also match resource ids/names of VMs/CTs/nodes (currently only top-level page names), so typing `1007` finds the VM page without expanding the VM group first.
- [ ] **Copy-to-clipboard button on dense config cells** — long Proxmox config strings (e.g. `virtio=BC:24:11:…,bridge=vmbr2,queues=4`) get a small 📋 button beside them.
- [ ] **Section anchor links** — show a `#` icon next to every `<h2>` so a specific in-page section can be linked directly via URL fragment.
- [ ] **Per-table CSV export** — alongside the existing "export standalone HTML" button, offer a "download this table as CSV" action.


