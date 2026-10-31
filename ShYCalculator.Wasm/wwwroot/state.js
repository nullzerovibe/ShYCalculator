/* App state and actions. Views read signals; only this module talks to the engine. */
import {
    signal, computed, effect, batch, createStore, themePref, engine, toast, copy, share, download, toCsv, pickFile, b64, confirm,
} from './nzv/nzv.js';
import * as eng from './engine.js';
import { detectVars, inferDefault, toEngine } from './syntax.js';
import { seedSnippets, iconFor, groupOfIcon } from './data.js';

const store = createStore('shy');
export const clearStore = () => store.clear();

/* ───────── persisted ───────── */

const SETTINGS = { dateFormat: 'dd/MM/yyyy', culture: 'en-US', enableHistory: true, historyLength: 15 };
const varList = (v) => (Array.isArray(v) ? v.filter((x) => x && typeof x.name === 'string').map((x) => ({ name: x.name, value: String(x.value ?? '') })) : []);

export const settings = store.signal('settings', SETTINGS, {
    migrate: 'shy_calc_settings',
    validate: (s) => {
        // WHY: the old app kept its own theme; carry it over once if the shared kit preference was never set
        if (s?.theme && localStorage.getItem('nzv:theme') === null) themePref.value = s.theme;
        const { theme, ...rest } = s ?? {};
        const n = Number.parseInt(rest.historyLength, 10);
        return { ...SETTINGS, ...rest, enableHistory: rest.enableHistory !== false, historyLength: Number.isFinite(n) && n > 0 ? Math.min(n, 100) : 15 };
    },
});

export const history = store.signal('history', [], {
    migrate: 'shy_calc_history',
    validate: (list) => (Array.isArray(list) ? list : []).filter((h) => typeof h?.expr === 'string')
        .map((h) => ({ expr: h.expr, result: String(h.result ?? ''), resultType: (h.resultType ?? '').toLowerCase(), vars: varList(h.vars), timestamp: h.timestamp ?? Date.now() })),
});

const toSnippet = (s, i) => ({
    id: String(s.id ?? `imported_${Date.now()}_${i}`),
    label: String(s.label).trim(),
    value: String(s.value),
    vars: varList(s.vars),
    icon: iconFor(s.icon),
    group: s.group || groupOfIcon(iconFor(s.icon)),
    pinned: !!s.pinned,
    ...(s.isSeeded ? { isSeeded: true } : {}),
});

const validSnippets = (list) => (Array.isArray(list) ? list : []).filter((s) => s?.label && s?.value).map(toSnippet);

export const snippets = store.signal('snippets', seedSnippets(), { migrate: 'shy_calc_snippets', validate: validSnippets });

/* ───────── session ───────── */

export const docs = signal({ functions: [], operators: [], byName: new Map() });
export const fnNames = computed(() => new Set(docs.value.byName.keys()));
export const appVersion = signal('');

export const expr = signal('');
/** User-entered (or loaded) variable values; names absent here fall back to an inferred default. */
export const values = signal({});
export const selectedSnippet = signal('');
export const showAst = signal(false);
export const busy = signal(false);
/** null | { ok, value, type, error, errors, ast, ms, seq } */
export const output = signal(null);
/** Live syntax check of the editor text: null | { ok, errors } */
export const check = signal(null);

export const ui = {
    help: signal(null),
    snippet: signal(null),
};

export const vars = computed(() => {
    const src = expr.value;
    const d = docs.value;
    const fmt = settings.value.dateFormat;
    return detectVars(src, fnNames.value).map((name) => {
        const own = values.value[name];
        return { name, value: own ?? inferDefault(name, src, d, fmt), auto: own === undefined };
    });
});

const plainVars = (list) => list.map(({ name, value }) => ({ name, value }));

export const setVar = (name, value) => { values.value = { ...values.value, [name]: value }; };

/* ───────── calculation ───────── */

let seq = 0;

export const calculate = async ({ record = true } = {}) => {
    if (engine.value !== 'ready') return;
    const src = expr.value;
    if (!src.trim()) {
        output.value = null;
        return;
    }
    const mine = ++seq;
    const list = plainVars(vars.value);
    const { expr: effective, dict, expanded } = toEngine(src, list);
    busy.value = true;
    const t0 = performance.now();
    try {
        const r = await eng.calculate(effective, dict, showAst.value);
        if (mine !== seq) return;
        output.value = { ...r, ms: performance.now() - t0, seq: mine, expanded };
        if (r.ok && record && settings.value.enableHistory) {
            const key = JSON.stringify(list);
            const next = [{ expr: src, result: r.value, resultType: r.type, vars: list, timestamp: Date.now() },
                ...history.value.filter((h) => h.expr !== src || JSON.stringify(h.vars) !== key)];
            history.value = next.slice(0, settings.value.historyLength);
        }
        writeUrl();
    } catch (e) {
        if (mine === seq) output.value = { ok: false, error: `Interop error: ${e.message}`, errors: [], seq: mine };
    } finally {
        if (mine === seq) busy.value = false;
    }
};

