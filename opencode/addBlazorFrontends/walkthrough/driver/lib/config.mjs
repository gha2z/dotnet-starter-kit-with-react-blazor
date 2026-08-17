import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const CLIENTS = fileURLToPath(new URL("../../../../../clients/", import.meta.url)); // repoRoot/clients/

export const RUN_DIR = fileURLToPath(new URL("../../evidence/", import.meta.url)); // walkthrough/evidence/

export const PLAYWRIGHT_ENTRY = pathToFileURL(path.join(CLIENTS, "admin", "node_modules", "playwright", "index.mjs")).href;
export const PNGJS_ENTRY = pathToFileURL(path.join(CLIENTS, "admin", "node_modules", "pngjs", "lib", "png.js")).href;

export const VIEWPORTS = {
  desktop: { width: 1440, height: 900 },
  mobile: { width: 390, height: 844 },
};

export const ACCOUNTS = {
  admin: { tenant: "root", email: "superadmin@root.com", password: "Password123!" },
  dashboard: { tenant: "acme", email: "admin@acme.com", password: "Password123!" },
};

export const APPS = {
  admin: {
    react: {
      id: "react-admin",
      title: "React Admin",
      base: "http://localhost:5173",
      framework: "react",
      login: { tenant: "root", email: "superadmin@root.com", password: "Password123!" },
      selectors: {
        tenant: 'input[placeholder="root"]',
        email: 'input[type="email"]',
        password: 'input[placeholder="Enter your password"]',
        submit: "button",
      },
    },
    blazor: {
      id: "blazor-admin",
      title: "Blazor Admin (FSH.Admin.Wasm)",
      base: "http://localhost:5175",
      framework: "blazor",
      login: { tenant: "root", email: "superadmin@root.com", password: "Password123!" },
      selectors: {
        tenant: "Tenant",
        email: "Email",
        password: "Password",
        submit: "button",
      },
    },
  },
  dashboard: {
    react: {
      id: "react-dashboard",
      title: "React Dashboard",
      base: "http://localhost:5174",
      framework: "react",
      login: { tenant: "acme", email: "admin@acme.com", password: "Password123!" },
      selectors: {
        tenant: 'input[placeholder="root"]',
        email: 'input[type="email"]',
        password: 'input[placeholder="Enter your password"]',
        submit: "button",
      },
    },
    blazor: {
      id: "blazor-dashboard",
      title: "Blazor Dashboard (FSH.Dashboard.Wasm)",
      base: "http://localhost:5176",
      framework: "blazor",
      login: { tenant: "acme", email: "admin@acme.com", password: "Password123!" },
      selectors: {
        tenant: "Tenant",
        email: "Email",
        password: "Password",
        submit: "button",
      },
    },
  },
};

export const IGNORED_FAILURES = /(favicon|hot-update|\.map|sse|stream|events)/i;

// Lesson 2026-08-13: react apps log one console error on EVERY page — a SignalR hub
// negotiation failure (pre-existing, react-side). Parity triage treats it as a finding,
// not a gap. Filter it out of per-screen console capture.
export const IGNORED_CONSOLE = /(signalr|negotiat|failed to start the connection|hub)/i;

export const TIMEOUTS = {
  nav: 30000,
  settle: 1500,
};