# Network Diagram (SVG)

Every `export` produces a standalone SVG alongside the Excel file, showing the per-node network topology of the cluster. Open it in any browser — it's a plain SVG, no server needed.

See [network-diagram.svg](network-diagram.svg) for a full example.

[![Network Diagram preview](network-diagram.png)](network-diagram.svg)

## What you get

Two files side-by-side with the same basename:

```
Report_YYYYMMDD_HHmmss.xlsx   ← full infrastructure inventory
Report_YYYYMMDD_HHmmss.svg    ← network topology diagram
```

With `--output` / `-o` the same basename is used for both, only the extension differs.

## Layout

For each Proxmox node, the diagram shows a horizontal chain from physical hardware to leaf VMs, plus a dedicated strip for network-backed storage at the bottom.

```
[NICs] ──► [Bonds] ──► [External bridges] ──► [Gateway VMs] ──► [Internal bridges] ──► [Leaf VMs]

───────── dashed divider ─────────

[Storages]  ← dedicated strip, each storage connected to the bridge that reaches its server
```

- **Columns** grow to fit the widest box in each column, so everything aligns vertically.
- **Rows** grow in height to fit multi-line labels without clipping.
- **Arrows** are orthogonal and always flow left-to-right (from physical hardware toward guests); arrows entering the same target are staggered to avoid overlapping vertical segments.

## Colours

| Colour | Meaning |
|--------|---------|
| 🟦 Blue (`#4A90D9`) | Physical NIC (eth / InfiniBand) |
| 🟥 Red (`#E74C3C`) | Physical NIC with a gateway configured directly on the host |
| 🟪 Purple (`#7B68EE`) | Bond (link aggregation) |
| 🟩 Green (`#27AE60`) | Bridge (Linux bridge or OVSBridge) |
| 🟧 Orange (`#FF9100`) | Multi-homed VM/CT — candidate gateway/router (NICs on an external + an internal bridge) |
| ⬜ Light (`#ECF0F1`) | Normal VM / CT |
| 🟦 Teal (`#00897B`) | Network-backed storage (NFS, CIFS, PBS, iSCSI, Ceph, RBD, GlusterFS, ...) |
| ⬛ Grey (`#95A5A6`) | Inactive / down / stopped / disabled (any kind of node) |

## What's in a box

Each box is titled with the interface or object name followed by a free-text comment when available, separated by a middle dot:

- Infrastructure: `vmbr2 · 10+10 Gb Lan interna`, `bond0 · LACP uplink`, `eno3 · WAN Internet`
- VM/CT: `VM 1000 · firewall-01`, `CT 100 · backup-01`
- Storage: `pbs-backup · pbs`, `esxi-import · esxi`

Additional lines inside the box vary by type:

- **NIC** — `DOWN` (if inactive), type (if not eth), IP/GW (for standalone NICs with an IP on the host), MTU
- **Bond** — `DOWN`, mode (e.g. 802.3ad), policy, miimon, `← slaves`, MTU
- **Bridge** — `DOWN`, type (if OVSBridge), IP/IP6, GW/GW6, MTU, VLANs, VLAN-aware, Ports, OVS Bonds
- **VM/CT** — hostname (if the QEMU agent reports it), one line per NIC in compact form `netX → bridge [VLAN N] [IP:X.X.X.X/NN] [GW:X.X.X.X]`
- **Storage** — `Shared`, `Server: host/ip`, `Target: export/datastore/pool/path`, `Content: ...`

Every `<rect>` also carries a `<title>` tooltip with the full record from the Proxmox API for items that don't fit the box.

## Gateway VM detection

A VM is painted orange when it has **at least one NIC on an external bridge** (one with a physical uplink) **and at least one NIC on an internal bridge** (no physical ports).

This structural heuristic is all we have when `qemu-guest-agent` isn't installed, because the VM's gateway/IP fields are empty. A side effect: multi-homed VMs that are **not** actually routers — e.g. a backup server with a management NIC plus a storage NIC — are still coloured orange. Accepted trade-off.

When the agent is installed the data is richer and future iterations can refine the detection (e.g. follow `Gateway` → VM IP chains), but the diagram already works with zero agent coverage.

## Network storage strip

Storages are rendered in a dedicated strip below the topology, separated by a dashed divider, not as cells in the grid. Rationale: they don't participate in the bridge chain — they sit on a subnet and the host mounts them.

Each storage box:

- Connects to the bridge whose `Cidr` matches the subnet of the storage's `server` (or `monhost` for Ceph).
- Falls back to "no arrow" when the server is a hostname (no subnet to match) or no bridge covers its subnet.
- Is coloured **grey** when `Disable: true`, keeping the same visual language as inactive NICs.

Storage types shown: `nfs`, `cifs`, `pbs`, `iscsi`, `iscsidirect`, `rbd`, `cephfs`, `glusterfs`, `zfs`, `esxi`.

Local storages (`dir`, `lvm`, `lvmthin`, `zfspool`, `btrfs`) are filtered out because they don't consume network bandwidth and aren't topologically interesting.

## Header: Legend and Info

At the top of the SVG there are two side-by-side panels:

- **Legend** — colour key, always the same across runs.
- **Info** — generation timestamp, counts (nodes, bridges, bonds, physical NICs, VMs/CTs, multi-homed candidates), plus a footer linking back to cv4pve-report and `corsinvest.it`.

## Known limitations

- **Hostnames instead of IPs** on the storage `server` field can't be matched against bridge CIDRs — the storage still appears but without an incoming arrow. Workaround: put the IP literal in the storage config.
- **Multi-homed non-routers** (see [Gateway VM detection](#gateway-vm-detection)) are coloured orange. This is intentional without guest-agent data.
- **Three-level routing chains** (e.g. `wan-bridge → fw1 → dmz → fw2 → lan`) aren't fully modelled: only the first-level gateway VMs are highlighted. Real N-level chains would require a different algorithm.
- The diagram is **per-node**: shared storages and cross-node concepts (corosync network, live migration) are shown in each node section that uses them rather than globally.
