// probe-hub-isolation.mjs — bypass Blazor entirely: two raw SignalR clients
// (admin + alice) against the live API. B POSTs a message via HTTP; we watch
// whether A's raw connection receives ChatMessageCreated. Proves/disproves
// the server broadcast independent of the Blazor wrapper.
import { pathToFileURL } from 'node:url';
import path from 'node:path';
const CLIENTS = path.resolve(import.meta.dirname, '..', '..', '..', 'clients');
const API = process.argv[2] ?? 'https://localhost:7030';
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);

async function issueToken(email, tenant) {
  const res = await fetch(`${API}/api/v1/identity/token/issue`, {
    method: 'POST',
    headers: { 'content-type': 'application/json', tenant, 'X-FSH-App': 'dashboard' },
    body: JSON.stringify({ email, password: 'Password123!' }),
  });
  if (!res.ok) throw new Error(`token ${email}: ${res.status} ${await res.text()}`);
  return (await res.json()).accessToken;
}

async function listChannels(token) {
  const res = await fetch(`${API}/api/v1/chat/channels`, { headers: { authorization: `Bearer ${token}` } });
  return res.json();
}

async function sendHttp(token, channelId, body) {
  const res = await fetch(`${API}/api/v1/chat/channels/${channelId}/messages`, {
    method: 'POST',
    headers: { 'content-type': 'application/json', authorization: `Bearer ${token}` },
    body: JSON.stringify({ body }),
  });
  return { status: res.status, text: await res.text() };
}

async function rawHubClient(signalr, token, label, events) {
  const conn = new signalr.HubConnectionBuilder()
    .withUrl(`${API}/api/v1/realtime/hub`, { accessTokenFactory: () => token })
    .configureLogging(signalr.LogLevel.Information)
    .build();
  for (const name of ['ChatMessageCreated', 'ChatMessageEdited', 'ChatTypingStarted', 'PresenceChanged']) {
    conn.on(name, (payload) => {
      events.push({ name, label });
      console.log(`[${label}] ← ${name}: ${JSON.stringify(payload).slice(0, 140)}`);
    });
  }
  conn.onreconnecting((e) => console.log(`[${label}] reconnecting: ${e?.message ?? e}`));
  await conn.start();
  console.log(`[${label}] hub connected (${conn.state})`);
  return conn;
}

const ts = Date.now().toString(36);
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
console.log('issuing tokens…');
const tokA = await issueToken('admin@acme.com', 'acme');
const tokB = await issueToken('alice@acme.com', 'acme');
console.log('tokens OK');

const srPath = path.join(CLIENTS, 'dashboard', 'node_modules', '@microsoft', 'signalr', 'dist', 'cjs', 'index.js');
const signalr = await import(pathToFileURL(srPath).href);

const eventsA = [];
const eventsB = [];
const A = await rawHubClient(signalr, tokA, 'A-raw', eventsA);
const B = await rawHubClient(signalr, tokB, 'B-raw', eventsB);

const channelsA = await listChannels(tokA);
const chans = Array.isArray(channelsA) ? channelsA : channelsA.items ?? [];
console.log('A channels:', chans.length, chans.slice(0, 3).map((c) => `${c.id}:${c.name ?? c.type}`).join(', '));
const general = chans[0];

await B.invoke('JoinChannel', general.id);
console.log('B invoked JoinChannel');

console.log('typing probe: B invokes Typing');
try { await B.invoke('Typing', general.id); } catch (e) { console.log('typing err:', String(e).slice(0, 120)); }
await new Promise((r) => setTimeout(r, 1500));

console.log(`HTTP send as B → channel ${general.id}`);
const sent = await sendHttp(tokB, general.id, `ISO-${ts}`);
console.log('send status:', sent.status, sent.text.slice(0, 160));

await new Promise((r) => setTimeout(r, 3000));
console.log('---');
console.log('A raw events:', JSON.stringify(eventsA));
console.log('VERDICT:', eventsA.some((e) => e.name === 'ChatMessageCreated')
  ? 'SERVER BROADCAST WORKS → bug is in Blazor client wiring'
  : 'SERVER NEVER DELIVERED → bug is server-side or hub connection-level');

await A.stop();
process.exit(0);
