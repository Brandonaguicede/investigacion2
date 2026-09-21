// Optional verification only. The application itself needs only run.ps1 / run.sh.
// Execute inside the Playwright container; see reproducibility-results.txt and final-validation-results.txt.
// Requires a freshly started backend (initial price 100).
const { chromium } = require('/tmp/check/node_modules/playwright');
const assert = require('node:assert/strict');
(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const context = await browser.newContext();
    const a = await context.newPage();
    const b = await context.newPage();
    const url = process.env.WEB_URL || 'http://host.docker.internal:8081';
    const errors = [];
    const received = [[], []];
    const navigations = [0, 0];
    for (const [i, page] of [a,b].entries()) {
      page.on('pageerror', error => errors.push(error.message));
      page.on('framenavigated', frame => { if (frame === page.mainFrame()) navigations[i]++; });
      page.on('websocket', ws => {
        assert.equal(ws.url(), url.replace(/^http/, 'ws') + '/ws');
        ws.on('framereceived', frame => received[i].push(JSON.parse(String(frame.payload))));
      });
      const response = await page.goto(url);
      assert.equal(response.status(), 200);
      await page.waitForFunction(() => document.querySelector('.connection-status strong')?.textContent === 'Conectado');
      assert.equal(await page.locator('button').isEnabled(), true);
      await state(page, 100, '---');
    }
    console.log('PASS: React rendered in two Chromium tabs; both Conectado; same-origin WebSocket');
    console.log('PASS: both tabs show the initial state $100 / --- without any bid');
    async function bid(page, name, value) {
      await page.locator('#participant').fill(name);
      await page.locator('#amount').fill(String(value));
      await page.locator('button').click();
    }
    async function state(page, price, winner) {
      await page.waitForFunction(({price,winner}) =>
        document.querySelector('.price')?.textContent === '$' + price &&
        document.querySelector('.winner dd')?.textContent === winner, {price,winner});
    }
    await bid(a, 'Kenneth', 120);
    await Promise.all([state(a,120,'Kenneth'), state(b,120,'Kenneth')]);
    console.log('PASS: Kenneth 120 -> both clients 120 / Kenneth, no refresh');
    const count = received[0].length;
    await bid(b,'Maria',110);
    await b.waitForFunction(() => {
      const text = document.querySelector('.messages p')?.textContent || '';
      return text !== 'Subasta actualizada.' && text !== 'Conexión establecida. Esperando ofertas.';
    });
    await b.waitForTimeout(750);
    assert.equal(received[0].length,count);
    assert.equal(received[1].at(-1).type,'error');
    assert.equal(await b.locator('.messages p').textContent(),received[1].at(-1).message);
    await Promise.all([state(a,120,'Kenneth'), state(b,120,'Kenneth')]);
    console.log('PASS: Maria 110 rejected only to Maria; Kenneth receives no message; price remains 120');
    await bid(b,'Maria',150);
    await Promise.all([state(a,150,'Maria'), state(b,150,'Maria')]);
    assert.deepEqual(navigations,[1,1]);
    assert.deepEqual(errors,[]);
    console.log('PASS: Maria 150 -> both clients 150 / Maria; zero reloads or JS errors');
    assert.deepEqual(received[0].map(x=>x.type),['auctionUpdated','auctionUpdated','auctionUpdated']);
    assert.deepEqual(received[1].map(x=>x.type),['auctionUpdated','auctionUpdated','error','auctionUpdated']);
    assert.deepEqual(received[0].map(x=>x.currentPrice),[100,120,150]);
    console.log('PASS: real WebSocket frames verified (initial state, broadcast, private error)');
    const late = await context.newPage();
    late.on('pageerror', error => errors.push(error.message));
    await late.goto(url);
    await state(late, 150, 'Maria');
    assert.deepEqual(errors,[]);
    console.log('PASS: late tab opened after the bids shows 150 / Maria immediately, without bidding');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