let checkTimer;
effect(() => {
    const src = expr.value;
    if (engine.value !== 'ready') return;
    clearTimeout(checkTimer);
    if (!src.trim()) {
        check.value = null;
        return;
    }
    checkTimer = setTimeout(async () => {
        const r = await eng.validate(src).catch(() => null);
        if (r && src === expr.value) check.value = r;
    }, 220);
});

export const validate = (src) => eng.validate(src);

/** Loads an expression with its saved variables and runs it. */
export const load = ({ value, vars: saved = [], id = '' }, { record = true } = {}) => {
    batch(() => {
        expr.value = value;
        values.value = Object.fromEntries(varList(saved).map((v) => [v.name, v.value]));
        selectedSnippet.value = id;
    });
    return calculate({ record });
};

export const setExpr = (value) => {
    batch(() => {
        expr.value = value;
        if (selectedSnippet.value && snippets.value.find((s) => s.id === selectedSnippet.value)?.value !== value) selectedSnippet.value = '';
    });
};

export const clearExpr = () => {
    batch(() => {
        expr.value = '';
        values.value = {};
        selectedSnippet.value = '';
        output.value = null;
    });
    writeUrl();
};

export const toggleAst = () => {
    showAst.value = !showAst.value;
    if (showAst.value && output.value?.ok && !output.value.ast) calculate({ record: false });
};

/* ───────── result I/O ───────── */

const hasResult = () => {
    if (output.value?.ok) return true;
    toast('Nothing to copy yet — calculate first', { tone: 'warn' });
    return false;
};

const equation = () => {
    const list = plainVars(vars.value);
    const where = list.length ? ` [where ${list.map((v) => `${v.name}=${v.value}`).join(', ')}]` : '';
    return `${expr.value}${where} = ${output.value.value}`;
};

export const copyResult = () => hasResult() && copy(output.value.value, 'Result copied');
export const copyEquation = () => hasResult() && copy(equation(), 'Equation copied');
export const copyJson = () => hasResult() && copy(JSON.stringify({
    expression: expr.value,
    variables: plainVars(vars.value),
    result: output.value.value,
    type: output.value.type,
    calculationTimeMs: Number(output.value.ms.toFixed(3)),
    timestamp: new Date().toISOString(),
}, null, 2), 'JSON copied');
export const shareResult = () => {
    if (!output.value?.ok) return toast('Nothing to share yet — calculate first', { tone: 'warn' });
    writeUrl();
    return share({ title: 'ShYCalculator', text: equation() });
};

/* ───────── deep links: ?e=<base64 expression>&v=<base64 JSON [{name,value}]> ───────── */

// WHY: links from the old app use standard base64, where a literal '+' may arrive as a space
const decodeParam = (s) => (s ? b64.decode(s.replace(/ /g, '+')) : null);

export const readUrl = () => {
    const q = new URLSearchParams(location.search);
    const e = decodeParam(q.get('e'));
    if (e === null) return null;
    let v = [];
    try { v = varList(JSON.parse(decodeParam(q.get('v')) ?? '[]')); } catch { /* malformed v: ignore */ }
    return { value: e, vars: v };
};

let restoring = true;
const writeUrl = () => {
    if (restoring) return;
    const url = new URL(location.href);
    const list = plainVars(vars.value).filter((v) => values.value[v.name] !== undefined);
    if (expr.value.trim()) url.searchParams.set('e', b64.encode(expr.value)); else url.searchParams.delete('e');
    if (list.length) url.searchParams.set('v', b64.encode(JSON.stringify(list))); else url.searchParams.delete('v');
    // WHY: `history` is the calculation-history signal here; the browser one is reached through globalThis
    if (url.href !== location.href) globalThis.history.replaceState(null, '', url);
};

let urlTimer;
effect(() => {
    expr.value;
    values.value;
    clearTimeout(urlTimer);
    urlTimer = setTimeout(writeUrl, 400);
});

addEventListener('popstate', () => {
    const link = readUrl();
    if (link) load(link, { record: false });
});

/* ───────── boot ───────── */

export const boot = async () => {
    const link = readUrl();
    if (link) batch(() => {
        expr.value = link.value;
        values.value = Object.fromEntries(link.vars.map((v) => [v.name, v.value]));
    });
    try {
        await eng.booted;
    } catch {
        output.value = { ok: false, error: 'The .NET engine failed to start. Reload the page to try again.', errors: [] };
        return;
    }
    const s = settings.value;
    await Promise.all([
        eng.configureDates(s.dateFormat, s.culture).catch(() => {}),
        eng.documentation().then((d) => { docs.value = d; }).catch((e) => console.error('[shy] documentation failed', e)),
        eng.version().then((v) => { appVersion.value = v; }).catch(() => {}),
    ]);
    restoring = false;
    if (expr.value.trim()) await calculate({ record: false });
};

