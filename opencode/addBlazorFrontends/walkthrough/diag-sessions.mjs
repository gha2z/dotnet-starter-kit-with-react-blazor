// diag-sessions.mjs — why does the sessions list return 0 rows?
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
await new Promise((r) => setTimeout(r, 1000));
const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { 'content-type': 'application/json', tenant: 'acme', 'X-FSH-App': 'dashboard' },
  body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }),
});
const tok = (await res.json()).accessToken;
const r = await fetch('https://localhost:7030/api/v1/identity/sessions', {
  headers: { authorization: `Bearer ${tok}`, tenant: 'acme' },
});
const txt = await r.text();
console.log('status:', r.status);
console.log('body:', txt.slice(0, 400));
