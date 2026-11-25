// Static parse of one Lit web component (Web Awesome dist build): no browser, no render.
// Reads component.js (class, properties, events, render template) and the style chunks
// that the class lists in `X.css = [...]`, then maps each CSS rule to parts and states.
// Run: node static-parse.mjs <component dir>   (dir has component.js, styles.json, style-*.js)
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const dir = process.argv[2];
const src = readFileSync(join(dir, 'component.js'), 'utf8');
const styleFiles = JSON.parse(readFileSync(join(dir, 'styles.json'), 'utf8'));

// ---------- small text helpers ----------
function matchClose(s, i, open, close) {
  // s[i] === open. Returns the index of the matching close. Skips strings and template literals.
  let depth = 0;
  for (let k = i; k < s.length; k++) {
    const ch = s[k];
    if (ch === '"' || ch === "'") { k = skipString(s, k, ch); continue; }
    if (ch === '`') { k = skipTemplate(s, k); continue; }
    if (ch === open) depth++;
    else if (ch === close && --depth === 0) return k;
  }
  return -1;
}
function skipString(s, i, q) { for (let k = i + 1; k < s.length; k++) { if (s[k] === '\\') k++; else if (s[k] === q) return k; } return s.length; }
function skipTemplate(s, i) {
  for (let k = i + 1; k < s.length; k++) {
    if (s[k] === '\\') k++;
    else if (s[k] === '`') return k;
    else if (s[k] === '$' && s[k + 1] === '{') k = matchClose(s, k + 1, '{', '}');
  }
  return s.length;
}
function templateBody(s, tickIndex) { return s.slice(tickIndex + 1, skipTemplate(s, tickIndex)); }
// Splits a template body into static text with markers \u0001N\u0001 and the list of ${} expressions.
function splitTemplate(body) {
  const exprs = []; let out = '';
  for (let k = 0; k < body.length; k++) {
    if (body[k] === '$' && body[k + 1] === '{') {
      const end = matchClose(body, k + 1, '{', '}');
      exprs.push(body.slice(k + 2, end).trim());
      out += `\u0001${exprs.length - 1}\u0001`;
      k = end;
    } else out += body[k];
  }
  return { text: out, exprs };
}
const squash = s => s.replace(/\s+/g, ' ').trim();

// ---------- class: tag, properties, events ----------
const tag = src.match(/customElement\("([\w-]+)"\)/)?.[1];
const ctorDefaults = {};
for (const m of src.matchAll(/this\.(\w+) = ([^;]+);/g)) if (!(m[1] in ctorDefaults)) ctorDefaults[m[1]] = m[2].trim();
const properties = [];
for (const m of src.matchAll(/__decorateClass\(\[\s*property\(([^)]*)\)\s*\], \w+\.prototype, "(\w+)", 2\)/g)) {
  const opts = m[1];
  properties.push({
    name: m[2],
    attribute: opts.match(/attribute: (?:"([^"]+)"|(false))/)?.slice(1).find(Boolean) ?? m[2].toLowerCase(),
    type: opts.match(/type: (\w+)/)?.[1] ?? 'String',
    reflect: /reflect: true/.test(opts),
    default: ctorDefaults[m[2]] ?? null,
  });
}
const events = [];
for (const m of src.matchAll(/dispatchEvent\(new (\w+)\(([^)]*)\)/g)) {
  const [, cls, args] = m;
  let name = args.match(/^\s*["']([^"']+)["']/)?.[1];
  if (!name && /^Wa\w+Event$/.test(cls)) name = 'wa-' + cls.slice(2, -5).replace(/[A-Z]/g, c => '-' + c.toLowerCase()).replace(/^-/, '');
  if (name && !events.some(e => e.name === name)) events.push({ name, source: `new ${cls}(${squash(args)})` });
}

// ---------- render template -> anatomy tree ----------
const renderStart = src.indexOf('render() {');
const renderBody = src.slice(renderStart, matchClose(src, src.indexOf('{', renderStart), '{', '}') + 1);
const localLiterals = {};
for (const m of renderBody.matchAll(/const (\w+) = ([^;]+);/g)) {
  const lits = [...m[2].matchAll(/literal`(\w+)`/g)].map(x => x[1]);
  if (lits.length) localLiterals[m[1]] = lits;
}
const retTick = renderBody.indexOf('html`', renderBody.indexOf('return html`')) + 4;
const VOID = new Set(['input', 'img', 'br', 'hr', 'meta', 'link', 'source', 'col', 'area', 'wbr']);

