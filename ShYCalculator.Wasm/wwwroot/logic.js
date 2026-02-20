import { signal, effect } from 'https://esm.sh/@preact/signals@1.2.2?deps=preact@10.19.3';

// --- CONFIGURATION & CONSTANTS ---
export const STORAGE_KEY_HISTORY = 'shy_calc_history';
export const STORAGE_KEY_SETTINGS = 'shy_calc_settings';
export const STORAGE_KEY_SNIPPETS = 'shy_calc_snippets';

export const EXAMPLE_GROUPS = [
    {
        label: "Basic",
        icon: "calculator",
        items: [
            { label: "Simple Arithmetic", value: "1 + 2 * 3" },
            { label: "Division & Addition", value: "10 / 2 + 5" },
            { label: "Power (2^3)", value: "2 ^ 3" }
        ]
    },
    {
        label: "Date & Time",
        icon: "calendar",
        items: [
            { label: "Current Date", value: "dt_now()" },
            { label: "Today (Midnight)", value: "dt_today()" },
            { label: "Add 1 Year", value: "dt_add(dt_today(), '1y')" },
            { label: "Day of Week", value: "dt_dayofweek(dt_now())" },
            { label: "Current Year", value: "dt_year(dt_now())" }
        ]
    },
    {
        label: "String Operations",
        icon: "type",
        items: [
            { label: "Contains 'World'?", value: "str_contains('Hello World', 'World')" },
            { label: "Case-Insensitive Equal", value: "str_equal('abc', 'ABC', true)" },
            { label: "Starts With 'Hello'", value: "str_starts('Hello World', 'Hello')" },
            { label: "Ends With '.txt'", value: "str_ends('test.txt', '.txt')" }
        ]
    },
    {
        label: "Scientific",
        icon: "sigma",
        items: [
            { label: "Trigonometry", value: "sin(0) + cos(0)" },
            { label: "Logarithm (base 10)", value: "log(100, 10)" },
            { label: "Roots & Absolute", value: "sqrt(16) + abs(-5)" }
        ]
    },
    {
        label: "Logic",
        icon: "cpu",
        items: [
            { label: "Logical AND", value: "true && false" },
            { label: "Logical OR", value: "true || false" },
            { label: "Logical NOT", value: "!true" },
            { label: "Combined Logic", value: "(5 > 3) && (2 < 4)" },
            { label: "Simple If/Else", value: "if(5 > 3, 100, 0)" },
            { label: "Max & Min", value: "max(10, 20) + min(5, 1)" },
            { label: "Ternary Operator", value: "true ? 1 : 0" }
        ]
    },
    {
        label: "Bitwise Operations",
        icon: "binary",
        items: [
            { label: "Bitwise AND", value: "5 & 3" },
            { label: "Bitwise OR", value: "5 | 3" },
            { label: "Bitwise XOR", value: "5 ^^ 3" },
            { label: "Bitwise NOT", value: "~5" },
            { label: "Bitwise Left Shift", value: "8 << 2" },
            { label: "Bitwise Right Shift", value: "16 >> 2" }
        ]
    },
    {
        label: "Expressions (Uses Variables)",
        icon: "square-function",
        items: [
            { label: "Area of Circle", value: "pi * r * r", vars: [{ name: "r", value: "5" }, { name: "pi", value: "3.14159" }] },
            { label: "Pythagoras", value: "a * a + b * b", vars: [{ name: "a", value: "3" }, { name: "b", value: "4" }] },
            { label: "Linear Equation", value: "offset + scale * x", vars: [{ name: "offset", value: "10" }, { name: "scale", value: "2" }, { name: "x", value: "5" }] }
        ]
    },
    {
        label: "Complex Scenarios",
        icon: "layers",
        items: [
            {
                label: "Driver Qualification",
                value: "(age >= 18 && any(hasLicense, false)) ? (if(max(score, 0) > 80, 'Qualified', 'Needs Retake')) : default",
                vars: [{ name: "age", value: "20" }, { name: "hasLicense", value: "true" }, { name: "score", value: "85" }, { name: "default", value: "Ineligible" }]
            },
            {
                label: "Shipping Calculator",
                value: "if(weight <= 0, 'Invalid Weight', 'Total: $' + round(max(5, weight * rate) + (express ? 10 : 0), 2))",
                vars: [{ name: "weight", value: "12.5" }, { name: "rate", value: "0.8" }, { name: "express", value: "true" }]
            }
        ]
    }
];

