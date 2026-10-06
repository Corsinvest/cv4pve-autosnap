// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import corsinvestTheme from '@corsinvest/cv4pve-docs-theme';

export default defineConfig({
  site: 'https://corsinvest.github.io',
  base: '/cv4pve-autosnap',
  integrations: [
    starlight({
      title: 'cv4pve-autosnap',
      description: 'Automatic snapshots of Proxmox VE virtual machines and containers, with retention.',
      // Brand, product icon, GitHub link, the Corsinvest sidebar group and
      // external links in a new tab come from the shared cv4pve theme.
      plugins: [
        corsinvestTheme({
          repo: 'cv4pve-autosnap',
          // Product icon: favicon and header, dark variant for the dark theme.
          icon: { light: '/icon.svg', dark: '/icon-dark.svg' },
          // Button in the home hero: the same engine runs inside cv4pve-admin.
          admin: { module: 'autosnap' },
          // Visits, without cookies.
          matomo: { url: 'https://matomo.corsinvest.it/', siteId: 9 },
          // Steps panel in the home hero: the same steps, in the same order and words, as
          // Getting started (CliGettingStarted). The commands are in the pages (CliInstall).
          steps: {
            items: [
              'Install cv4pve-autosnap',
              { text: 'Create an API token', href: 'permissions/#user-and-token' },
              'Run `cv4pve-autosnap status`',
              'Take the first snapshots',
            ],
          },
        }),
      ],
      sidebar: [
        {
          label: 'Start here',
          items: ['getting-started', 'permissions', 'connection', 'ai-agents', 'troubleshooting'],
        },
        {
          label: 'Guide',
          items: ['guests', 'retention', 'scheduling', 'consistency', 'hooks'],
        },
        {
          label: 'Reference',
          items: ['commands'],
        },
      ],
    }),
  ],
});
