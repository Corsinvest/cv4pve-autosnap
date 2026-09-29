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
      // Brand, logo, GitHub and "Edit page" links, the Corsinvest sidebar group and
      // external links in a new tab come from the shared cv4pve theme.
      plugins: [
        corsinvestTheme({
          repo: 'cv4pve-autosnap',
          // Product icon: favicon and header, dark variant for the dark theme.
          icon: { light: '/icon.svg', dark: '/icon-dark.svg' },
          // Banner on the home page: the same engine runs inside cv4pve-admin.
          admin: { module: 'autosnap' },
          // Visits, without cookies.
          matomo: { url: 'https://matomo.corsinvest.it/', siteId: 9 },
          // Install-and-run panel in the home hero.
          install: {
            targets: ['linux', 'macos', 'windows'],
            run: ['--host=pve01', "--api-token='autosnap@pve!snap=…'", '--vmid=@all', 'snap --label=daily --keep=7'],
            output: [{ text: 'Create snapshot: autodaily260929020000', tone: 'ok' }],
          },
        }),
      ],
      lastUpdated: true,
      sidebar: [
        {
          label: 'Start here',
          items: ['getting-started', 'permissions', 'troubleshooting'],
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