// Flatten for lookup
let globalIndex = 0;
export const FLAT_MAP = [];
EXAMPLE_GROUPS.forEach(g => {
    g.items.forEach(item => {
        item._idx = globalIndex++;
        FLAT_MAP.push(item);
    });
});

export const getCategoryIconUrl = (cat) => {
    const c = (cat || '').trim().toLowerCase();
    let icon = 'help-circle';
    switch (c) {
        case 'arithmetic':
        case 'arithmetical':
        case 'exponent':
        case 'power':
        case 'superscript': icon = 'superscript'; break;
        case 'divide':
        case 'multiply':
        case 'calculator': icon = 'calculator'; break;
        case 'plus':
        case 'unary': icon = 'plus'; break;
        case 'minus': icon = 'minus'; break;
        case 'comparison':
        case 'equal': icon = 'equal'; break;
        case 'grouping':
        case 'parentheses': icon = 'parentheses'; break;
        case 'logical':
        case 'logic': icon = 'circuit-board'; break;
        case 'cpu': icon = 'cpu'; break;
        case 'bitwise':
        case 'bitwise operations':
        case 'binary': icon = 'binary'; break;
        case 'on/off':
        case 'switch':
        case 'power-button': icon = 'power'; break;
        case 'scientific':
        case 'sigma': icon = 'sigma'; break;
        case 'chemistry':
        case 'flask-conical': icon = 'flask-conical'; break;
        case 'physics':
        case 'atom': icon = 'atom'; break;
        case 'infinity': icon = 'infinity'; break;
        case 'brain': icon = 'brain'; break;
        case 'activity': icon = 'activity'; break;
        case 'sort':
        case 'sort-asc': icon = 'arrow-up-0-1'; break;
        case 'tally':
        case 'count': icon = 'tally-5'; break;
        case 'list-tree':
        case 'logic tree': icon = 'list-tree'; break;
        case 'network': icon = 'network'; break;
        case 'numeric':
        case 'hash': icon = 'hash'; break;
        case 'list-ordered':
        case 'list': icon = 'list-ordered'; break;
        case 'percent': icon = 'percent'; break;
        case 'dice':
        case 'dice-5': icon = 'dice-5'; break;
        case 'string':
        case 'string operations':
        case 'type': icon = 'type'; break;
        case 'text-select':
        case 'select text': icon = 'text-select'; break;
        case 'message':
        case 'message-square': icon = 'message-square'; break;
        case 'tags': icon = 'tags'; break;
        case 'quote':
        case 'string quote': icon = 'quote'; break;
        case 'regex':
        case 'regular expression': icon = 'regex'; break;
        case 'date':
        case 'date & time':
        case 'calendar': icon = 'calendar'; break;
        case 'time':
        case 'history': icon = 'history'; break;
        case 'timer':
        case 'stopwatch': icon = 'timer'; break;
        case 'hourglass': icon = 'hourglass'; break;
        case 'sun': icon = 'sun'; break;
        case 'moon': icon = 'moon'; break;
        case 'array':
        case 'complex scenarios':
        case 'layers': icon = 'layers'; break;
        case 'all_categories':
        case 'layout-grid': icon = 'layout-grid'; break;
        case 'square-function':
        case 'function-square':
        case 'expressions (uses variables)': icon = 'square-function'; break;
        case 'rocket': icon = 'rocket'; break;
        case 'heart': icon = 'heart'; break;
        case 'star': icon = 'star'; break;
        case 'trophy':
        case 'winner': icon = 'trophy'; break;
        case 'crown':
        case 'king': icon = 'crown'; break;
        case 'zap':
        case 'lightning':
        case 'flash': icon = 'zap'; break;
        case 'bookmark': icon = 'bookmark'; break;
        case 'wrench':
        case 'tools':
        case 'tool': icon = 'wrench'; break;
    }
    return `https://api.iconify.design/lucide/${icon}.svg`;
};

export const getTypeIconUrl = (type) => {
    const t = (type || '').trim().toLowerCase();
    let icon = 'help-circle';
    switch (t) {
        case 'number': icon = 'hash'; break;
        case 'string': icon = 'type'; break;
        case 'date': icon = 'calendar'; break;
        case 'boolean': icon = 'toggle-left'; break;
        case 'array': icon = 'layers'; break;
    }
    return `https://api.iconify.design/lucide/${icon}.svg`;
};

// --- PERSISTENCE UTILS ---
const loadSavedHistory = () => {
    try {
        const saved = localStorage.getItem(STORAGE_KEY_HISTORY);
        return saved ? JSON.parse(saved) : [];
    } catch { return []; }
};

