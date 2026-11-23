// Records the same touch as unity/RecordPointer.cs on screen.html, with real browser input.
// Input.dispatchMouseEvent goes through the browser hit test, :hover, :active and click.
// Nothing calls the button directly.
// Needs: the button-spike server on port 8766, Microsoft Edge, Node 22 or later.
// Run: node Design/button-spike/record-pointer-cdp.mjs   (writes Temp/touch-frames/f*.png)
import { spawn } from 'node:child_process';
import { mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const EDGE = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
const PORT = 9333;
const DIR = 'Temp/touch-frames';
const URL = 'http://localhost:8766/screen.html?export=screen';
const H = 1080;

rmSync(DIR, { recursive: true, force: true });
mkdirSync(DIR, { recursive: true });

const edge = spawn(EDGE, [
  '--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
  `--remote-debugging-port=${PORT}`, `--user-data-dir=${join(tmpdir(), 'edge-cdp-profile')}`,
  '--window-size=1920,1080', 'about:blank',
], { stdio: 'ignore' });

const sleep = ms => new Promise(r => setTimeout(r, ms));
let targets;
for (let i = 0; i < 50 && !targets; i++) {
  try { targets = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json(); } catch { await sleep(200); }
}
const page = targets.find(t => t.type === 'page');
const ws = new WebSocket(page.webSocketDebuggerUrl);
await new Promise(r => ws.addEventListener('open', r, { once: true }));

let nextId = 1;
const waiting = new Map();
const events = [];
ws.addEventListener('message', m => {
  const msg = JSON.parse(m.data);
  if (msg.id && waiting.has(msg.id)) { waiting.get(msg.id)(msg); waiting.delete(msg.id); }
  else if (msg.method) events.push(msg.method);
});
function send(method, params = {}) {
  const id = nextId++;
  ws.send(JSON.stringify({ id, method, params }));
  return new Promise((ok, fail) => waiting.set(id, msg => msg.error ? fail(new Error(`${method}: ${msg.error.message}`)) : ok(msg.result)));
}
async function js(expression) {
  const r = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
  if (r.exceptionDetails) throw new Error(r.exceptionDetails.text);
  return r.result.value;
}

await send('Page.enable');
await send('Emulation.setDeviceMetricsOverride', { width: 1920, height: H, deviceScaleFactor: 1, mobile: false });
await send('Page.navigate', { url: URL });
while (!events.includes('Page.loadEventFired')) await sleep(50);
await js('document.fonts.ready.then(() => true)');

await js(`(() => {
  const btn = document.getElementById('play');
  window.__clicks = 0;
  btn.addEventListener('click', () => window.__clicks++);
  const mark = document.createElement('div');
  mark.id = 'touch-mark';
  mark.style.cssText = 'position:fixed;border-radius:50%;background:rgba(255,255,255,.55);pointer-events:none;z-index:10;transform:translate(-50%,-50%)';
  const cap = document.createElement('div');
  cap.id = 'caption';
  cap.style.cssText = 'position:fixed;left:64px;bottom:64px;width:1400px;font:600 32px/1.2 Inter, sans-serif;color:#fff;white-space:pre;pointer-events:none;z-index:10';
  document.body.append(mark, cap);
  window.__show = (x, y, size) => {
    mark.style.left = x + 'px'; mark.style.top = y + 'px';
    mark.style.width = mark.style.height = size + 'px';
    const el = document.elementFromPoint(x, y);
    const under = !el ? 'nothing' : el.closest('#play') ? 'play button' : (el.id || el.tagName.toLowerCase());
    const state = btn.disabled ? 'disabled' : btn.matches(':active') ? 'pressed'
      : btn.matches(':hover') ? 'hover' : btn.matches(':focus-visible') ? 'focus' : 'normal';
    cap.textContent = 'real input path (CDP)\\nunder pointer: ' + under + '   state: ' + state + '   onClick: ' + window.__clicks;
    return new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
  };
  return true;
})()`);

// Same points as RecordPointer.cs. Unity has y up, the browser has y down.
const up = (x, y) => ({ x, y: H - y });
const start = up(1250, 560), miss = up(1500, 124), target = up(1696, 124), away = up(1250, 300);
let pos = { ...start }, held = false, size = 56, frame = 0;

async function shot() {
  await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: pos.x, y: pos.y, button: held ? 'left' : 'none', buttons: held ? 1 : 0 });
  await js(`__show(${pos.x}, ${pos.y}, ${size})`);
  const { data } = await send('Page.captureScreenshot', { format: 'png' });
  writeFileSync(`${DIR}/f${String(frame++).padStart(4, '0')}.png`, Buffer.from(data, 'base64'));
}
async function move(b, n) {
  const a = { ...pos };
  for (let i = 1; i <= n; i++) {
    let t = i / n; t = t * t * (3 - 2 * t);
    pos = { x: a.x + (b.x - a.x) * t, y: a.y + (b.y - a.y) * t };
    await shot();
  }
}
async function hold(n) { for (let i = 0; i < n; i++) await shot(); }
async function tap(n) {
  await send('Input.dispatchMouseEvent', { type: 'mousePressed', x: pos.x, y: pos.y, button: 'left', buttons: 1, clickCount: 1 });
  held = true; size = 80; await shot();
  await hold(n);
  await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: pos.x, y: pos.y, button: 'left', buttons: 0, clickCount: 1 });
  held = false; size = 56; await shot();
}

await hold(10);
await move(miss, 18);
await tap(8);
const clicksAfterMiss = await js('window.__clicks');
await hold(8);
await move(target, 10);
await hold(12);
await tap(16);
await hold(16);
await move(away, 18);
await hold(10);
await js(`document.getElementById('play').disabled = true`);
await hold(18);
const clicks = await js('window.__clicks');

await send('Browser.close').catch(() => {});
ws.close();
edge.kill();
const result = `${frame} frames, clicks after miss ${clicksAfterMiss}, clicks at end ${clicks}`;
writeFileSync('Temp/encode-name.txt', 'html-pointer');
console.log(result);
