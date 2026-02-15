import { h, render } from 'https://esm.sh/preact@10.19.3';
import { signal, effect } from 'https://esm.sh/@preact/signals@1.2.2?deps=preact@10.19.3';
import htm from 'https://esm.sh/htm@3.1.1';
import { App, FLAT_MAP } from './template.js';

const html = htm.bind(h);

// --- PERSISTENCE UTILS ---
const STORAGE_KEY_HISTORY = 'shy_calc_history';
const STORAGE_KEY_CONFIG = 'shy_calc_config';

const loadSavedHistory = () => {
    try {
        const saved = localStorage.getItem(STORAGE_KEY_HISTORY);
        return saved ? JSON.parse(saved) : [];
    } catch { return []; }
};


const STORAGE_KEY_SETTINGS = 'shy_calc_settings';
const loadSavedSettings = () => {
    const defaults = { dateFormat: 'dd/MM/yyyy', culture: 'en-US', enableHistory: true, historyLength: 15 };
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

    // Quick & Dirty C# to JS pattern mapper for common ones
    return pattern
        .replace('yyyy', y)
        .replace('MM', m)
        .replace('dd', d)
        .replace('yyyy', y) // repeat for safety
        .replace('M', now.getMonth() + 1)
        .replace('d', now.getDate());
};

const settings = loadSavedSettings();

// --- STATE ---
const appState = {
    input: signal(''),
    selectedIdx: signal(''),
    result: signal('---'),
    status: signal('Initializing...'),
    isReady: signal(false),
    isLoading: signal(true),
    isCalculating: signal(false),
    variables: signal([]), // Array of { name, value }
    history: signal(loadSavedHistory()),   // Array of { expr, result }
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
    operatorSortBy: signal('Precedence'),
    operatorSortDir: signal('desc'),
    settings: signal(settings),
    showScrollTop: signal(false)
};

// --- AUTO-SAVE EFFECTS ---
effect(() => {
    localStorage.setItem(STORAGE_KEY_HISTORY, JSON.stringify(appState.history.value));
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
            <sl-icon name="${icon}" slot="icon"></sl-icon>
            ${message}
        `;

        document.body.append(alert);

        // Defensive check: if .toast() is missing, wait for definition and a tiny hydration window
        if (typeof alert.toast !== 'function') {
            await customElements.whenDefined('sl-alert');
            // Tiny delay to ensure the component is fully hydrated internally
            await new Promise(r => setTimeout(r, 50));
        }

        return alert.toast();
    }
};

const actions = {
    init: async () => {
        try {
            appState.status.value = 'Connecting...';
            const interop = await waitForInterop();

            await interop.invokeMethodAsync('Ping');

            try {
                const ver = await interop.invokeMethodAsync('GetVersion');
                appState.version.value = ver;
            } catch (e) {
                console.warn("Version check failed", e);
                appState.version.value = "Unknown";
            }

            appState.status.value = 'Engine Ready';
            appState.isReady.value = true;
            appState.isLoading.value = false;

            // Apply saved settings to engine
            const s = appState.settings.value;
            await interop.invokeMethodAsync('ConfigureDates', s.dateFormat, s.culture);

            loadDocumentation();

            // Auto-load first example on startup
            if (FLAT_MAP && FLAT_MAP.length > 0) {
                const first = FLAT_MAP[0];
                actions.insertExample(first.value, first.vars, "0");
            }

        } catch (e) {
            console.error("Initialization Failed", e);
            appState.status.value = 'Error: ' + e.message;
            appState.isLoading.value = false;
        }
    },

    openDocs: () => {
        appState.docsOpen.value = true;
    },

    calculate: async () => {
        if (!appState.isReady.value || appState.isCalculating.value) return;
        const expr = appState.input.value;
        if (!expr.trim()) return;

        appState.isCalculating.value = true;
        // Artificial delay for visual feedback
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

                    // --- Expression Expansion for Arrays ---
                    // If value looks like an array [1, 2, 3], we expand it in the string
                    // This allows aggregate functions like var($list) to work since the engine sees
                    // them as separate arguments.
                    if (val.trim().startsWith('[') && val.trim().endsWith(']')) {
                        const items = val.trim().slice(1, -1); // Remove [ and ]
                        const escapedName = v.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
                        // Use a regex that ensures the variable isn't part of a larger identifier
                        const expandRegex = new RegExp(`(^|[^a-zA-Z0-9_$])${escapedName}($|[^a-zA-Z0-9_$])`, 'g');
                        effectiveExpr = effectiveExpr.replace(expandRegex, (match, p1, p2) => p1 + items + p2);
                    }

                    if (!isNaN(parseFloat(val)) && isFinite(val)) val = parseFloat(val);
                    else if (val.toLowerCase() === 'true') val = true;
                    else if (val.toLowerCase() === 'false') val = false;

                    vars[v.name] = val;
                }
                res = await interop.invokeMethodAsync('CalculateWithVars', effectiveExpr, vars);
            } else {
                res = await interop.invokeMethodAsync('Calculate', expr);
            }

            const endTime = performance.now();
            appState.calcTime.value = (endTime - startTime).toFixed(1);

            if (res.success || res.Success) {
                // Extract value checking for camelCase and PascalCase
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

                    // Deduplication Logic: Move to Top
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
        appState.calcTime.value = null; // Hide badge on load as we don't persist timing in history yet
        // Don't auto-calculate, just load the state
    },
    saveSettings: async (newSettings) => {
        if (!appState.isReady.value) return;
        try {
            const interop = globalThis.shyCalculator;
            await interop.invokeMethodAsync('ConfigureDates', newSettings.dateFormat, newSettings.culture);

            appState.settings.value = { ...newSettings };

            // Trim current history if length was reduced
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
        appState.variables.value = []; // Absolute Reset
        appState.selectedIdx.value = sourceIdx;

        if (predefinedVars && predefinedVars.length > 0) {
            appState.variables.value = predefinedVars.map(v => ({ ...v }));
        } else {
            // Smart Variable Extraction (Ignore literals)
            // Strip everything inside single or double quotes to prevent matching 'Yes' as a variable
            const strippedExpr = expr.replace(/'[^']*'|"[^"]*"/g, m => ' '.repeat(m.length));

            const idRegex = /(?:^|[^a-zA-Z0-9_$])([a-zA-Z_$][a-zA-Z0-9_$]*)/g;
            const matches = [...strippedExpr.matchAll(idRegex)];
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

            if (uniqueVars.length > 0) {
                const newVars = [];
                uniqueVars.forEach(({ name, pos }) => {
                    const lower = name.toLowerCase();
                    const escapedName = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

                    // --- Signature-Based Inference ---
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
                            // Fallback to Category if positional arg not found
                            if (!inferredType) {
                                const cat = (func.Category || func.category || '').toLowerCase();
                                if (cat === 'datetime') inferredType = 'date';
                                else if (cat === 'string') inferredType = 'string';
                                else if (cat === 'logical') inferredType = 'boolean';
                            }
                        }
                    }

                    // --- Fallback Heuristics ---
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
                    else if (isDate) val = formatDate(appState.settings.value.dateFormat);
                    else if (isString) val = 'abc';
                    else if (isList) val = '[1, 10, 100]';

                    newVars.push({ name, value: val });
                });
                appState.variables.value = newVars;
                appState.message.value = "Formula loaded. Smart defaults applied based on function context.";
            }
        }

        appState.result.value = '---';
        actions.calculate();
    }
};

// --- RENDER ---
render(html`<${App} state=${appState} actions=${actions} />`, document.getElementById('app'));