function parseClassMap(expr) {
  const body = expr.match(/classMap\(\{([\s\S]*)\}\)/)?.[1];
  if (!body) return null;
  const out = [];
  let depth = 0, cur = '';
  for (const ch of body) { if ('([{'.includes(ch)) depth++; if (')]}'.includes(ch)) depth--; if (ch === ',' && depth === 0) { out.push(cur); cur = ''; } else cur += ch; }
  if (cur.trim()) out.push(cur);
  return out.map(p => { const i = p.indexOf(':'); const when = squash(p.slice(i + 1)); return { name: p.slice(0, i).trim().replace(/^["']|["']$/g, ''), when: when === 'true' ? 'always' : when }; });
}

function parseTemplate(body, condition) {
  const { text, exprs } = splitTemplate(body);
  const root = { children: [] }; const stack = [root];
  const re = /<(\/?)([a-zA-Z][\w-]*|\u0001\d+\u0001)((?:[^>"']|"[^"]*"|'[^']*')*?)(\/?)>|\u0001(\d+)\u0001/g;
  let m;
  while ((m = re.exec(text))) {
    const parent = stack[stack.length - 1];
    if (m[5] !== undefined) { // a ${} in content: conditional or dynamic children
      const expr = exprs[+m[5]];
      const branches = [];
      for (const hm of expr.matchAll(/html`/g)) {
        const tick = hm.index + 4;
        const before = expr.slice(0, hm.index);
        const isElse = /:\s*$/.test(before.trimEnd()) && before.includes('?');
        const test = squash(expr.slice(0, expr.indexOf('?')));
        branches.push({ when: isElse ? `!(${test})` : test, nodes: parseTemplate(templateBody(expr, tick)).children });
      }
      if (branches.length) for (const b of branches) for (const n of b.nodes) { n.when = b.when; parent.children.push(n); }
      else parent.children.push({ text: '${' + squash(expr) + '}' });
      continue;
    }
    const [, closing, rawName, rawAttrs, selfClose] = m;
    let name = rawName;
    const dyn = rawName.match(/\u0001(\d+)\u0001/);
    if (dyn) { const e = exprs[+dyn[1]]; name = (localLiterals[e] ?? [e]).join('|'); }
    if (closing) { while (stack.length > 1) { const n = stack.pop(); if (n.tag === name) break; } continue; }
    const node = { tag: name, attrs: {}, children: [] };
    for (const a of rawAttrs.matchAll(/([^\s=]+)(?:=("[^"]*"|'[^']*'|[^\s>]+))?/g)) {
      let v = a[2] ?? true;
      if (typeof v === 'string') v = v.replace(/^["']|["']$/g, '');
      if (typeof v === 'string' && /\u0001\d+\u0001/.test(v)) {
        const e = exprs[+v.match(/\u0001(\d+)\u0001/)[1]];
        if (a[1] === 'class' && /classMap/.test(e)) { node.classMap = parseClassMap(e); continue; }
        v = '${' + squash(e) + '}';
      }
      node.attrs[a[1]] = v;
    }
    if (condition) node.when = condition;
    parent.children.push(node);
    if (!selfClose && !VOID.has(name)) stack.push(node);
  }
  return root;
}
const tree = parseTemplate(templateBody(renderBody, retTick)).children;

// Flatten the tree for the part and class index.
const nodes = [];
(function walk(list, path, when) {
  for (const n of list) {
    if (!n.tag) continue;
    const w = [when, n.when].filter(Boolean).join(' && ') || undefined;
    const parts = (n.attrs.part ?? '').split(/\s+/).filter(Boolean);
    const staticClasses = typeof n.attrs.class === 'string' && !n.attrs.class.startsWith('${') ? n.attrs.class.split(/\s+/) : [];
    const entry = {
      path: [...path, n.tag].join(' > '), tag: n.tag, parts, when: w,
      classes: [...staticClasses.map(c => ({ name: c, when: 'always' })), ...(n.classMap ?? [])],
      slot: n.tag === 'slot' ? (n.attrs.name ?? '(default)') : undefined,
      component: n.tag.startsWith('wa-') ? n.tag : undefined,
    };
    nodes.push(entry);
    walk(n.children, [...path, n.tag + (parts[0] ? `[${parts[0]}]` : '')], w);
  }
})(tree, [], undefined);

// ---------- CSS ----------
function cssText(file) {
  const s = readFileSync(join(dir, file), 'utf8');
  const i = s.indexOf('css`');
  return templateBody(s, i + 3).replace(/\/\*[\s\S]*?\*\//g, '');
}
function splitTop(s, sep) { const out = []; let d = 0, cur = ''; for (const ch of s) { if ('(['.includes(ch)) d++; if (')]'.includes(ch)) d--; if (ch === sep && d === 0) { out.push(cur); cur = ''; } else cur += ch; } out.push(cur); return out.map(x => x.trim()).filter(Boolean); }
function combine(parents, sel) {
  const own = splitTop(sel, ',');
  if (!parents.length) return own;
  const out = [];
  for (const p of parents) for (const o of own) out.push(o.includes('&') ? o.replaceAll('&', p) : `${p} ${o}`);
  return out;
}
function parseCss(text, file) {
  const rules = [];
  function block(s, selectors, media) {
    let i = 0; const decls = [];
    while (i < s.length) {
      const brace = s.indexOf('{', i), semi = s.indexOf(';', i);
      if (brace === -1 && semi === -1) break;
      if (semi !== -1 && (brace === -1 || semi < brace)) {
        const d = s.slice(i, semi).trim(); i = semi + 1;
        const c = d.indexOf(':'); if (c > 0) decls.push({ prop: d.slice(0, c).trim(), value: squash(d.slice(c + 1)) });
        continue;
      }
      const prelude = squash(s.slice(i, brace)); const end = matchClose(s, brace, '{', '}');
      const inner = s.slice(brace + 1, end); i = end + 1;
      if (prelude.startsWith('@layer') || prelude.startsWith('@supports')) block(inner, selectors, media);
      else if (prelude.startsWith('@media') || prelude.startsWith('@container')) block(inner, selectors, [...media, prelude]);
      else if (prelude.startsWith('@keyframes') || prelude.startsWith('@font-face') || prelude.startsWith('@property')) continue;
      else block(inner, combine(selectors, prelude), media);
    }
    if (decls.length && selectors.length) rules.push({ file, selectors, media, decls });
  }
  block(text, [], []);
  return rules;
}
function tokensIn(value) { return [...value.matchAll(/var\((--[\w-]+)/g)].map(m => m[1]); }

// ---------- rule -> target parts + states ----------
const classIndex = new Map(); // class -> [{node, when}]
for (const n of nodes) for (const c of n.classes) { if (!classIndex.has(c.name)) classIndex.set(c.name, []); classIndex.get(c.name).push({ node: n, when: c.when }); }
const PSEUDO_STATES = ['hover', 'active', 'focus-visible', 'focus-within', 'focus', 'disabled', 'checked', 'indeterminate', 'invalid', 'user-invalid', 'placeholder-shown', 'empty'];

function analyzeSelector(sel) {
  const states = []; const unknown = [];
  // host states: :host([attr]) :host([attr='v']) :host(:state(x)) :host(:hover) ...
  for (const h of sel.matchAll(/:host\(((?:[^()]|\([^()]*\))*)\)/g)) {
    for (const a of h[1].matchAll(/\[([\w-]+)(?:[~|^$*]?=['"]?([^'"\]]+)['"]?)?\]/g)) states.push(`host[${a[1]}${a[2] ? '=' + a[2] : ''}]`);
    for (const s of h[1].matchAll(/:state\(([\w-]+)\)/g)) states.push(`host:state(${s[1]})`);
    for (const p of PSEUDO_STATES) if (new RegExp(`:${p}(?![\\w-])`).test(h[1].replace(/:state\([^)]*\)/g, ''))) states.push(`host:${p}`);
  }
  const rest = sel.replace(/:host\(((?:[^()]|\([^()]*\))*)\)/g, ':host');
  // the subject = last compound selector (split on combinators outside brackets and parentheses)
  const compounds = []; { let d = 0, cur = ''; for (const ch of rest) { if ('(['.includes(ch)) d++; if (')]'.includes(ch)) d--; if (d === 0 && /[\s>+~]/.test(ch)) { if (cur) compounds.push(cur); cur = ''; } else cur += ch; } if (cur) compounds.push(cur); }
  let subject = compounds[compounds.length - 1] ?? '';
  // :is(a, b) and :where(a, b) in the subject: use the first alternative that names a part
  const isList = subject.match(/^:(?:is|where)\(((?:[^()]|\([^()]*\))*)\)/);
  if (isList) { const alts = splitTop(isList[1], ','); subject = (alts.find(a => /part/.test(a)) ?? alts[0]) + subject.slice(isList[0].length); }
  // A rule that starts at :root or a page class is a theme rule outside the component.
  if (/^(:where\()?:root|^\.wa-[\w-]+$/.test(sel.trim())) return { targets: ['global (page theme)'], states: [], unknown: [] };
  // negated pieces
  for (const n of rest.matchAll(/:not\(([^()]*)\)/g)) for (const c of n[1].matchAll(/\.([\w-]+)/g)) states.push(`not .${c[1]}`);
  const plain = rest.replace(/:not\([^()]*\)/g, '');
  for (const p of PSEUDO_STATES) if (new RegExp(`:${p}(?![\\w-])`).test(plain)) states.push(`:${p}`);
  for (const s of plain.matchAll(/:state\(([\w-]+)\)/g)) states.push(`:state(${s[1]})`);
  // classes that are conditional in the template are states, the others select an element
  const targets = new Set(); let targetHost = false;
  const subjectPlain = subject.replace(/:not\([^()]*\)/g, '');
  const subjClasses = [...subjectPlain.matchAll(/\.([\w-]+)/g)].map(m => m[1]);
  const subjTag = subjectPlain.match(/^([a-z][\w-]*)/)?.[1];
  const subjPart = subjectPlain.match(/::part\(([\w-]+)\)|\[part~?=['"]?([\w-]+)/);
  for (const c of [...plain.matchAll(/\.([\w-]+)/g)].map(m => m[1])) {
    const hits = classIndex.get(c);
    if (!hits) { unknown.push(`.${c}`); continue; }
    if (hits.every(h => h.when !== 'always')) states.push(`.${c} (${hits[0].when})`);
  }
  if (/::slotted\(/.test(subject)) targets.add('slotted content: ' + subject.match(/::slotted\(([^)]*)\)/)[1]);
  else if (subjPart) targets.add(`part:${subjPart[1] ?? subjPart[2]}`);
  else if (subjClasses.length) {
    const always = subjClasses.filter(c => classIndex.get(c)?.some(h => h.when === 'always'));
    const pick = always.length ? always : subjClasses;
    for (const c of pick) for (const h of classIndex.get(c) ?? []) targets.add(h.node.parts[0] ? `part:${h.node.parts[0]}` : h.node.path);
  } else if (subjTag) {
    const hit = nodes.filter(n => n.tag === subjTag || n.tag.split('|').includes(subjTag));
    if (hit.length) hit.forEach(h => targets.add(h.parts[0] ? `part:${h.parts[0]}` : h.path)); else unknown.push(subjTag);
  } else if (/^:host/.test(subject) || subject === '') targetHost = true;
  if (!targets.size && !targetHost && /^:host/.test(subject)) targetHost = true;
  return { targets: targetHost ? ['host', ...targets] : [...targets], states: [...new Set(states)], unknown };
}

const rules = styleFiles.flatMap(f => parseCss(cssText(f), f));
const bindings = [];
for (const r of rules) for (const sel of r.selectors) {
  const a = analyzeSelector(sel);
  bindings.push({
    file: r.file, selector: sel, media: r.media.length ? r.media : undefined,
    targets: a.targets, states: a.states, unknown: a.unknown.length ? a.unknown : undefined,
    decls: r.decls.map(d => ({ prop: d.prop, value: d.value, tokens: tokensIn(d.value) })).filter(d => d.tokens.length || !d.prop.startsWith('--') || true),
  });
}

// Tokens that the component itself defines (custom properties set inside its rules).
const definedTokens = {};
for (const b of bindings) for (const d of b.decls) if (d.prop.startsWith('--')) (definedTokens[d.prop] ??= []).push({ selector: b.selector, value: d.value });

const result = {
  tag, properties, events,
  anatomy: tree,
  parts: nodes.flatMap(n => n.parts.map(p => ({ part: p, tag: n.tag, path: n.path, when: n.when }))),
  slots: nodes.filter(n => n.slot).map(n => ({ name: n.slot, parts: n.parts, when: n.when })),
  dependencies: [...new Set(nodes.filter(n => n.component).map(n => n.component))],
  states: [...new Set(bindings.flatMap(b => b.states))].sort(),
  definedTokens,
  bindings,
  unmapped: bindings.filter(b => !b.targets.length || b.unknown).map(b => ({ selector: b.selector, unknown: b.unknown })),
};
console.log(JSON.stringify(result, (k, v) => (k === 'children' && Array.isArray(v) && !v.length ? undefined : v), 2));
