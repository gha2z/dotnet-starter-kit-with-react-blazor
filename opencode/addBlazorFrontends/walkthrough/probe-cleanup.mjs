// probe-cleanup.mjs — delete QA-* / Visual-* / qa-* probe entities from the demo DB (P8)
// Best-effort: 404/400 on individual items is fine (already gone). Prints a per-type tally.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
await new Promise((r) => setTimeout(r, 1500));
const API = 'https://localhost:7030';

async function token(email, tenant) {
  const res = await fetch(`${API}/api/v1/identity/token/issue`, {
    method: 'POST',
    headers: { 'content-type': 'application/json', tenant },
    body: JSON.stringify({ email, password: 'Password123!' }),
  });
  return (await res.json()).accessToken;
}

const dash = await token('admin@acme.com', 'acme');
const admin = await token('superadmin@root.com', 'root');
const H = { authorization: `Bearer ${dash}`, tenant: 'acme' };
const HA = { authorization: `Bearer ${admin}`, tenant: 'root' };

const isQa = (name) => /^(QA-|Visual-|qauser|qa-)/i.test(name ?? '');

async function list(url, headers) {
  const res = await fetch(url, { headers });
  if (!res.ok) return [];
  const json = await res.json();
  return json.items ?? [];
}

async function del(url, headers, name) {
  const res = await fetch(url, { method: 'DELETE', headers });
  if (res.ok) return 1;
  console.log(`  skip ${name} → ${res.status}`);
  return 0;
}

let total = 0;

// Catalog brands + categories + products (acme)
for (const [url, label] of [
  [`${API}/api/v1/catalog/brands?PageSize=200`, 'brands'],
  [`${API}/api/v1/catalog/categories?PageSize=200`, 'categories'],
  [`${API}/api/v1/catalog/products?PageSize=200`, 'products'],
]) {
  const items = await list(url, H);
  let n = 0;
  for (const it of items) {
    if (!isQa(it.name)) continue;
    const base = url.split('?')[0];
    n += await del(`${base}/${it.id}`, H, it.name);
  }
  console.log(`${label}: deleted ${n}`);
  total += n;
}

// Channels (acme) — archive QA channels (archive is the correct lifecycle op).
// NOTE: chat channels return a plain array, not a paged envelope.
{
  const res = await fetch(`${API}/api/v1/chat/channels?PageSize=200`, { headers: H });
  const json = await res.json();
  const items = Array.isArray(json) ? json : (json.items ?? []);
  let n = 0;
  for (const ch of items) {
    if (!isQa(ch.name) || ch.type !== 'Channel') continue;
    const r = await fetch(`${API}/api/v1/chat/channels/${ch.id}`, { method: 'DELETE', headers: H });
    if (r.ok) n++; else console.log(`  skip channel ${ch.name} → ${r.status}`);
  }
  console.log(`channels archived: ${n} (of ${items.filter((c) => isQa(c.name) && c.type === 'Channel').length} QA channels)`);
  total += n;
}

// Roles + groups (acme)
for (const [url, label] of [
  [`${API}/api/v1/identity/roles?PageSize=200`, 'roles'],
  [`${API}/api/v1/identity/groups?PageSize=200`, 'groups'],
]) {
  const items = await list(url, H);
  let n = 0;
  for (const it of items) {
    if (!isQa(it.name)) continue;
    n += await del(`${url.split('?')[0]}/${it.id}`, H, it.name);
  }
  console.log(`${label}: deleted ${n}`);
  total += n;
}

// Tickets (acme) — close + delete QA tickets if delete exists; else skip
{
  const items = await list(`${API}/api/v1/tickets?PageSize=200`, H);
  const qa = items.filter((t) => isQa(t.title));
  console.log(`tickets: ${qa.length} QA tickets found (left in place — no public delete; they age out with the demo DB)`);
}

// Tenants (root) — QA tenants from the admin harness
{
  const items = await list(`${API}/api/v1/tenants?PageSize=100`, HA);
  let n = 0;
  for (const t of items) {
    if (!/^qa-tenant-/i.test(t.id ?? '')) continue;
    const res = await fetch(`${API}/api/v1/tenants/${t.id}`, { method: 'DELETE', headers: HA });
    if (res.ok) n++; else console.log(`  skip tenant ${t.id} → ${res.status}`);
  }
  console.log(`tenants deleted: ${n}`);
  total += n;
}

// Files (acme) — qa-*.txt uploads
{
  const items = await list(`${API}/api/v1/files/my?page=1&pageSize=200`, H);
  let n = 0;
  for (const f of items) {
    if (!/^qa-/i.test(f.originalFileName ?? '')) continue;
    const res = await fetch(`${API}/api/v1/files/${f.id}`, { method: 'DELETE', headers: H });
    if (res.ok) n++;
  }
  console.log(`files deleted: ${n}`);
  total += n;
}

console.log(`\nTOTAL deleted/archived: ${total}`);
