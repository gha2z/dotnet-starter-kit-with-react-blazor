// diag-p12-revert.mjs — revert the probe's +2 stock on the QA product (PATCH /stock)
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { tenant: 'acme', 'content-type': 'application/json' },
  body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }),
});
const tok = (await res.json()).accessToken;
const r = await fetch('https://localhost:7030/api/v1/catalog/products/01a03958-4a3f-7a1b-8312-e4ba70588226/stock', {
  method: 'PATCH',
  headers: { authorization: `Bearer ${tok}`, tenant: 'acme', 'content-type': 'application/json' },
  body: JSON.stringify({ delta: -2 }),
});
console.log('revert status:', r.status, await r.text());
