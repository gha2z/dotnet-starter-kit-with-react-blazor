// diag-audits-api.mjs — API-level verification: every audits filter returns only matching items.
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
const tok = (await (await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { 'content-type': 'application/json', tenant: 'acme' },
  body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }),
})).json()).accessToken;
const H = { authorization: `Bearer ${tok}`, tenant: 'acme' };

async function q(label, qs, check) {
  const r = await fetch(`https://localhost:7030/api/v1/audits?${qs}`, { headers: H });
  const j = await r.json();
  const items = j.items ?? [];
  const bad = items.filter((x) => !check(x)).length;
  console.log(`${label} → ${r.status} total=${j.totalCount} page=${items.length} mismatches=${bad}`);
  if (bad > 0) console.log('   sample mismatch:', JSON.stringify(items.find((x) => !check(x))).slice(0, 200));
}

await q('no-filter        ', 'PageNumber=1&PageSize=50', () => true);
await q('eventType=Security', 'PageNumber=1&PageSize=50&EventType=Security', (x) => x.eventType === 'Security');
await q('eventType=Activity', 'PageNumber=1&PageSize=50&EventType=Activity', (x) => x.eventType === 'Activity');
await q('severity=Error    ', 'PageNumber=1&PageSize=50&Severity=Error', (x) => x.severity === 'Error');
await q('severity=Warning  ', 'PageNumber=1&PageSize=50&Severity=Warning', (x) => x.severity === 'Warning');
await q('search=admin      ', 'PageNumber=1&PageSize=50&Search=admin', (x) => [x.userName, x.source].some((v) => typeof v === 'string' && v.toLowerCase().includes('admin')));
await q('from=24h ago      ', `PageNumber=1&PageSize=50&FromUtc=${encodeURIComponent(new Date(Date.now() - 24 * 3600e3).toISOString())}`, (x) => new Date(x.occurredAtUtc) >= new Date(Date.now() - 24 * 3600e3));
await q('exclude=Activity  ', 'PageNumber=1&PageSize=50&ExcludeEventType=Activity', (x) => x.eventType !== 'Activity');
