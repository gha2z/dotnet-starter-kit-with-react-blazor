// diag-ticket-filters.mjs — classify the tickets filter failure (P5.1).
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
await new Promise((r) => setTimeout(r, 1000));
const res = await fetch('https://localhost:7030/api/v1/identity/token/issue', {
  method: 'POST',
  headers: { 'content-type': 'application/json', tenant: 'acme', 'X-FSH-App': 'dashboard' },
  body: JSON.stringify({ email: 'admin@acme.com', password: 'Password123!' }),
});
const tok = (await res.json()).accessToken;
const H = { authorization: `Bearer ${tok}`, tenant: 'acme' };

async function q(label, qs) {
  const t0 = Date.now();
  try {
    const r = await fetch(`https://localhost:7030/api/v1/tickets?${qs}`, { headers: H });
    const txt = await r.text();
    let n = '?';
    try { n = JSON.parse(txt).totalCount; } catch { /* non-json */ }
    console.log(`${label.padEnd(16)} → ${r.status} in ${Date.now() - t0}ms total=${n}`);
    if (r.status !== 200) console.log('   body:', txt.slice(0, 220));
  } catch (e) {
    console.log(`${label.padEnd(16)} → THREW after ${Date.now() - t0}ms: ${String(e).slice(0, 140)}`);
  }
}

await q('(none)', 'PageNumber=1&PageSize=20&sortBy=createdAtUtc&sortDir=desc');
await q('status=Open', 'PageNumber=1&PageSize=20&status=Open&sortBy=createdAtUtc&sortDir=desc');
await q('status=0', 'PageNumber=1&PageSize=20&status=0&sortBy=createdAtUtc&sortDir=desc');
await q('priority=High', 'PageNumber=1&PageSize=20&priority=High&sortBy=createdAtUtc&sortDir=desc');
await q('status=Closed', 'PageNumber=1&PageSize=20&status=Closed&sortBy=createdAtUtc&sortDir=desc');
await q('priority=Low', 'PageNumber=1&PageSize=20&priority=Low&sortBy=createdAtUtc&sortDir=desc');
