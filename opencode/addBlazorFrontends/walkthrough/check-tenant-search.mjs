// check-tenant-search.mjs — reproduce SearchInTenantAsync server-side behavior
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
await new Promise((r) => setTimeout(r, 3000));
const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { 'content-type': 'application/json', tenant: 'root', 'X-FSH-App': 'admin' },
  body: JSON.stringify({ email: 'superadmin@root.com', password: 'Password123!' }),
});
console.log('token status:', res.status);
const tok = (await res.json()).accessToken;

// acme tenant id
const tRes = await fetch('https://localhost:7030/api/v1/tenants/?PageNumber=1&PageSize=50', { headers: { authorization: `Bearer ${tok}`, tenant: 'root' } });
const tenants = await tRes.json();
const list = Array.isArray(tenants) ? tenants : tenants.items ?? [];
const acme = list.find((t) => /^acme$/i.test(t.name ?? t.identifier ?? ''));
console.log('acme tenant:', acme?.id ?? acme?.identifier ?? JSON.stringify(tenants).slice(0, 200));

const tid = acme?.id ?? acme?.identifier;
const sRes = await fetch(`https://localhost:7030/api/v1/identity/users/search?PageNumber=1&PageSize=25&Search=alice`, {
  headers: { authorization: `Bearer ${tok}`, tenant: tid },
});
const body = await sRes.text();
console.log('status', sRes.status, 'body:', body.slice(0, 300));
