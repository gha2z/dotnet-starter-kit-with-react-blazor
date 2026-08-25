// diag-audit-search.mjs — does search=admin match on the payload (not visible in the summary)?
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
const tok = (await (await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { 'content-type': 'application/json', tenant: 'acme' },
  body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }),
})).json()).accessToken;
const H = { authorization: `Bearer ${tok}`, tenant: 'acme' };

const r = await fetch('https://localhost:7030/api/v1/audits?PageNumber=1&PageSize=5&Search=admin', { headers: H });
const j = await r.json();
for (const item of (j.items ?? []).slice(0, 3)) {
  const d = await fetch(`https://localhost:7030/api/v1/audits/${item.id}`, { headers: H });
  if (!d.ok) { console.log(`${item.id} detail → ${d.status}`); continue; }
  const full = await d.json();
  const payload = JSON.stringify(full.payloadJson ?? full.payload ?? '');
  const inPayload = payload.toLowerCase().includes('admin');
  const inSource = (full.source ?? '').toLowerCase().includes('admin');
  const inUser = (full.userName ?? '').toLowerCase().includes('admin');
  console.log(`${item.id} → payload=${inPayload} source=${inSource}(${full.source}) user=${inUser}(${full.userName})`);
}
