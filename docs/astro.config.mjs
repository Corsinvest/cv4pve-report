// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import corsinvestTheme from '@corsinvest/cv4pve-docs-theme';

export default defineConfig({
  site: 'https://corsinvest.github.io',
  base: '/cv4pve-report',
  integrations: [
    starlight({
      title: 'cv4pve-report',
      description: 'Inventory of a whole Proxmox VE cluster in Excel, HTML or JSON: the RVTools for Proxmox VE.',
      // Brand, logo, GitHub and "Edit page" links, the Corsinvest sidebar group and
      // external links in a new tab come from the shared cv4pve theme.
      plugins: [
        corsinvestTheme({
          repo: 'cv4pve-report',
          // Product icon: favicon and header, dark variant for the dark theme.
          icon: { light: '/icon.svg', dark: '/icon-dark.svg' },
          // Banner on the home page: the same engine runs inside cv4pve-admin.
          admin: { module: 'system-report' },
          // Visits, without cookies.
          matomo: { url: 'https://matomo.corsinvest.it/', siteId: 5 },
          // Install-and-run panel in the home hero.
          install: {
            targets: ['linux', 'macos', 'windows'],
            run: ['--host=pve01', "--api-token='report@pve!report=…'", 'export'],
            output: [{ text: 'Report generated: Report_20260928_101500.zip', tone: 'ok' }],
          },
        }),
      ],
      lastUpdated: true,
      sidebar: [
        {
          label: 'Start here',
          items: ['getting-started', 'permissions', 'connection', 'coming-from-rvtools', 'ai-agents', 'troubleshooting'],
        },
        {
          label: 'Output',
          items: [
            { label: 'Overview', slug: 'output' },
            'output/excel',
            'output/html',
            'output/json',
            'output/network-diagram',
          ],
        },
        {
          label: 'Sections',
          collapsed: true,
          items: [
            'sections/capacity-planning',
            'sections/cluster',
            'sections/nodes',
            'sections/guests',
            'sections/storage',
            'sections/network-firewall',
            'sections/metrics-logs',
          ],
        },
        {
          label: 'Reference',
          items: ['settings'],
        },
      ],
    }),
  ],
});
