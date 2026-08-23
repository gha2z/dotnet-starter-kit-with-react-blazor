// check-desc.mjs — read QA-LC channels' name+description via API
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { 'content-type': 'application/json', tenant: 'acme', 'X-FSH-App': 'dashboard' },
  body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }),
});
const tok = (await res.json()).accessToken;
const r = await fetch('https://localhost:7030/api/v1/chat/channels', { headers: { authorization: `Bearer ${tok}` } });
const list = await r.json();
const chans = Array.isArray(list) ? list : list.items ?? [];
for (const c of chans.filter((x) => /^QA-LC/.test(x.name ?? ''))) {
  console.log(`${c.name} | desc=${JSON.stringify(c.description)}`);
}
