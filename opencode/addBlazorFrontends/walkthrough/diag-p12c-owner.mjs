// diag-p12c-owner.mjs — who owns the file rows alice sees? (validates probe-p12c expectations)
import { pathToFileURL } from 'node:url';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const CLIENTS = path.resolve(here, '..', '..', '..', 'clients');
const { chromium } = await import(pathToFileURL(path.join(CLIENTS, 'admin', 'node_modules', 'playwright', 'index.mjs')).href);
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

const API = 'https://localhost:7030';
const browser = await chromium.launch({ ignoreHTTPSErrors: true });
try {
  const page = await browser.newPage();
  const issue = async (email) => {
    const res = await page.request.post(`${API}/api/v1/identity/token/issue`, {
      headers: { tenant: 'acme', 'content-type': 'application/json' },
      data: { email, password: 'Password123!' },
    });
    return (await res.json()).accessToken;
  };
  const tok = await issue('alice@acme.com');
  const h = { authorization: `Bearer ${tok}`, tenant: 'acme' };
  const claims = JSON.parse(Buffer.from(tok.split('.')[1], 'base64').toString('utf8'));
  console.log('alice token uid:', claims.sub ?? claims.nameid ?? claims.nameidentifier ?? JSON.stringify(claims).slice(0, 200));
  for (const [label, url] of [['mine', `${API}/api/v1/files/mine?page=1&pageSize=20`], ['shared', `${API}/api/v1/files/shared?page=1&pageSize=20`]]) {
    const res = await page.request.get(url, { headers: h });
    const body = await res.text();
    console.log(`-- ${label} (${res.status()}) --`);
    try {
      for (const f of JSON.parse(body)) console.log(`row: ${f.name} | createdBy=${f.createdByUserId} | visibility=${f.visibility}`);
    } catch { console.log('body:', body.slice(0, 200)); }
  }
} catch (err) {
  console.error('ERR', String(err).slice(0, 300));
} finally {
  await browser.close();
}
