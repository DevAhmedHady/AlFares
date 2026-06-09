import { chromium } from 'playwright';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const testPage = 'file:///' + path.join(__dirname, 'dist', 'web', 'browser', 'icon-test.html').replace(/\\/g, '/');

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(testPage);
await page.waitForLoadState('networkidle');

async function inspect(id) {
  return page.$eval('#' + id, (el) => {
    const cs = getComputedStyle(el);
    // Read the ::before glyph the icon font produces
    const before = getComputedStyle(el, '::before').content;
    return { fontFamily: cs.fontFamily, beforeContent: before };
  });
}

const targets = ['btn-icon', 'bare-icon', 'nested-icon'];
let allPass = true;
for (const id of targets) {
  const r = await inspect(id);
  const usesPrimeicons = /primeicons/i.test(r.fontFamily);
  const hasGlyph = r.beforeContent && r.beforeContent !== 'none' && r.beforeContent !== 'normal' && r.beforeContent !== '""';
  const pass = usesPrimeicons && hasGlyph;
  allPass = allPass && pass;
  console.log(`${pass ? 'PASS' : 'FAIL'}  #${id}`);
  console.log(`        font-family   : ${r.fontFamily}`);
  console.log(`        ::before glyph: ${JSON.stringify(r.beforeContent)}`);
}

await browser.close();
console.log(allPass ? '\nALL ICONS USE primeicons FONT ✓' : '\nSOME ICONS STILL OVERRIDDEN ✗');
process.exit(allPass ? 0 : 1);
