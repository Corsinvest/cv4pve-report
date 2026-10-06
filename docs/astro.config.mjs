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
      // Brand, product icon, GitHub link, the Corsinvest sidebar group and
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
          // Steps panel in the home hero: the same steps, in the same order, as Getting started
          // (CliGettingStarted). The commands are in the page (CliInstall).
          steps: {
            title: 'Your first report in',
            highlight: '4 steps',
            items: [
              'Install cv4pve-report',
              { text: 'Create an API token', href: 'permissions/#user-and-token' },
              'Run `cv4pve-report export`',
              'Open the report',
            ],
          },
        }),
      ],
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