const loadSnippets = () => {
    try {
        const saved = localStorage.getItem(STORAGE_KEY_SNIPPETS);
        if (saved) return JSON.parse(saved);

        // Seed from EXAMPLE_GROUPS on first load
        const seeded = [];
        let idCount = 0;
        EXAMPLE_GROUPS.forEach(group => {
            group.items.forEach(item => {
                seeded.push({
                    id: `seeded_${idCount++}`,
                    label: item.label,
                    value: item.value,
                    vars: item.vars || [],
                    icon: group.icon || 'bookmark',
                    group: group.label,
                    isSeeded: true
                });
            });
        });
        return seeded;
    } catch { return []; }
};

const loadSavedSettings = () => {
    const defaults = { dateFormat: 'dd/MM/yyyy', culture: 'en-US', enableHistory: true, historyLength: 15, theme: 'auto' };
    try {
        const saved = localStorage.getItem(STORAGE_KEY_SETTINGS);
        if (!saved) return defaults;
        return { ...defaults, ...JSON.parse(saved) };
    } catch { return defaults; }
};

const formatDate = (pattern) => {
    const now = new Date();
    const d = String(now.getDate()).padStart(2, '0');
    const m = String(now.getMonth() + 1).padStart(2, '0');
    const y = now.getFullYear();

    return pattern
        .replace('yyyy', y)
        .replace('MM', m)
        .replace('dd', d)
        .replace('M', now.getMonth() + 1)
        .replace('d', now.getDate());
};

const inferVariables = (expr) => {
    if (!expr) return [];

    const strippedExpr = expr.replace(/'[^']*'|"[^"]*"/g, m => ' '.repeat(m.length));
    const idRegex = /(?:^|[^a-zA-Z0-9_$])([a-zA-Z_$][a-zA-Z0-9_$]*)/g;
    const matches = [...strippedExpr.matchAll(idRegex)];

    // We need access to appState.docs.value, so this function must be called where appState is available.
    // Ideally appState should be defined before this, but due to hoisting/module structure, we access it directly if available, 
    // or we pass it in. For now, we assume appState is imported/available in scope (which it is in this file).

    const docInfo = appState.docs.value;
    const keywords = new Set(['true', 'false', 'null', 'pi', 'e', 'inf', 'nan', 'infinity']);
    const docFuncs = new Set((docInfo.functions || []).map(f => (f.Name || f.name || '').toLowerCase()));

    const uniqueVars = [];
    const seen = new Set();
    matches.forEach(m => {
        const name = m[1];
        const pos = m.index + m[0].indexOf(name);
        if (!seen.has(name)) {
            const lower = name.toLowerCase();
            if (!keywords.has(lower) && !docFuncs.has(lower)) {
                uniqueVars.push({ name, pos });
                seen.add(name);
            }
        }
    });

    if (uniqueVars.length === 0) return [];

    const newVars = [];
    uniqueVars.forEach(({ name, pos }) => {
        const lower = name.toLowerCase();
        const escapedName = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

        function getArgContext(formula, startPos) {
            let depth = 0;
            let commas = 0;
            for (let i = startPos - 1; i >= 0; i--) {
                const c = formula[i];
                if (c === ')') depth++;
                else if (c === '(') {
                    if (depth === 0) {
                        let nStart = i - 1;
                        while (nStart >= 0 && /[a-zA-Z0-9_$]/.test(formula[nStart])) nStart--;
                        return { name: formula.slice(nStart + 1, i).trim().toLowerCase(), index: commas };
                    }
                    depth--;
                } else if (c === ',' && depth === 0) {
                    commas++;
                }
            }
            return null;
        }

        const ctx = getArgContext(expr, pos);
        let inferredType = null;

        if (ctx) {
            const func = (docInfo.functions || []).find(f => (f.Name || f.name || '').toLowerCase() === ctx.name);
            if (func && func.Arguments) {
                let argDef = null;
                let currentIdx = 0;
                for (const arg of func.Arguments) {
                    if (arg.Type === 'array' || arg.type === 'array') {
                        argDef = (arg.Arguments || arg.arguments)?.[0];
                        break;
                    }
                    if (currentIdx === ctx.index) {
                        argDef = arg;
                        break;
                    }
                    currentIdx++;
                }
                if (argDef) inferredType = (argDef.Type || argDef.type || '').toLowerCase();
                if (!inferredType) {
                    const cat = (func.Category || func.category || '').toLowerCase();
                    if (cat === 'datetime') inferredType = 'date';
                    else if (cat === 'string') inferredType = 'string';
                    else if (cat === 'logical') inferredType = 'boolean';
                }
            }
        }

        const isBool = inferredType === 'boolean' ||
            lower.includes('bool') || lower.includes('flag') || lower.includes('success') ||
            new RegExp(`(?:if|iif|any|all)\\s*\\(\\s*${escapedName}\\s*[,)]`, 'i').test(expr) ||
            new RegExp(`${escapedName}\\s*\\?`, 'i').test(expr);

        const isDate = inferredType === 'date' ||
            lower.includes('date') || lower.includes('time') || lower.includes('dt');

        const isString = inferredType === 'string' || lower.startsWith('str_');
        const isList = inferredType === 'array' || lower.includes('list') || lower.includes('arr');

        let val = '10';
        if (isBool) val = 'true';
        // formatDate depends on appState being available, which it is.
        else if (isDate) val = formatDate(appState.settings.value.dateFormat);
        else if (isString) val = 'abc';
        else if (isList) val = '[1, 10, 100]';

        newVars.push({ name, value: val });
    });

    return newVars;
};

