/* The .NET boundary. Everything that crosses it is normalised here, so the UI never sees Name/name dual casing. */
import { startDotnet } from './nzv/nzv.js';

let ref;
export const booted = startDotnet('registerShYCalculator').then((r) => { ref = r; });

const call = (method, ...args) => ref.invokeMethodAsync(method, ...args);

// WHY: Blazor's interop serializer is camelCase, but the hand-serialized JSON strings are PascalCase
const get = (o, key) => o?.[key] ?? o?.[key[0].toUpperCase() + key.slice(1)];

const TYPES = { 1: 'number', 2: 'boolean', 4: 'date', 8: 'string' };

const toErrors = (list) => (list ?? []).map((e) => ({
    message: get(e, 'message') ?? '',
    start: get(e, 'startIndex') ?? -1,
    length: get(e, 'length') ?? 0,
}));

/** { ok, value, type, error, errors, ast } */
const toResult = (r) => {
    if (!get(r, 'success')) {
        return { ok: false, error: get(r, 'message') || 'Unknown error', errors: toErrors(get(r, 'errors')) };
    }
    const value = ['mvalue', 'nvalue', 'bvalue', 'svalue', 'dvalue'].map((k) => get(r, k)).find((v) => v !== null && v !== undefined);
    return { ok: true, value: value === undefined ? 'null' : String(value), type: TYPES[get(r, 'dataType')] ?? '', ast: get(r, 'ast') ?? null };
};

export const calculate = async (expr, dict, includeAst) => toResult(Object.keys(dict).length
    ? await call('CalculateWithVars', expr, dict, includeAst)
    : await call('Calculate', expr, includeAst));

export const validate = async (expr) => {
    const r = JSON.parse(await call('ValidateExpression', expr, false));
    return { ok: !!get(r, 'success'), message: get(r, 'message') ?? '', errors: toErrors(get(r, 'errors')) };
};

export const configureDates = (format, culture) => call('ConfigureDates', format, culture);

export const version = async () => (await call('GetVersion')).split('.').slice(0, 3).join('.');

export const documentation = async () => {
    const raw = JSON.parse(await call('GetDocumentation'));
    const functions = (get(raw, 'functions') ?? []).map((f) => ({
        name: get(f, 'name') ?? '',
        description: get(f, 'description') ?? '',
        examples: get(f, 'examples') ?? [],
        args: get(f, 'arguments') ?? [],
        category: get(f, 'category') ?? 'General',
    }));
    const operators = (get(raw, 'operators') ?? []).map((o) => ({
        symbol: get(o, 'symbol') ?? '',
        name: get(o, 'name') ?? '',
        precedence: get(o, 'precedence') ?? 0,
        associativity: get(o, 'associativity') ?? 'Left',
        category: get(o, 'category') ?? 'General',
    }));
    return { functions, operators, byName: new Map(functions.map((f) => [f.name.toLowerCase(), f])) };
};
