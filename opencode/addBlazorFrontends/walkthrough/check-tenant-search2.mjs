// check-tenant-search2.mjs — isolate: tenant scoping vs search matching
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
await new Promise((r) => setTimeout(r, 2000));
const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { 'content-type': 'application/json', tenant: 'root', 'X-FSH-App': 'admin' },
  body: JSON.stringify({ email: 'superadmin@root.com', password: 'Password123!' }),
});
const tok = (await res.json()).accessToken;
const H = (tenant) => ({ authorization: `Bearer ${tok}`, tenant });

async function q(tenant, search) {
  const url = `https://localhost:7030/api/v1/identity/users/search?PageNumber=1&PageSize=25${search ? `&Search=${encodeURIComponent(search)}` : ''}`;
  const r = await fetch(url, { headers: H(tenant) });
  const j = await r.json().catch(() => null);
  const n = j?.totalCount ?? '?';
  const names = (j?.items ?? []).slice(0, 3).map((u) => u.userName ?? u.email).join(', ');
  console.log(`tenant=${tenant} search="${search ?? ''}" → total=${n} [${names}]`);
}

await q('root', '');
await q('root', 'super');
await q('acme', '');
await q('acme', 'alice');
await q('acme', 'Alice');
await q('acme', 'admin');