// --- STATE ---
const settings = loadSavedSettings();

export const appState = {
    input: signal(''),
    selectedIdx: signal(''),
    result: signal('---'),
    status: signal('Initializing...'),
    isReady: signal(false),
    isLoading: signal(true),
    isCalculating: signal(false),
    variables: signal([]),
    history: signal(loadSavedHistory()),
    version: signal('Loading...'),
    docsOpen: signal(false),
    docs: signal({ functions: [], operators: [] }),
    docsSafe: signal(false),
    calcTime: signal(null),
    message: signal(''),
    resultType: signal(''),
    exampleSearch: signal(''),
    docSearch: signal(sessionStorage.getItem('docSearch') || ''),
    docCategory: signal(sessionStorage.getItem('docCategory') || 'All_Categories'),
    docActiveTab: signal('funcs'),
    operatorSortBy: signal('Precedence'),
    operatorSortDir: signal('desc'),
    librarySortBy: signal('Name'),
    librarySortDir: signal('asc'),
    settings: signal(settings),
    settingsOriginal: signal(JSON.parse(JSON.stringify(settings))),
    showScrollTop: signal(false),
    isOfflineReady: signal(false),
    pwaUpdateAvailable: signal(false),
    pwaInstallPrompt: signal(null),
    suggestions: signal([]),
    snippets: signal(loadSnippets()),
    saveSnippetOpen: signal(false),
    editingSnippet: signal(null),
    knownNames: signal(new Set(['pi', 'e', 'true', 'false'])),
    snippetSearch: signal(''),
    librarySearch: signal(''),
    confirmDialog: {
        open: signal(false),
        title: signal('Confirm Action'),
        message: signal('Are you sure you want to proceed?'),
        variant: signal('primary'),
        onConfirm: signal(null),
        confirmLabel: signal('Confirm'),
        cancelLabel: signal('Cancel'),
        lastActiveElement: signal(null)
    },
    snippetErrors: signal({ name: '', value: '', icon: '' })
};

// --- AUTO-SAVE EFFECTS ---
effect(() => {
    localStorage.setItem(STORAGE_KEY_HISTORY, JSON.stringify(appState.history.value));
});

effect(() => {
    localStorage.setItem(STORAGE_KEY_SNIPPETS, JSON.stringify(appState.snippets.value));
});

effect(() => {
    let theme = appState.settingsOriginal.value.theme || 'auto';

    if (theme === 'auto') {
        const isDark = globalThis.matchMedia('(prefers-color-scheme: dark)').matches;
        theme = isDark ? 'dark' : 'light';
    }

    document.body.dataset.theme = theme;
    document.documentElement.dataset.theme = theme;
    const slTheme = theme === 'dark' ? 'sl-theme-dark' : 'sl-theme-light';
    document.documentElement.className = slTheme;
});

// Listener for system preference changes
globalThis.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (appState.settingsOriginal.value.theme === 'auto') {
        const isDark = globalThis.matchMedia('(prefers-color-scheme: dark)').matches;
        const theme = isDark ? 'dark' : 'light';
        document.body.dataset.theme = theme;
        document.documentElement.dataset.theme = theme;
        document.documentElement.className = isDark ? 'sl-theme-dark' : 'sl-theme-light';
    }
});

