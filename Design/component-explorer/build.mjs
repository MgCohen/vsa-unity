// Builds index.html from the sources and page.html. Usage: node build.mjs [list]
import fs from "node:fs";
import path from "node:path";
const here = import.meta.dirname, src = path.join(here, "sources"), spike = path.join(here, "../button-spike");

// ---------- CSS: flatten nested rules, keep layer and media ----------
function split(s, sep) {
  const out = []; let d = 0, cur = "";
  for (const c of s) {
    if ("([".includes(c)) d++;
    if (")]".includes(c)) d--;
    if (!d && (sep === " " ? /\s/.test(c) : c === sep)) { if (cur.trim()) out.push(cur.trim()); cur = ""; } else cur += c;
  }
  if (cur.trim()) out.push(cur.trim());
  return out;
}
function flatten(css, base = 1) {
  css = css.replace(/\/\*[\s\S]*?\*\//g, c => c.replace(/[^\n]/g, " "));
  const nl = t => t.split("\n").length - 1;
  // First and last line of a text that ends immediately before the current character.
  const span = raw => { const end = base + nl(css.slice(0, i - 1)) - nl(raw.slice(raw.trimEnd().length)); return [end - nl(raw.trim()), end]; };
  const rules = []; let i = 0;
  function block(ctx) {
    let buf = "", own = null;
    const add = t => {
      const at = span(t); t = t.trim(); if (!t || !ctx.sel || ctx.skip) return;
      const k = t.indexOf(":");
      if (!own) rules.push(own = { sel: ctx.sel, layer: ctx.layer, media: ctx.media, l: ctx.l, decls: [] });
      own.decls.push([t.slice(0, k).trim(), t.slice(k + 1).trim().replace(/\s+/g, " "), at]);
    };
    while (i < css.length) {
      const c = css[i++];
      if (c === "}") break;
      if (c === ";") { add(buf); buf = ""; }
      else if (c === "{") {
        const l = span(buf), h = buf.trim().replace(/\s+/g, " "); buf = "";
        if (h.startsWith("@layer")) block({ ...ctx, layer: h.slice(7) });
        else if (h.startsWith("@media")) block({ ...ctx, media: [...ctx.media, h.slice(7)] });
        else if (h.startsWith("@")) block({ ...ctx, skip: true });
        else {
          const kids = split(h, ",");
          block({ ...ctx, l, sel: ctx.sel ? ctx.sel.flatMap(p => kids.map(k => k.includes("&") ? k.replaceAll("&", p) : p + " " + k)) : kids });
        }
        own = null;
      } else buf += c;
    }
    add(buf);
  }
  block({ sel: null, media: [], layer: "" });
  return rules;
}
// ---------- Files ----------
const files = {};
const norm = t => t.replace(/\r\n/g, "\n");
const load = (dir, f, key = f) => { files[key] = { name: path.posix.basename(key), path: key, text: norm(fs.readFileSync(path.join(dir, f), "utf8")), g: {} }; };
(function walk(d) { for (const e of fs.readdirSync(path.join(src, d), { withFileTypes: true })) { const f = path.posix.join(d, e.name); if (e.isDirectory()) walk(f); else if (/\.(ts|css)$/.test(f)) load(src, f); } })("");
for (const f of ["button.css", "components.json", "tokens.css"]) load(spike, f, "ds/" + f);
const lineAt = (t, i) => t.slice(0, i).split("\n").length;
const add = (f, g, item) => { (files[f].g[g] ||= []).push(item); return item; };

// ---------- TypeScript: the members of the class ----------
// A member starts at an indent of two spaces. Lines with a deeper indent and closing brackets belong to it.
function chunks(lines, a, b) {
  const out = []; let cur = null, open = false;
  for (let i = a; i < b; i++) {
    const t = lines[i];
    if (!t.trim()) { if (cur && cur.doc && !cur.code) cur = null; continue; }
    const top = /^ {2}\S/.test(t), s = t.trim();
    if (top && s.startsWith("/**")) { cur = { doc: s, d: i, l: 0, code: "" }; open = !s.endsWith("*/"); continue; }
    if (open) { cur.doc += " " + s; if (s.endsWith("*/")) open = false; continue; }
    if (top && (s.startsWith("//") || s.startsWith("/*"))) { if (!(cur && cur.code && !cur.done)) cur = null; continue; }
    if (top && !/^[})\]]/.test(s) && !(cur && !cur.done && cur.deco)) { if (!cur || cur.code) cur = { doc: "", code: "" }; cur.l = i + 1; cur.e = i + 1; cur.code = s; out.push(cur); }
    else if (cur) { cur.code += "\n" + t; cur.e = i + 1; }
    cur.deco = !noDeco(cur.code).trim();
  }
  return out;
}
function noDeco(c) {
  c = c.trimStart();
  while (c[0] === "@") {
    let i = c.match(/^@\w+/)[0].length, d = 0;
    if (c[i] === "(") for (; i < c.length; i++) { if ("({[".includes(c[i])) d++; if (")}]".includes(c[i]) && !--d) { i++; break; } }
    if (d) return "";
    c = c.slice(i).trimStart();
  }
  return c;
}
const docText = d => d.replace(/^\/\*\*\s*|\s*\*\/$/g, "").replace(/(^|\s)\*\s/g, " ").replace(/@internal\s*/g, "").trim();
function parseTs(f) {
  const t = files[f].text, lines = t.split("\n"), P = {};
  const imports = [];
  for (const m of t.matchAll(/^import (?:type )?(?:([^;']*?) from )?'([^']+)';/gm)) {
    const to = m[2].startsWith(".") ? path.posix.join(path.posix.dirname(f), m[2]).replace(/\.js$/, ".ts") : null;
    if (to && !files[to]) throw new Error("No file " + to);
    const ids = (m[1] || "").replace(/[{}]/g, "").split(",").map(x => x.trim().replace(/^type /, "").replace(/.* as /, "")).filter(Boolean);
    imports.push({ ids, to, from: m[2], l: [lineAt(t, m.index), lineAt(t, m.index + m[0].length)] });
  }
  const cl = lines.findIndex(x => /^(export )?(default )?(export default )?(abstract )?class \w+/.test(x));
  let styleIds = [];
  if (cl >= 0) {
    let hd = cl; while (!lines[hd].endsWith("{")) hd++;
    let end = hd; while (!/^\}/.test(lines[end])) end++;
    files[f].cls = lines[cl].match(/class (\w+)/)[1];
    for (const c of chunks(lines, hd + 1, end)) {
      const s = noDeco(c.code), m = s.match(/^((?:static |private |protected |public |readonly |async |get |set |override )*)(#?\w+)/);
      if (!m) throw new Error(f + ":" + c.l + " " + s);
      const mods = m[1].trim().split(" ").filter(Boolean), rest = s.slice(m[0].length), n = m[2];
      const deco = (c.code.match(/@(\w+)/g) || []).filter((_, i) => c.code.trimStart()[0] === "@").map(x => x.slice(1));
      const item = { n, l: [c.l, c.e], d: docText(c.doc), m: true };
      const tags = mods.filter(x => !["readonly", "async", "public"].includes(x));
      if (n === "css" && mods.includes("static") && /^ =/.test(rest)) {
        styleIds = [...rest.matchAll(/\b([a-zA-Z]\w*)\b/g)].map(x => x[1]);
        for (const id of styleIds) { const im = imports.find(x => x.ids.includes(id)); if (im) { im.used = true; add(f, "Styles", { n: path.posix.basename(im.to), l: item.l, to: { f: im.to } }); } }
        continue;
      }
      const accessor = mods.includes("get") || mods.includes("set");
      const method = !accessor && (/^[(<]/.test(rest) || /^\s*(:[^=]+)?=\s*(async )?(\([^=]*\)|\w+)(: [^=]+)?\s*=>/.test(rest));
      if (method) { item.tag = tags.join(" "); add(f, "Methods", item); continue; }
      const q = accessor ? null : rest.match(/^[?!]?(?::\s*([\s\S]*?))?(?:\s=\s([\s\S]*?))?;?(?:\s*\/\/.*)?$/);
      if (q) { if (q[1]) item.type = q[1].replace(/\s+/g, " "); if (q[2] && q[2].length < 60) item.v = q[2].replace(/\s+/g, " "); }
      if (item.type && /^'/.test(item.type)) item.vals = item.type.split("|").map(x => x.trim());
      const attr = deco.includes("property") && (c.code.match(/attribute: '([^']+)'/) || [])[1];
      if (attr) item.attr = attr; 
      if (accessor) item.tag = tags.join(" ");
      else if (deco.includes("state") || /state: true/.test(c.code.slice(0, 60)) && deco.includes("property")) { item.tag = "internal"; add(f, "States", item); continue; }
      else if (!deco.includes("property")) item.tag = [...tags, deco.includes("query") ? "element" : ""].filter(Boolean).join(" ") || "field";
      const same = (files[f].g.Properties || []).find(x => x.n === n);
      if (same && accessor) { same.l = [same.l[0], item.l[1]]; same.tag = same.tag.includes("get") || same.tag.includes("set") ? "get set" : same.tag; continue; }
      add(f, "Properties", item);
    }
    // public properties first, then the other members
    const p = files[f].g.Properties; if (p) p.sort((a, b) => !!a.tag - !!b.tag);
  }
  // functions, constants and types at the top level
  lines.forEach((x, i) => {
    const m = x.match(/^(export )?(?:default )?(?:async )?(function|const|let|interface|type) (\w+)/); if (!m || /css`/.test(x)) return;
    let e = i; if (!/;\s*$/.test(x)) while (e < lines.length - 1 && !/^[})\]]|;$/.test(lines[e + (e === i ? 1 : 0)]) && e - i < 400) e++;
    if (e > i || /\{\s*$/.test(x)) { e = i; while (e < lines.length - 1 && !/^[})\]].*$/.test(lines[e])) e++; }
    add(f, m[2] === "function" ? "Methods" : m[2] === "interface" || m[2] === "type" ? "Types" : "Properties", { n: m[3], l: [i + 1, e + 1], m: true, tag: m[2] === "function" ? "" : m[2] });
  });
  // documentation tags
  const parts = {}, slots = {};
  lines.forEach((x, i) => {
    const m = x.match(/^ \* @(event|slot|csspart|cssstate|cssproperty)\s+(.*)$/); if (!m) return;
    const k = m[2].match(/^(\S*?)\s*-\s+(.*)$/) || [, m[2], ""];
    let n = k[1], d = k[2]; const br = m[2].match(/^\[(--[\w-]+)(?:=([^\]]*))?\]\s*(?:-\s*)?(.*)$/); if (br) { n = br[1]; d = br[3] + (br[2] ? " Default: " + br[2] + "." : ""); } if (m[2].startsWith("- ")) { n = ""; d = m[2].slice(2); }
    const it = { n, d, l: [i + 1, i + 1] };
    if (m[1] === "event") add(f, "Events", it);
    if (m[1] === "cssstate") add(f, "States", it);
    if (m[1] === "cssproperty") add(f, "Properties", { ...it, tag: "CSS" });
    if (m[1] === "csspart") parts[n] = it;
    if (m[1] === "slot") slots[n] = it;
  });
  if (files[f].g.States) files[f].g.States.sort((a, b) => !!a.tag - !!b.tag);
  for (const m of t.matchAll(/dispatchEvent\(\s*new Wa(\w+)Event/g)) { const n = "wa-" + m[1].toLowerCase(), l = lineAt(t, m.index); const ex = (files[f].g.Events || []).find(x => x.n === n); if (ex) ex.by = l; else add(f, "Events", { n, l: [l, l], by: l }); }
  parseTemplate(f, parts, slots);
  for (const im of imports) if (!im.used && im.to) {
    const comp = im.to && im.to.match(/components\/(\w+)\/\1\.ts$/);
    if (im.to && (files[f].g.Uses || []).some(x => x.to && (x.to.f === im.to || comp && x.to.c === "wa-" + comp[1]))) continue;
    add(f, "Uses", { n: im.ids.join(", ") || path.posix.basename(im.to || im.from), l: im.l, v: im.to ? path.posix.basename(im.to) : im.from, to: im.to ? (comp ? { c: "wa-" + comp[1] } : { f: im.to }) : null, ids: im.ids, tag: im.to ? "" : "package" });
  }
  const ext = cl >= 0 && lines.slice(cl, cl + 3).join(" ").match(/extends (\w+)/);
  if (ext) { const u = (files[f].g.Uses || []).find(x => x.ids && x.ids.includes(ext[1])); if (u) { u.tag = "base class"; files[f].g.Uses.sort((a, b) => (b.tag === "base class") - (a.tag === "base class")); } }
}

// ---------- The template of render() ----------
function parseTemplate(f, parts, slots) {
  const t = files[f].text, at = t.search(/^ {2}render\(\) \{/m); if (at < 0) return;
  const end = t.indexOf("\n  }\n", at), body = t.slice(at, end);
  const stack = [], conds = []; const list = []; const re = /<(\/?)(\$\{tag\}|[a-z][\w-]*)|html`|`/g;
  let i = body.indexOf("html`"); if (i < 0) return;
  for (; i < body.length; i++) {
    const s = body.slice(i);
    let m;
    if ((m = s.match(/^\$\{\s*(this\.\w+)\s*\?\s*html`/))) { conds.push(m[1].slice(5)); i += m[0].length - 1; continue; }
    if (conds.length && s[0] === "`") { conds.pop(); continue; }
    if (/^\$\{/.test(s)) { let d = 0; do { if (body[i] === "{") d++; if (body[i] === "}") d--; i++; } while (d || body[i - 1] !== "}"); i--; continue; }
    if ((m = s.match(/^<\/(\$\{tag\}|[a-z][\w-]*)>/))) { stack.pop().l[1] = lineAt(t, at + i); i += m[0].length - 1; continue; }
    if ((m = s.match(/^<(\$\{tag\}|[a-z][\w-]*)/))) {
      let j = i + m[0].length, d = 0; while (d || body[j] !== ">") { if (body[j] === "{") d++; if (body[j] === "}") d--; j++; }
      const tx = body.slice(i, j + 1), a = Object.fromEntries([...tx.replace(/\$\{[^}]*\}/g, "").matchAll(/\s([\w-]+)="([^"]*)"/g)].map(x => [x[1], x[2]]));
      const name = m[1] === "${tag}" ? "button" : m[1], ps = (a.part || "").split(" ").filter(Boolean);
      const label = name === "slot" ? "slot " + (a.name || "(default)") : name;
      const it = { n: label, depth: stack.length, l: [lineAt(t, at + i), lineAt(t, at + j)], cond: conds[conds.length - 1] };
      if (m[1] === "${tag}") it.v = "a if href is set";
      const docs = []; if (name === "slot" && slots[a.name || ""]) docs.push(slots[a.name || ""].d);
      if (ps.length) { it.parts = ps; for (const p of ps) if (parts[p]) { if (!/^Deprecated/.test(parts[p].d)) docs.push(parts[p].d); parts[p].used = true; } }
      it.d = docs.join(" "); it.hl = name === "button" ? ".button" : name === "slot" ? (a.name ? `slot[name='${a.name}']` : "slot:not([name])") : name;
      const c = name.startsWith("wa-") && "components/" + name.slice(3) + "/" + name.slice(3) + ".ts"; if (c && files[c]) it.comp = name;
      add(f, "Elements", it);
      if (!/\/>$/.test(tx)) { it.u = [...it.l]; stack.push(it); }
      i = j;
    }
  }
  for (const p in parts) if (!parts[p].used) add(f, "Elements", { n: "part " + p, d: parts[p].d, l: parts[p].l, depth: 0 });
}

// ---------- CSS ----------
const tokens = {};
const isState = s => /:(hover|active|focus|focus-visible|disabled|state)\b|\.(is-|loading|disabled|no-label)|\[(disabled|loading)\]/.test(s.replace(/:not\([^()]*\)/g, ""));
function parseCss(f) {
  let css = files[f].text, base = 1;
  if (f.endsWith(".ts")) { const a = css.indexOf("css`") + 4; base = lineAt(css, a); css = css.slice(a, css.lastIndexOf("`")); }
  const lines = files[f].text.split("\n"), own = new Set(), used = new Map();
  for (const r of flatten(css, base)) {
    const sel = r.sel.join(", "), l = [r.l[0], Math.max(...r.decls.map(d => d[2][1]))]; if (/^\s*\}/.test(lines[l[1]] || "")) l[1]++;
    for (const [p, v, dl] of r.decls) {
      if (p.startsWith("--")) { own.add(p); (tokens[p] ||= []).push({ f, l: dl, sel, v }); }
      for (const m of v.matchAll(/var\(\s*(--[\w-]+)/g)) if (!used.has(m[1])) used.set(m[1], dl);
    }
    const rootLike = /(:root|\.wa-(theme|palette|light|dark|neutral-|brand-|success-|warning-|danger-))/.test(sel) && !/:host/.test(sel);
    if (rootLike) { for (const [p, v, dl] of r.decls) if (p.startsWith("--")) { if (!(files[f].g.Properties || []).some(x => x.n === p)) add(f, "Properties", { n: p, v: v.length < 48 ? v : "", l: dl, to: { t: p } }); } continue; }
    const g = r.sel.some(isState) ? "States" : /:host\(\[|^\.wa-/.test(sel) ? "Properties" : "Elements";
    add(f, g, { n: sel, l, hl: r.sel, rule: r.decls.map(([p, v, dl]) => [p, v, dl]), tag: r.media.length ? "@media " + r.media.join(" and ") : "" });
  }
  lines.forEach((x, i) => {
    const m = x.match(/^\s*@(keyframes [\w-]+|import url\('([^']+)'\))/); if (!m) return;
    if (m[2]) { const to = path.posix.join(path.posix.dirname(f), m[2]); add(f, "Uses", { n: path.posix.basename(to), l: [i + 1, i + 1], to: { f: to } }); return; }
    let e = i, d = 0; do { d += (lines[e].match(/\{/g) || []).length - (lines[e].match(/\}/g) || []).length; e++; } while (d);
    add(f, "Elements", { n: "@" + m[1], l: [i + 1, e], code: true });
  });
  for (const [n, l] of used) if (!own.has(n)) add(f, "Uses", { n, l, to: { t: n } });
}

// ---------- The manifest of ds-button ----------
function parseManifest(f) {
  const t = files[f].text, lines = t.split("\n"), d = JSON.parse(t).modules[0].declarations[0];
  const block = k => lines.findIndex(x => x.includes(`"${k}": [`));
  const where = (k, s) => { const b = block(k); for (let i = b; i < lines.length; i++) if (lines[i].includes(s)) { let e = i, c = 0; do { c += (lines[e].match(/[{]/g) || []).length - (lines[e].match(/[}]/g) || []).length; e++; } while (c > 0); return [i + 1, e]; } throw new Error(k + " " + s); };
  const one = s => { const i = lines.findIndex(x => x.includes(s)); if (i < 0) throw new Error(s); return [i + 1, i + 1]; };
  const L = t.indexOf('"anatomy"');
  add(f, "Elements", { n: d.ds.native, depth: 0, l: one('"native"'), hl: ".ds-button", v: d.ds.size.width + " × " + d.ds.size.height });
  for (const a of d.ds.anatomy) {
    const up = (files[f].g.Elements).find(x => x.n === a.parent), slot = d.slots.find(x => x.name === a.slot);
    const st = lines.findIndex(x => x.includes(`"part": "${a.part}",`) && !x.includes("{")); let e = st, c = 1; while (c) { e++; c += (lines[e].match(/[{]/g) || []).length - (lines[e].match(/[}]/g) || []).length; }
    add(f, "Elements", { n: a.part, depth: up ? up.depth + 1 : 1, l: [st, e + 1], parts: [a.part], hl: ".ds-button__" + a.part, d: slot ? slot.description : "", v: a.slot === "" ? "slot (default)" : a.kind,
      vals: a.sprites ? a.sprites.map(x => x.state + ": " + x.file) : null });
  }
  for (const a of d.attributes) add(f, "Properties", { n: a.name, type: a.type.text, l: where("attributes", `"${a.name}"`), tag: a.source });
  for (const s of d.cssStates) add(f, "States", { n: s.name, l: where("cssStates", `"${s.name}"`) });
  for (const e of d.events) add(f, "Events", { n: e.name, l: where("events", `"${e.name}"`), tag: e.source });
  add(f, "Styles", { n: "button.css", l: one('"path"'), to: { f: "ds/button.css" } });
  add(f, "Uses", { n: "tokens.css", l: one("from tokens.css"), to: { f: "ds/tokens.css" }, tag: "file" });
  const seen = new Set();
  for (const x of d.ds.tokens) for (const n of x.token.match(/--[\w-]+/g)) if (!seen.has(n)) { seen.add(n); add(f, "Uses", { n, l: one(`"${x.token}"`), to: { t: n } }); }
}

const pages = {};
for (const c of ["wa-button", "ds-button"]) pages[c] = norm(fs.readFileSync(path.join(here, "pages", c + ".html"), "utf8"));
const STRUCT = ["Elements", "Properties", "States", "Events", "Methods", "Styles", "Uses"], STYLE = ["Elements", "Properties", "States", "Uses"];
for (const f in files) { const style = /\.styles\.ts$|\.css$/.test(f); files[f].std = style ? STYLE : STRUCT; files[f].kind = style ? "style" : "structure"; if (style) parseCss(f); else if (f.endsWith(".json")) parseManifest(f); else parseTs(f); }
const comps = { "ds-button": ["ds/components.json", "ds/button.css"] };
for (const c of ["button", "icon", "spinner"]) comps["wa-" + c] = [`components/${c}/${c}.ts`, `components/${c}/${c}.styles.ts`];
const sum = { "ds-button": JSON.parse(files["ds/components.json"].text).modules[0].declarations[0].description };
for (const c of ["button", "icon", "spinner"]) sum["wa-" + c] = files[`components/${c}/${c}.ts`].text.match(/@summary ([\s\S]*?)\n \* @/)[1].replace(/\n \*\s*/g, " ");

// ---------- Links between the items of a file, and the checks ----------
let items = 0; const bad = [];
for (const f in files) {
  const F = files[f], lines = F.text.split("\n"), names = {};
  for (const g in F.g) F.g[g].forEach((x, i) => { if (x.m) names[x.n] = { f, g, i }; });
  const ids = {}; for (const [i, x] of (F.g.Uses || []).entries()) for (const id of x.ids || []) ids[id] = { f, g: "Uses", i };
  for (const g in F.g) F.g[g].forEach((x, i) => {
    items++;
    if (!(x.l && x.l[0] >= 1 && x.l[1] >= x.l[0] && x.l[1] <= lines.length)) bad.push([f, g, x.n, x.l]);
    const ul = x.u || x.l, code = lines.slice(ul[0] - 1, ul[1]).join("\n");
    const key = x.parts && x.n.startsWith("slot") ? null : x.n.replace(/^part /, "").split(/[ ,]/)[0];
    if (x.m && !code.includes(x.n)) bad.push([f, g, x.n, "name not in lines"]);
    const use = []; const put = u => { if (u && !use.some(y => JSON.stringify(y) === JSON.stringify(u)) && !(u.f === f && u.g === g && u.i === i)) use.push(u); };
    if ((x.m || x.hl && F.kind === "structure") && !f.endsWith(".json")) {
      for (const m of code.matchAll(/this\.(#?\w+)/g)) if (Object.hasOwn(names, m[1])) put(names[m[1]]);
      if (x.cond && names[x.cond]) put(names[x.cond]);
      for (const id in ids) if (new RegExp("\\b" + id + "\\b").test(code)) { const u = F.g.Uses[ids[id].i]; if (u.to) put(u.to); else put(ids[id]); }
      if (x.comp) put({ c: x.comp });
    }
    if (x.rule || x.code) for (const m of code.matchAll(/var\(\s*(--[\w-]+)/g)) put({ t: m[1] });
    if (g === "States" && !x.m && !x.rule) for (const n in names) { const y = F.g[names[n].g][names[n].i]; if (lines.slice(y.l[0] - 1, y.l[1]).join("\n").includes(`customStates.set('${x.n}'`)) put(names[n]); }
    if (x.by) for (const n in names) { const y = F.g[names[n].g][names[n].i]; if (y.l[0] <= x.by && y.l[1] >= x.by) put(names[n]); }
    if (use.length) x.use = use; delete x.ids; delete x.by; delete x.m; delete x.rule; for (const k in x) if (x[k] == null || x[k] === "" || x[k] === false) delete x[k];
  });
}
for (const f in files) for (const g in files[f].g) if (g === "Uses") for (const x of files[f].g[g]) if (x.to && x.to.t && !tokens[x.to.t]) { x.to = null; x.tag = "not set here"; }
console.log(Object.keys(files).length, "files", items, "items", Object.keys(tokens).length, "tokens", bad.length, "bad", bad.slice(0, 20));
if (process.argv[2] === "list") for (const c in comps) for (const f of comps[c]) { console.log("\n##", f); for (const g of files[f].std) console.log(g, (files[f].g[g] || []).length, "|", (files[f].g[g] || []).map(x => x.n + (x.tag ? " [" + x.tag + "]" : "") + ":" + x.l.join("-")).join("; ").slice(0, 1800)); }
if (bad.length) process.exit(1);
for (const c in pages) { const t = pages[c], i = t.split("\n").findIndex(x => x.includes(c === "wa-button" ? "<wa-button>" : '<button class')) + 1, e = t.split("\n").findIndex(x => x.includes(c === "wa-button" ? "</wa-button>" : "</button>")) + 1; files["page/" + c] = { name: c + ".html", path: "pages/" + c + ".html", text: t, g: {}, std: [], kind: "page", l: [i, e] }; }
const tpl = fs.readFileSync(path.join(here, "page.html"), "utf8"), font = fs.readFileSync(path.join(spike, "fonts/Inter-SemiBold.ttf")).toString("base64");
const js = o => JSON.stringify(o).replace(/</g, "\\u003c");
fs.writeFileSync(path.join(here, "index.html"), tpl.replace("/*DATA*/", () => `const DATA = ${js({ files, comps, sum, tokens })}, FONT = "${font}";`));