let lastDates = '';
effect(() => {
    const { dateFormat, culture } = settings.value;
    if (engine.value !== 'ready' || restoring) return;
    const key = `${dateFormat}|${culture}`;
    if (key === lastDates) return;
    const first = !lastDates;
    lastDates = key;
    if (first) return;
    eng.configureDates(dateFormat, culture)
        .then(() => {
            toast(`Dates: ${dateFormat} · ${culture}`, { tone: 'ok', icon: 'calendar' });
            if (output.value) calculate({ record: false });
        })
        .catch((e) => toast(`Could not apply date settings: ${e.message}`, { tone: 'danger' }));
});

/* ───────── history ───────── */

export const removeHistory = (item) => { history.value = history.value.filter((h) => h !== item); };

export const clearHistory = async () => {
    if (!history.value.length) return;
    if (await confirm({ title: 'Clear history?', message: `Removes all ${history.value.length} entries stored on this device.`, danger: true, confirmLabel: 'Clear history' })) {
        history.value = [];
        toast('History cleared', { tone: 'ok', icon: 'trash-2' });
    }
};

export const exportHistory = (format) => {
    if (!history.value.length) return toast('History is empty', { tone: 'warn' });
    const stamp = new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-');
    if (format === 'json') return download(`shy-history-${stamp}.json`, JSON.stringify(history.value, null, 2), 'application/json');
    const rows = history.value.map((h) => ({
        Expression: h.expr, Result: h.result, Type: h.resultType, Variables: h.vars.map((v) => `${v.name}=${v.value}`).join('; '),
        Time: new Date(h.timestamp).toISOString(),
    }));
    return download(`shy-history-${stamp}.csv`, toCsv(rows), 'text/csv;charset=utf-8');
};

effect(() => {
    const max = settings.value.historyLength;
    if (history.value.length > max) history.value = history.value.slice(0, max);
});

/* ───────── snippets ───────── */

export const varsFor = (src, preferred = []) => {
    const own = Object.fromEntries(preferred.map((v) => [v.name, v.value]));
    return detectVars(src, fnNames.value).map((name) => ({
        name, value: own[name] ?? values.value[name] ?? inferDefault(name, src, docs.value, settings.value.dateFormat),
    }));
};

export const openSnippet = (s = null) => {
    ui.snippet.value = s ? { ...s } : { label: '', value: expr.value, icon: '', pinned: false };
};

export const saveSnippet = ({ id, label, value, icon, pinned }) => {
    const existing = id && snippets.value.find((s) => s.id === id);
    const item = {
        id: id || `s_${Date.now().toString(36)}`,
        label: label.trim(), value: value.trim(), icon, group: groupOfIcon(icon), pinned: !!pinned,
        vars: varsFor(value, existing?.vars ?? []),
    };
    snippets.value = existing ? snippets.value.map((s) => (s.id === id ? item : s)) : [...snippets.value, item];
    toast(existing ? `Updated “${item.label}”` : `Saved “${item.label}” to the library`, { tone: 'ok', icon: 'bookmark' });
    if (!existing || selectedSnippet.value === id) load(item, { record: false });
    return item;
};

export const togglePin = (id) => {
    snippets.value = snippets.value.map((s) => (s.id === id ? { ...s, pinned: !s.pinned } : s));
};

export const deleteSnippet = async (s) => {
    if (!await confirm({ title: 'Delete expression?', message: `“${s.label}” will be removed from your library.`, danger: true, confirmLabel: 'Delete' })) return false;
    snippets.value = snippets.value.filter((x) => x.id !== s.id);
    if (selectedSnippet.value === s.id) selectedSnippet.value = '';
    toast(`Deleted “${s.label}”`, { tone: 'ok', icon: 'trash-2' });
    return true;
};

export const exportSnippets = () => download(`shy-library-${new Date().toISOString().slice(0, 10)}.json`, JSON.stringify(snippets.value, null, 2), 'application/json');

/** Merge by name: same name + same formula overwrites, same name + new formula is added as "(Imported)". */
export const importSnippets = async () => {
    const text = await pickFile('.json,application/json');
    if (text === null) return;
    try {
        const incoming = validSnippets(JSON.parse(text));
        if (!incoming.length) throw new Error('no valid expressions found');
        const current = [...snippets.value];
        const ids = new Set(current.map((s) => s.id));
        let added = 0;
        let updated = 0;
        for (const s of incoming) {
            const twin = current.findIndex((c) => c.label.toLowerCase() === s.label.toLowerCase());
            if (twin >= 0 && current[twin].value === s.value) {
                current[twin] = { ...s, id: current[twin].id };
                updated++;
                continue;
            }
            const id = ids.has(s.id) ? `imported_${Date.now().toString(36)}_${added}` : s.id;
            ids.add(id);
            current.push({ ...s, id, label: twin >= 0 ? `${s.label} (Imported)` : s.label });
            added++;
        }
        snippets.value = current;
        toast(`Imported ${added} new, ${updated} updated`, { tone: 'ok', icon: 'upload' });
    } catch (e) {
        toast(`Import failed: ${e.message}`, { tone: 'danger' });
    }
};