// --- INTEROP & LOGIC ---
async function waitForInterop() {
    if (globalThis.shyCalculator) return globalThis.shyCalculator;
    return new Promise((resolve) => {
        const handler = (e) => {
            globalThis.removeEventListener('shy-calculator-ready', handler);
            resolve(e.detail);
        };
        globalThis.addEventListener('shy-calculator-ready', handler);
        const i = setInterval(() => {
            if (globalThis.shyCalculator) {
                clearInterval(i);
                globalThis.removeEventListener('shy-calculator-ready', handler);
                resolve(globalThis.shyCalculator);
            }
        }, 100);
    });
}

async function loadDocumentation() {
    if (appState.docsSafe.value) return;
    try {
        const interop = globalThis.shyCalculator;
        const json = await interop.invokeMethodAsync('GetDocumentation');
        const data = JSON.parse(json);
        appState.docs.value = data;

        // Populate known names for intelligence features
        const names = new Set(['pi', 'e', 'true', 'false']);
        if (data.functions) data.functions.forEach(f => names.add(f.Name || f.name));
        if (data.operators) data.operators.forEach(o => names.add(o.Name || o.name));
        appState.knownNames.value = names;

        appState.docsSafe.value = true;
    } catch (e) {
        console.error("Docs load failed", e);
    }
}

const util = {
    notify: async (message, variant = 'success', icon = 'check2-circle') => {
        const alert = document.createElement('sl-alert');
        alert.variant = variant;
        alert.closable = true;
        alert.duration = 3000;
        alert.innerHTML = `
            ${message}
        `;

        // Prevent layout shift/flicker by removing from flow immediately
        alert.style.position = 'fixed';
        alert.style.top = '0';
        alert.style.opacity = '0';
        alert.style.pointerEvents = 'none';

        document.body.append(alert);

        if (typeof alert.toast !== 'function') {
            await customElements.whenDefined('sl-alert');
            await new Promise(r => setTimeout(r, 50));
        }

        return alert.toast();
    }
};

