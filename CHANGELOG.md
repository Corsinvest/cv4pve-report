# Changelog

---

## [1.0.0] — 2026-03-27

### Initial release

- Export Proxmox VE infrastructure inventory to Excel (`.xlsx`)
- **Summary** sheet with report metadata, table of contents and link to GitHub
- **Cluster** sheet: status, options, users, API tokens, TFA, groups, roles, ACL, firewall rules/options, domains, backup jobs, replication, storages, metric servers, SDN zones/vnets/controllers, hardware mappings (dir/PCI/USB), resource pools with member list (VM/CT and storage)
- **Storages** sheet: storage list with links to per-storage detail sheets (content, RRD data)
- **Nodes** sheet: node list with links to per-node detail sheets (services, network, disks, SMART data, replication, RRD data, APT updates, package versions, firewall rules, SSL certificates, tasks)
- **Vms** sheet: VM/CT list with links to per-VM detail sheets (network, disks, RRD data, backups, snapshots, firewall, tasks, QEMU agent OS info/network/disks)
- QEMU agent data (hostname, OS info, network interfaces, filesystems) read once per VM — no duplicate API calls
- Agent status shown in overview: `Agent not enabled!`, `Agent not running!`, `Error Agent data!`
- `--fast` / `--full` as options on `export` and `create-settings` subcommands (not global)
- `--output|-o` option on `export` to specify output file path
- Filter support for nodes, VMs/CTs and storages (`@all`, comma-separated, wildcards, pools, tags, nodes, exclusions)
- Configurable RRD time frame and consolidation function
- Three profiles: Fast, Standard (default), Full
- Settings via `settings.json` (`create-settings` command)
- Fully navigable — every node, VM and storage is a clickable hyperlink
- Cross-platform (Windows, Linux, macOS)
- API-based, no root/SSH access required
