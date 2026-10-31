/* Pure expression helpers: tokenising for the highlighter, variable discovery and type inference. */

const CONSTANTS = new Set(['pi', 'e']);
const BOOLS = new Set(['true', 'false']);
const TOKEN = /('(?:[^'\\]|\\.)*'?|"(?:[^"\\]|\\.)*"?)|(\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)|([\p{L}_$][\p{L}\p{N}_$]*)|(\s+)|([()[\],])|([+\-*/^%<>=!&|?:~∑√×÷−⊕]+)|(.)/gu;

/**
 * [{ t, s, i }] where t ∈ str | num | bool | const | fn | fn-unknown | var | op | punct | ws | other.
 * `fns` is the lower-cased set of documented functions; while it is empty every call counts as known.
 */
export const tokenize = (src, fns = new Set()) => {
    const out = [];
    for (const m of src.matchAll(TOKEN)) {
        const [s, str, num, id, ws, punct, op] = m;
        const i = m.index;
        let t = 'other';
        if (str) t = 'str';
        else if (num) t = 'num';
        else if (ws) t = 'ws';
        else if (punct) t = 'punct';
        else if (op) t = 'op';
        else if (id) {
            const lower = id.toLowerCase();
            if (/^\s*\(/.test(src.slice(i + id.length))) t = !fns.size || fns.has(lower) ? 'fn' : 'fn-unknown';
            else if (BOOLS.has(lower)) t = 'bool';
            else if (CONSTANTS.has(lower)) t = 'const';
            else t = 'var';
        }
        out.push({ t, s, i });
    }
    return out;
};

export const detectVars = (src, fns) => [...new Set(tokenize(src, fns).filter((k) => k.t === 'var').map((k) => k.s))];

const NUMERIC = /^[+-]?(\d+(\.\d*)?|\.\d+)([eE][+-]?\d+)?$/;
const DATEISH = /^\d{1,4}[/.-]\d{1,2}[/.-]\d{1,4}/;

/** What the engine will receive for this raw input. */
export const valueType = (v) => {
    const s = String(v ?? '').trim();
    if (/^(true|false)$/i.test(s)) return 'boolean';
    if (NUMERIC.test(s)) return 'number';
    if (/^\[.*\]$/s.test(s)) return 'list';
    if (DATEISH.test(s)) return 'date';
    return 'string';
};

export const formatToday = (pattern) => {
    const now = new Date();
    const parts = { yyyy: now.getFullYear(), MM: String(now.getMonth() + 1).padStart(2, '0'), dd: String(now.getDate()).padStart(2, '0'), M: now.getMonth() + 1, d: now.getDate() };
    return pattern.replace(/yyyy|MM|dd|M|d/g, (k) => parts[k]);
};

/** Function name and argument index of the call that encloses position `pos`, if any. */
const enclosingCall = (src, pos) => {
    let depth = 0;
    let commas = 0;
    for (let i = pos - 1; i >= 0; i--) {
        const c = src[i];
        if (c === ')') depth++;
        else if (c === '(') {
            if (depth === 0) {
                const name = /([\p{L}_$][\p{L}\p{N}_$]*)\s*$/u.exec(src.slice(0, i))?.[1];
                return name ? { name: name.toLowerCase(), index: commas } : null;
            }
            depth--;
        } else if (c === ',' && depth === 0) commas++;
    }
    return null;
};

const typeFromArg = (arg = '') => {
    const a = arg.toLowerCase();
    const hits = ['boolean', 'date', 'string', 'number'].filter((t) => a.includes(t));
    return hits.length === 1 ? hits[0] : null;
};

const CATEGORY_TYPE = { datetime: 'date', date: 'date', string: 'string' };

/** Best guess for a fresh variable: how it is used first, then the function argument it fills, then its name. */
export const inferDefault = (name, src, docs, dateFormat) => {
    const toks = tokenize(src, new Set(docs.byName.keys())).filter((k) => k.t !== 'ws');
    const at = toks.findIndex((k) => k.t === 'var' && k.s === name);
    let type = null;
    if (at >= 0) {
        const prev = toks[at - 1];
        const next = toks[at + 1];
        const ops = [prev, next].filter((k) => k?.t === 'op').map((k) => k.s);
        const strNeighbour = [toks[at - 2], toks[at + 2]].some((k) => k?.t === 'str');
        if (next?.t === 'op' && next.s === '?') type = 'boolean';
        else if (ops.some((o) => /^(&&|\|\||!)$/.test(o))) type = 'boolean';
        else if (ops.includes('+') && strNeighbour) type = 'string';
        else if (ops.length) type = 'number';
        else if (prev?.s === '(' || prev?.s === ',') {
            const ctx = enclosingCall(src, toks[at].i);
            const fn = ctx && docs.byName.get(ctx.name);
            if (fn) {
                const arg = fn.args[Math.min(ctx.index, fn.args.length - 1)] ?? '';
                // WHY: variadic "array" parameters take plain numbers; a list value is only for whole-array inputs
                type = arg.startsWith('array') ? (fn.category === 'Logical' ? 'boolean' : 'number') : typeFromArg(arg) ?? CATEGORY_TYPE[fn.category.toLowerCase()] ?? null;
            }
        }
    }
    if (!type || type === 'any') {
        if (/bool|flag|success|^is[A-Z_]|^has[A-Z_]/.test(name)) type = 'boolean';
        else if (/date|time|(^|_)dt/.test(name.toLowerCase())) type = 'date';
        else if (/^str_|name|text|label/i.test(name)) type = 'string';
        else if (/list|arr/i.test(name)) type = 'list';
    }
    return { boolean: 'true', date: formatToday(dateFormat), string: 'abc', list: '[1, 10, 100]' }[type] ?? '10';
};

/**
 * Turns UI variables into the engine call. List values ("[1, 2]") are spliced into the expression because
 * the engine takes variadic arguments, not arrays.
 */
export const toEngine = (src, vars) => {
    let expr = src;
    const dict = {};
    for (const { name, value } of vars) {
        const raw = String(value ?? '').trim();
        if (/^\[.*\]$/s.test(raw)) {
            const esc = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            expr = expr.replace(new RegExp(`(^|[^\\w$])${esc}(?![\\w$])`, 'g'), (_, p) => p + raw.slice(1, -1));
            continue;
        }
        const t = valueType(raw);
        dict[name] = t === 'number' ? Number(raw) : t === 'boolean' ? raw.toLowerCase() === 'true' : raw;
    }
    return { expr, dict, expanded: expr !== src };
};