export const actions = {
    init: async () => {
        try {
            appState.status.value = 'Connecting...';
            const interop = await waitForInterop();
            await interop.invokeMethodAsync('Ping');

            try {
                const ver = await interop.invokeMethodAsync('GetVersion');
                appState.version.value = ver;
            } catch (e) {
                appState.version.value = "Unknown";
            }

            appState.status.value = 'Engine Ready';
            appState.isReady.value = true;
            appState.isLoading.value = false;

            const s = appState.settings.value;
            await interop.invokeMethodAsync('ConfigureDates', s.dateFormat, s.culture);
            appState.settingsOriginal.value = JSON.parse(JSON.stringify(s));

            loadDocumentation();

        } catch (e) {
            console.error("Initialization Failed", e);
            appState.status.value = 'Error: ' + e.message;
            appState.isLoading.value = false;
        }
    },

    openDocs: () => {
        appState.docCategory.value = 'All_Categories';
        appState.docSearch.value = '';
        appState.docsOpen.value = true;
    },

    calculate: async () => {
        if (!appState.isReady.value || appState.isCalculating.value) return;
        const expr = appState.input.value;
        if (!expr.trim()) return;

        appState.isCalculating.value = true;
        await new Promise(r => setTimeout(r, 60));

        const startTime = performance.now();
        try {
            const interop = globalThis.shyCalculator;
            let res;

            if (appState.variables.value.length > 0) {
                let effectiveExpr = expr;
                const vars = {};
                for (const v of appState.variables.value) {
                    let val = v.value;

                    if (val.trim().startsWith('[') && val.trim().endsWith(']')) {
                        const items = val.trim().slice(1, -1);
                        const escapedName = v.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                        const expandRegex = new RegExp(`(^|[^a-zA-Z0-9_$])${escapedName}($|[^a-zA-Z0-9_$])`, 'g');
                        effectiveExpr = effectiveExpr.replace(expandRegex, (match, p1, p2) => p1 + items + p2);
                    }

                    if (!isNaN(parseFloat(val)) && isFinite(val)) val = parseFloat(val);
                    else if (val.toLowerCase() === 'true') val = true;
                    else if (val.toLowerCase() === 'false') val = false;

                    vars[v.name] = val;
                }
                res = await interop.invokeMethodAsync('CalculateWithVars', effectiveExpr, vars, false);
            } else {
                res = await interop.invokeMethodAsync('Calculate', expr, false);
            }

            const endTime = performance.now();
            appState.calcTime.value = (endTime - startTime).toFixed(1);

            if (res.success || res.Success) {
                const getProps = (obj) => ({
                    n: obj.nvalue ?? obj.Nvalue,
                    b: obj.bvalue ?? obj.Bvalue,
                    s: obj.svalue ?? obj.Svalue,
                    d: obj.dvalue ?? obj.Dvalue,
                    e: obj.error ?? obj.Error,
                    m: obj.message ?? obj.Message,
                    t: obj.dataType ?? obj.DataType
                });

                const p = getProps(res);
                let val = null;

                const typeMap = { 1: 'Number', 2: 'Boolean', 4: 'Date', 8: 'String' };
                appState.resultType.value = typeMap[p.t] || '';

                if (p.n !== null && p.n !== undefined) val = p.n;
                else if (p.b !== null && p.b !== undefined) val = p.b;
                else if (p.s !== null && p.s !== undefined) val = p.s;
                else if (p.d !== null && p.d !== undefined) val = p.d;

                appState.result.value = val?.toString() ?? "null";
                appState.message.value = p.m || "Calculation successful";

                if (appState.settings.value.enableHistory) {
                    const historyItem = {
                        expr: expr,
                        result: appState.result.value,
                        resultType: appState.resultType.value,
                        vars: JSON.parse(JSON.stringify(appState.variables.value)),
                        timestamp: Date.now()
                    };

                    const currentHistory = appState.history.value;
                    const varsStr = JSON.stringify(historyItem.vars);
                    const filteredHistory = currentHistory.filter(h =>
                        h.expr !== historyItem.expr || JSON.stringify(h.vars) !== varsStr
                    );

                    const maxLen = parseInt(appState.settings.value.historyLength) || 15;
                    const newHistory = [historyItem, ...filteredHistory].slice(0, maxLen);
                    appState.history.value = newHistory;
                }
            } else {
                appState.result.value = "Error";
                appState.message.value = res.error || res.Error || res.message || res.Message || "Unknown error";
                appState.resultType.value = "";
            }

        } catch (e) {
            appState.result.value = "Interop Error";
            appState.message.value = e.message;
            appState.resultType.value = "";
        } finally {
            appState.isLoading.value = false;
            appState.isCalculating.value = false;
        }
    },

    addVar: () => {
        appState.variables.value = [...appState.variables.value, { name: 'x', value: '10' }];
    },

    updateVar: (index, field, val) => {
        const newVars = [...appState.variables.value];
        newVars[index][field] = val;
        appState.variables.value = newVars;
    },

    removeVar: (index) => {
        appState.variables.value = appState.variables.value.filter((_, i) => i !== index);
    },
    clearVars: () => {
        appState.variables.value = [];
        appState.result.value = '---';
        appState.message.value = '';
        appState.resultType.value = '';
        appState.calcTime.value = null;
    },
    copyToClipboard: (text) => {
        navigator.clipboard.writeText(text);
        util.notify("Copied to clipboard!", "success", "copy");
    },
    clearHistory: () => {
        appState.history.value = [];
    },
    loadHistoryItem: (item) => {
        appState.input.value = item.expr;
        appState.variables.value = JSON.parse(JSON.stringify(item.vars || []));
        appState.result.value = item.result;
        appState.resultType.value = item.resultType || '';
        appState.calcTime.value = null;
        actions.calculate();
    },
    saveSettings: async (newSettings) => {
        if (!appState.isReady.value) return;
        try {
            const interop = globalThis.shyCalculator;
            await interop.invokeMethodAsync('ConfigureDates', newSettings.dateFormat, newSettings.culture);

            appState.settings.value = { ...newSettings };
            appState.settingsOriginal.value = JSON.parse(JSON.stringify(newSettings));

            const maxLen = parseInt(newSettings.historyLength) || 15;
            if (appState.history.value.length > maxLen) {
                appState.history.value = appState.history.value.slice(0, maxLen);
            }

            localStorage.setItem(STORAGE_KEY_SETTINGS, JSON.stringify(newSettings));
            appState.message.value = "Settings saved & engine reconfigured";
            util.notify("Settings saved successfully!");
        } catch (e) {
            console.error("Settings failed", e);
            appState.message.value = "Error: " + e.message;
        }
    },
    insertExample: (expr, predefinedVars, sourceIdx = '') => {
        appState.input.value = expr;
        appState.docsOpen.value = false;
        appState.variables.value = [];
        appState.selectedIdx.value = sourceIdx;

        if (predefinedVars && predefinedVars.length > 0) {
            appState.variables.value = predefinedVars.map(v => ({ ...v }));
        } else {
            const newVars = inferVariables(expr);
            if (newVars.length > 0) {
                appState.variables.value = newVars;
                appState.message.value = "Expression loaded. Smart defaults applied based on function context.";
            }
        }

        appState.result.value = '---';
        actions.calculate();
    },

    saveSnippet: (name, icon, value, group) => {
        const editing = appState.editingSnippet.value;
        if (editing && editing.id) {
            appState.snippets.value = appState.snippets.value.map(s =>
                s.id === editing.id ? { ...s, label: name, icon, value, group, vars: JSON.parse(JSON.stringify(appState.variables.value)) } : s
            );
            util.notify("Expression updated!", "success", "check2-circle");
        } else {
            let vars = JSON.parse(JSON.stringify(appState.variables.value));

            // If the user modified the expression in the dialog, or if variables are missing, try to infer them
            if (value && (value !== appState.input.value || vars.length === 0)) {
                const inferred = inferVariables(value);
                if (inferred.length > 0) {
                    vars = inferred;
                }
            }

            const item = {
                id: Date.now().toString(),
                label: name,
                value: value || appState.input.value,
                vars: vars,
                icon: icon || 'bookmark',
                group: group || 'Custom Expressions'
            };
            appState.snippets.value = [...appState.snippets.value, item];
            util.notify("Expression saved to library!", "success", "bookmark");

            // Auto-select the new snippet
            actions.loadSnippet(item);
            appState.selectedIdx.value = item.id;
        }
        appState.editingSnippet.value = null;
    },

    deleteSnippet: (id) => {
        appState.snippets.value = appState.snippets.value.filter(s => s.id !== id);
        util.notify("Expression removed from library.");
    },
    editSnippet: (snippet) => {
        actions.openSaveSnippet(snippet);
    },
    openSaveSnippet: (snippet = null) => {
        appState.snippetErrors.value = { name: '', value: '', icon: '' };
        if (snippet) {
            appState.editingSnippet.value = JSON.parse(JSON.stringify(snippet));
        } else {
            // New snippet: use current input but separate state so we can track Name/Icon
            appState.editingSnippet.value = {
                label: '',
                value: appState.input.value,
                icon: ''
            };
        }
        appState.saveSnippetOpen.value = true;
    },
    togglePinSnippet: (id) => {
        appState.snippets.value = appState.snippets.value.map(s =>
            s.id === id ? { ...s, pinned: !s.pinned } : s
        );
        const snippet = appState.snippets.value.find(s => s.id === id);
        util.notify(snippet.pinned ? "Expression pinned!" : "Expression unpinned.");
    },

    loadSnippet: (snippet) => {
        appState.input.value = snippet.value;
        appState.variables.value = JSON.parse(JSON.stringify(snippet.vars || []));
        appState.selectedIdx.value = '';
        appState.docsOpen.value = false;
        appState.result.value = '---';
        actions.calculate();
    },

    validateExpression: async (expr, includeAst = false) => {
        try {
            const interop = globalThis.shyCalculator;
            const json = await interop.invokeMethodAsync('ValidateExpression', expr, includeAst);
            return JSON.parse(json);
        } catch (e) {
            console.error("Validation failed", e);
            return { Success: false, Message: "Validation internal error: " + e.message, Errors: [] };
        }
    },

    exportHistory: (format) => {
        const history = appState.history.value;
        if (history.length === 0) return;

        let content = '';
        let fileName = `shy_history_${new Date().getTime()}`;
        let mimeType = 'text/plain';

        if (format === 'json') {
            content = JSON.stringify(history, null, 2);
            fileName += '.json';
            mimeType = 'application/json';
        } else {
            // CSV
            const headers = ['Expression', 'Result', 'Type', 'Variables'];
            const rows = history.map(h => [
                `"${h.expr.replaceAll('"', '""')}"`,
                `"${String(h.result).replaceAll('"', '""')}"`,
                `"${h.resultType || ''}"`,
                `"${(h.vars || []).map(v => `${v.name}=${v.value}`).join('; ').replace(/"/g, '""')}"`
            ]);
            content = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
            fileName += '.csv';
            mimeType = 'text/csv';
        }

        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    exportSnippets: () => {
        const snippets = appState.snippets.value;
        const content = JSON.stringify(snippets, null, 2);
        const fileName = `shy_snippets_${new Date().getTime()}.json`;
        const blob = new Blob([content], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        util.notify("Expression library exported successfully!", "success", "download");
    },

    importSnippets: async (file) => {
        if (!file) return;
        try {
            const text = await file.text();
            const imported = JSON.parse(text);
            if (!Array.isArray(imported)) throw new Error("Invalid format");

            // Basic validation
            const valid = imported.filter(s => s && s.label && s.value);
            if (valid.length === 0) throw new Error("No valid expressions found");

            // Merge with existing, avoiding duplicates by ID if possible, otherwise by content
            const current = appState.snippets.value;
            // Create lookup for existing snippets by Name (normalized)
            const existingByName = {};
            current.forEach(s => existingByName[(s.label || '').toLowerCase()] = s);

            const toAdd = [];
            const idsToRemove = new Set(); // For overwrites

            valid.forEach(s => {
                const importLabel = (s.label || '').trim();
                const importKey = importLabel.toLowerCase();
                const existing = existingByName[importKey];

                if (existing) {
                    // Name conflict
                    if (existing.value === s.value) {
                        // Exact match (Name + Formula) -> Overwrite
                        // We keep the EXISTING ID to preserve references, but update metadata
                        // To do this, we remove the old one (by ID) and push the new one (with OLD ID)
                        idsToRemove.add(existing.id);
                        toAdd.push({ ...s, id: existing.id });
                        addedCount++;
                    } else {
                        // Name match but different Formula -> Append/Rename
                        // Create new snippet with modified name
                        const newLabel = `${importLabel} (Imported)`;
                        const newId = `imported_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
                        toAdd.push({ ...s, id: newId, label: newLabel });
                        addedCount++;
                    }
                } else {
                    // No name conflict -> Add as new
                    // Check ID conflict just in case
                    let newId = s.id;
                    if (!newId || existingIds.has(newId)) {
                        newId = `imported_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
                    }
                    toAdd.push({ ...s, id: newId });
                    addedCount++;
                }
            });

            // Filter out overwritten snippets from current state
            const remainingCurrent = current.filter(s => !idsToRemove.has(s.id));

            appState.snippets.value = [...remainingCurrent, ...toAdd];
            util.notify(`Imported ${addedCount} expressions!`, "success", "upload");
        } catch (e) {
            console.error(e);
            util.notify("Failed to import expressions: " + e.message, "danger", "alert-triangle");
        }
    },

    toggleTheme: () => {
        const current = appState.settings.value.theme || 'dark';
        const next = current === 'dark' ? 'light' : 'dark';
        appState.settings.value = { ...appState.settings.value, theme: next };
        actions.saveSettings(appState.settings.value);
    },

    cancelSettings: () => {
        appState.settings.value = JSON.parse(JSON.stringify(appState.settingsOriginal.value));
        util.notify("Changes reverted", "neutral", "arrow-counterclockwise");
    },
    openConfirm: (title, message, callback, options = {}) => {
        appState.confirmDialog.lastActiveElement.value = document.activeElement;
        appState.confirmDialog.title.value = title || 'Confirm';
        appState.confirmDialog.message.value = message || 'Are you sure?';
        appState.confirmDialog.onConfirm.value = callback;
        appState.confirmDialog.variant.value = options.variant || 'primary';
        appState.confirmDialog.confirmLabel.value = options.confirmLabel || 'Confirm';
        appState.confirmDialog.cancelLabel.value = options.cancelLabel || 'Cancel';
        appState.confirmDialog.open.value = true;
    },
    closeConfirm: () => {
        appState.confirmDialog.open.value = false;
        const last = appState.confirmDialog.lastActiveElement.value;
        if (last && typeof last.focus === 'function') {
            last.focus();
        }
    },

    installPwa: async () => {
        const promptEvent = appState.pwaInstallPrompt.value;
        if (promptEvent) {
            promptEvent.prompt();
            const result = await promptEvent.userChoice;
            if (result.outcome === 'accepted') {
                appState.pwaInstallPrompt.value = null;
                util.notify("App installed successfully!", "success", "download");
            }
        }
    },

    refreshPwa: () => {
        globalThis.location.reload();
    },

    uninstallApp: async () => {
        actions.openConfirm(
            "Clear App Data",
            "Are you sure you want to unregister the Service Worker and clear all cached app data? You will still need to manually remove the app icon from your device using your browser's uninstall option.",
            async () => {
                if ('serviceWorker' in navigator) {
                    const regs = await navigator.serviceWorker.getRegistrations();
                    for (const reg of regs) {
                        await reg.unregister();
                    }
                }
                if ('caches' in window) {
                    const keys = await caches.keys();
                    for (const key of keys) {
                        await caches.delete(key);
                    }
                }
                util.notify("App data cleared. Please use your browser menu to fully uninstall.", "success", "trash");
                setTimeout(() => window.location.reload(), 2000);
            },
            { variant: 'danger', confirmLabel: 'Clear Data' }
        );
    }
};
