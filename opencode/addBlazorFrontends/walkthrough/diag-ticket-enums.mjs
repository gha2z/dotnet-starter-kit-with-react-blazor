// diag-ticket-enums.mjs — dump the status/priority strings the server returns for open tickets.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
await new Promise((r) => setTimeout(r, 1000));
const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { 'content-type': 'application/json', tenant: 'acme', 'X-FSH-App': 'dashboard' },
  body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }),
});
const tok = (await res.json()).accessToken;
const r = await fetch('https://localhost:7030/api/v1/tickets?status=Open&PageNumber=1&PageSize=20', {
  headers: { authorization: `Bearer ${tok}`, tenant: 'acme' },
});
const data = await r.json();
console.log('totalCount:', data.totalCount);
for (const t of data.items) {
  console.log(`${t.number}  status=${JSON.stringify(t.status)}  priority=${JSON.stringify(t.priority)}`);
}
