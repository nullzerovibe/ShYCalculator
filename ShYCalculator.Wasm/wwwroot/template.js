import { h } from 'https://esm.sh/preact@10.19.3';
import { useEffect } from 'https://esm.sh/preact@10.19.3/hooks';
import htm from 'https://esm.sh/htm@3.1.1';

// Initialize htm with Preact
const html = htm.bind(h);

const getCategoryIconUrl = (cat) => {
    const c = (cat || '').trim().toLowerCase();
    let icon = 'help-circle';
    switch (c) {
        case 'arithmetic':
        case 'arithmetical': icon = 'calculator'; break;
        case 'unary': icon = 'plus'; break;
        case 'comparison': icon = 'equal'; break;
        case 'grouping': icon = 'parentheses'; break;
        case 'logical': icon = 'cpu'; break;
        case 'bitwise': icon = 'binary'; break;
        case 'scientific': icon = 'sigma'; break;
        case 'numeric': icon = 'hash'; break;
        case 'string': icon = 'type'; break;
        case 'date':
        case 'date & time': icon = 'calendar'; break;
        case 'array': icon = 'layers'; break;
        case 'all_categories': icon = 'layout-grid'; break;
    }
    return `https://api.iconify.design/lucide/${icon}.svg?color=%23cbd5e1`;
};

const getTypeIconUrl = (type) => {
    const t = (type || '').trim().toLowerCase();
    let icon = 'help-circle';
    switch (t) {
        case 'number': icon = 'hash'; break;
        case 'string': icon = 'type'; break;
        case 'date': icon = 'calendar'; break;
        case 'boolean': icon = 'toggle-left'; break;
        case 'array': icon = 'layers'; break;
    }
    return `https://api.iconify.design/lucide/${icon}.svg?color=%23cbd5e1`;
};

export const Header = ({ state }) => html`
    <header class="app-header">
        <div class="logo-area">
            <h1 class="app-title">ShYCalculator</h1>
            <p class="app-subtitle">
                High-performance .NET WASM Expression Evaluator 
                <span class="version-tag">v${state.version.value}</span>
            </p>
        </div>
        <div class="status-indicator">
            <sl-badge variant="${state.isReady.value ? 'success' : 'danger'}">
                ${state.status.value}
            </sl-badge>
        </div>
    </header>
`;

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
        label: "Formulas (Uses Variables)",
        icon: "function-square",
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

// Flatten for lookup, adding a unique global index
let globalIndex = 0;
export const FLAT_MAP = [];
EXAMPLE_GROUPS.forEach(g => {
    g.items.forEach(item => {
        item._idx = globalIndex++;
        FLAT_MAP.push(item);
    });
});

export const MainCard = ({ state, actions }) => {
    const onInput = (e) => state.input.value = e.target.value;

    const onExampleSelect = (e) => {
        const idx = e.target.value;
        if (idx !== '' && FLAT_MAP[Number.parseInt(idx)]) {
            const item = FLAT_MAP[Number.parseInt(idx)];
            actions.insertExample(item.value, item.vars, idx);
        }
    };

    const vars = state.variables.value;

    return html`
        <sl-card class="main-card">
            <div class="form-section">
                <label class="section-label">
                    <sl-icon src="https://api.iconify.design/lucide/list-plus.svg?color=%23cbd5e1" class="section-icon"></sl-icon>
                    Load Example
                </label>
                <div class="controls-top">
                    <sl-select placeholder="Select an example" value=${state.selectedIdx.value} onsl-change=${onExampleSelect} hoist>
                        ${EXAMPLE_GROUPS.map((g, i) => html`
                            <sl-menu-label>
                                <sl-icon src="https://api.iconify.design/lucide/${g.icon || 'help-circle'}.svg?color=%23cbd5e1" class="ex-group-icon"></sl-icon>
                                ${g.label}
                            </sl-menu-label>
                            ${g.items.map(ex => html`
                                <sl-option value="${ex._idx}">${ex.label}</sl-option>
                            `)}
                            ${i < EXAMPLE_GROUPS.length - 1 ? html`<sl-divider></sl-divider>` : null}
                        `)}
                    </sl-select>
                    <sl-button outline class="btn-secondary" onclick=${actions.openDocs}>
                        <sl-icon slot="prefix" name="book"></sl-icon> Reference Guide
                    </sl-button>
                </div>
            </div>

            <div class="form-section">
                <label class="section-label">
                    <sl-icon src="https://api.iconify.design/lucide/terminal.svg?color=%23cbd5e1" class="section-icon"></sl-icon>
                    Mathematical Expression
                </label>
                <sl-input 
                    placeholder="Enter expression..." 
                    value=${state.input.value}
                    oninput=${onInput}
                    onkeydown=${e => e.key === 'Enter' && actions.calculate()}
                    clearable
                ></sl-input>
            </div>

            <div class="form-section">
                <div class="section-header">
                    <label class="section-label">
                        <sl-icon src="https://api.iconify.design/lucide/variable.svg?color=%23cbd5e1" class="section-icon"></sl-icon>
                        Variables (Context)
                    </label>
                    <div class="section-actions">
                        <sl-button size="small" variant="neutral" outline class="btn-clear-all btn-secondary" onclick=${actions.clearVars} style="margin-right: 0.5rem;">
                            <sl-icon slot="prefix" name="trash"></sl-icon> Clear
                        </sl-button>
                        <sl-button size="small" variant="neutral" outline class="btn-secondary" onclick=${actions.addVar}>
                            <sl-icon slot="prefix" name="plus"></sl-icon> Add
                        </sl-button>
                    </div>
                </div>
                
                <div class="vars-list">
                    ${vars.length === 0 ? html`<div class="empty-state-small">No variables defined</div>` : null}
                    ${vars.map((v, i) => html`
                        <div class="var-row">
                            <sl-input placeholder="Name" size="small" value=${v.name} oninput=${e => actions.updateVar(i, 'name', e.target.value)}></sl-input>
                            <span class="eq">=</span>
                            <sl-input placeholder="Value" size="small" value=${v.value} oninput=${e => actions.updateVar(i, 'value', e.target.value)}></sl-input>
                            <sl-icon-button name="trash" label="Remove" class="danger-icon" onclick=${() => actions.removeVar(i)}></sl-icon-button>
                        </div>
                    `)}
                </div>
            </div>

            <div class="form-actions">
                <sl-button variant="primary" class="btn-calculate" disabled=${!state.isReady.value || state.isCalculating.value} onclick=${actions.calculate}>
                    Calculate
                </sl-button>

                <div class="result-box ${state.result.value === 'Error' || state.result.value.startsWith('Interop') ? 'error' : ''}" style="margin-top: 1.5rem; position: relative; min-height: 80px; display: flex; flex-direction: column; justify-content: center;">
                    <div class="result-body" style="display: flex; align-items: center; justify-content: space-between; gap: 1rem;">
                        <div class="result-value" style="flex: 1; display: flex; align-items: center;">${state.result.value}</div>
                        <div class="result-actions" style="display: ${state.result.value === '---' || state.result.value === 'Error' || state.result.value === 'null' ? 'none' : 'flex'}; align-items: center;">
                            <sl-icon-button name="copy" label="Copy Result" onclick=${() => actions.copyToClipboard(state.result.value)} class="copy-btn" style="font-size: 1.25rem;"></sl-icon-button>
                        </div>
                    </div>

                    <div class="result-footer" style="display: flex; justify-content: space-between; align-items: flex-end; margin-top: 1rem;">
                        <div class="result-badge-area" style="display: flex; align-items: center; gap: 0.5rem; visibility: ${state.message.value ? 'visible' : 'hidden'};">
                            <sl-badge size="small" class="shy-badge">
                                <sl-icon src="${getTypeIconUrl(state.resultType.value)}" class="type-icon-sm"></sl-icon>
                                ${state.resultType.value}
                            </sl-badge>
                            <span class="result-msg" style="color: var(--text-muted); font-size: 0.9rem; font-weight: 500;">${state.message.value}</span>
                        </div>
                        <div class="result-stats">
                            ${state.calcTime.value === null ? null : html`
                                <sl-badge size="small" class="shy-badge">
                                    <sl-icon src="https://api.iconify.design/lucide/timer.svg?color=%23cbd5e1" class="type-icon-sm"></sl-icon>
                                    ${state.calcTime.value}ms
                                </sl-badge>
                            `}
                        </div>
                    </div>
                </div>
            </div>

            ${state.settings.value.enableHistory && state.history.value.length > 0 ? html`
                <div class="history-section" style="border-top: 1px solid var(--glass-border); padding-top: 1rem;">
                    <div class="history-header" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
                        <label class="section-label">History</label>
                        <sl-button size="small" variant="neutral" outline class="btn-clear-all btn-secondary" onclick=${actions.clearHistory}>
                            <sl-icon slot="prefix" name="trash"></sl-icon> Clear
                        </sl-button>
                    </div>
                    <div class="history-list" style="display: flex; flex-direction: column; gap: 0.5rem; max-height: 300px; overflow-y: auto;">
                        ${state.history.value.map(item => html`
                            <div class="history-item" onclick=${() => actions.loadHistoryItem(item)} style="background: rgba(0, 0, 0, 0.3); border: 1px solid var(--glass-border); border-radius: 8px; padding: 0.75rem 1rem; cursor: pointer; transition: all 0.2s;">
                                <div class="history-item-main" style="display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; margin-bottom: 4px;">
                                    <span class="hist-expr" style="font-family: 'Fira Code', 'Courier New', monospace; font-size: 0.95rem; color: #cbd5e1; word-break: break-all;">${item.expr}</span>
                                    <span class="hist-res-group" style="display: flex; gap: 0.5rem; white-space: nowrap;">
                                        <span class="hist-eq" style="color: var(--accent); font-weight: bold;">=</span>
                                        <span class="hist-res" style="color: var(--success); font-weight: bold;">${item.result}</span>
                                    </span>
                                </div>
                                <div class="history-item-meta" style="display: flex; justify-content: space-between; align-items: center; gap: 1rem;">
                                    <span class="hist-vars" style="font-size: 0.8rem; color: var(--text-muted); font-family: 'Fira Code', 'Courier New', monospace; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                                        ${item.vars && item.vars.length > 0 ? item.vars.map(v => `${v.name}=${v.value}`).join(', ') : ''}
                                    </span>
                                    ${item.resultType ? html`
                                        <sl-badge size="small" class="shy-badge" style="display: inline-flex; align-items: center; gap: 4px;">
                                            <sl-icon src="${getTypeIconUrl(item.resultType)}" class="type-icon-sm"></sl-icon>
                                            ${item.resultType}
                                        </sl-badge>
                                    ` : null}
                                </div>
                            </div>
                        `)}
                    </div>
                </div>
            ` : null}
        </sl-card>
    `;
};



export const Documentation = ({ state, actions }) => {
    const ALL_CATS = 'All_Categories';
    const docs = state.docs.value || { functions: [], operators: [] };
    const functions = docs.functions || docs.Functions || [];
    const operators = docs.operators || docs.Operators || [];
    const query = (state.docSearch.value || '').trim().toLowerCase();
    const category = (state.docCategory.value || '').trim() || ALL_CATS;

    useEffect(() => {
        if (state.docsOpen.value) {
            if (!state.docCategory.value || state.docCategory.value.trim() === '') {
                state.docCategory.value = ALL_CATS;
            }
            if (!state.docSearch.value) {
                state.docSearch.value = '';
            }
        }
    }, [state.docsOpen.value]);



    const categories = [ALL_CATS, ...new Set(functions.map(f => {
        const c = f.Category || f.category;
        return c ? c.trim().replaceAll(' ', '_') : null;
    }).filter(Boolean))];

    const filteredFunctions = (functions || []).filter(f => {
        const name = (f.Name || f.name || '').trim().toLowerCase();
        const desc = (f.Description || f.description || '').trim().toLowerCase();
        const rawFCat = (f.Category || f.category || '').trim();
        const fCategory = rawFCat.replaceAll(' ', '_');

        // Wildcard check for categories - MUST ALWAYS SHOW ALL if ALL_CATS is selected
        const isWildcard = !category || category === ALL_CATS || category === '';
        const matchesCategory = isWildcard || (fCategory === category);

        // Search check
        const matchesQuery = !query || name.includes(query) || desc.includes(query);

        return matchesQuery && matchesCategory;
    });

    const onSearch = (e) => {
        const val = e.target.value;
        state.docSearch.value = val;
        sessionStorage.setItem('docSearch', val);
    };
    const onCategory = (e) => {
        const val = e.target.value || ALL_CATS;
        state.docCategory.value = val;
        sessionStorage.setItem('docCategory', val);
    };

    const onSort = (field) => {
        if (state.operatorSortBy.value === field) {
            state.operatorSortDir.value = state.operatorSortDir.value === 'asc' ? 'desc' : 'asc';
        } else {
            state.operatorSortBy.value = field;
            state.operatorSortDir.value = field === 'Precedence' ? 'desc' : 'asc';
        }
    };

    const sortedOperators = [...operators].sort((a, b) => {
        const field = state.operatorSortBy.value;
        const dir = state.operatorSortDir.value;

        const getVal = (obj, f) => {
            if (obj[f] !== undefined) return obj[f];
            const lowerF = f.charAt(0).toLowerCase() + f.slice(1);
            if (obj[lowerF] !== undefined) return obj[lowerF];
            const upperF = f.charAt(0).toUpperCase() + f.slice(1);
            if (obj[upperF] !== undefined) return obj[upperF];
            return 0;
        };

        let v1 = getVal(a, field);
        let v2 = getVal(b, field);

        if (typeof v1 === 'string') {
            v1 = v1.toLowerCase();
            v2 = v2.toLowerCase();
        }

        if (v1 < v2) return dir === 'asc' ? -1 : 1;
        if (v1 > v2) return dir === 'asc' ? 1 : -1;
        return 0;
    });

    const renderSortHeader = (label, field) => {
        const isActive = state.operatorSortBy.value === field;
        const dir = state.operatorSortDir.value;
        return html`
            <th onclick=${() => onSort(field)} class="sortable-header ${isActive ? 'active' : ''}">
                <div class="th-content">
                    ${label}
                    <sl-icon name="${isActive ? (dir === 'asc' ? 'sort-up' : 'sort-down') : 'arrow-down-up'}" 
                             style="font-size: 0.7rem; color: ${isActive ? 'var(--accent)' : 'inherit'}; opacity: ${isActive ? 1 : 0.4};">
                    </sl-icon>
                </div>
            </th>
        `;
    };

    const onSaveSettings = (e) => {
        e.preventDefault();
        const form = e.target;

        // Robust data collection for Shoelace components
        const getVal = (name) => form.querySelector(`[name="${name}"]`)?.value || '';
        const isChecked = (name) => form.querySelector(`[name="${name}"]`)?.checked || false;

        actions.saveSettings({
            dateFormat: getVal('dateFormat'),
            culture: getVal('culture'),
            enableHistory: isChecked('enableHistory'),
            historyLength: getVal('historyLength')
        });
    };

    const onInputChange = (e) => {
        const { name, value } = e.target;
        state.settings.value = { ...state.settings.value, [name]: value };
    };

    const setInputValue = (name, val) => {
        const input = document.querySelector(`sl-input[name="${name}"]`);
        if (input) {
            input.value = val;
            // Dispatch both for raw DOM listeners and Preact's oninput/onsl-input
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('sl-input', { bubbles: true }));
        }
    };

    const scrollToTop = () => {
        const activePanel = document.querySelector('sl-tab-panel[active]');
        if (activePanel) {
            activePanel.scrollTo({ top: 0, behavior: 'smooth' });
        }
    };

    const onPanelScroll = (e) => {
        state.showScrollTop.value = e.target.scrollTop > 200;
    };

    const onTabShow = () => {
        state.showScrollTop.value = false;
    };

    return html`
        <sl-dialog class="docs-dialog" open=${state.docsOpen.value} onsl-after-hide=${(e) => { if (e.target.localName === 'sl-dialog') { state.docsOpen.value = false; state.showScrollTop.value = false; } }} style="--width: 800px;">
            <div slot="label" style="display: flex; align-items: center; gap: 0.75rem;">
                <sl-icon src="https://api.iconify.design/lucide/book-open.svg?color=%23cbd5e1" style="font-size: 1.6rem;"></sl-icon>
                Reference Guide
            </div>
            <sl-tab-group onsl-tab-show=${onTabShow}>
                <sl-tab slot="nav" panel="funcs">
                    <sl-icon src="https://api.iconify.design/lucide/variable.svg?color=%23cbd5e1" class="tab-icon"></sl-icon> Functions
                </sl-tab>
                <sl-tab slot="nav" panel="ops">
                    <sl-icon src="https://api.iconify.design/lucide/percent.svg?color=%23cbd5e1" class="tab-icon"></sl-icon> Operators
                </sl-tab>
                <sl-tab slot="nav" panel="settings">
                    <sl-icon src="https://api.iconify.design/lucide/settings-2.svg?color=%23cbd5e1" class="tab-icon"></sl-icon> Settings
                </sl-tab>
                <sl-tab slot="nav" panel="about">
                    <sl-icon src="https://api.iconify.design/lucide/info.svg?color=%23cbd5e1" class="tab-icon"></sl-icon> About
                </sl-tab>
                
                <sl-tab-panel name="funcs" onscroll=${onPanelScroll}>
                    <div class="docs-header">
                        <sl-input placeholder="Search functions..." clearable 
                            autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck="false"
                            oninput=${onSearch} 
                            onsl-clear=${() => { state.docSearch.value = ''; sessionStorage.setItem('docSearch', ''); }} 
                            value=${state.docSearch.value} style="flex: 2;">
                            <sl-icon name="search" slot="prefix"></sl-icon>
                        </sl-input>
                        <sl-select value=${category} onsl-change=${onCategory} clearable=${category !== ALL_CATS} style="flex: 1;">
                            <sl-icon src="${getCategoryIconUrl(category)}" slot="prefix" class="cat-icon-sm" style="margin-left: 0.5rem;"></sl-icon>
                            ${categories.map(cat => html`
                                <sl-option value=${cat}>
                                    <div class="cat-icon-wrapper">
                                        <sl-icon src="${getCategoryIconUrl(cat)}" class="cat-icon-sm"></sl-icon>
                                    </div>
                                    ${cat.replaceAll('_', ' ')}
                                </sl-option>
                            `)}
                        </sl-select>
                    </div>
                    <div class="docs-list">
                        ${filteredFunctions.map(fn => {
        const name = fn.Name || fn.name || 'Unknown';
        const args = fn.Arguments || fn.arguments || [];
        const category = fn.Category || fn.category || 'General';
        const description = fn.Description || fn.description || '';
        const examples = fn.Examples || fn.examples || [];

        return html`
                            <div class="doc-card">
                                <div class="doc-card-header">
                                    <span class="doc-name">${name}</span>
                                    <span class="doc-args">(${args.join(', ') || ''})</span>
                                    <sl-badge variant="primary" size="small" outline class="doc-category">
                                        <sl-icon src="${getCategoryIconUrl(category)}" class="cat-icon"></sl-icon> ${category}
                                    </sl-badge>
                                </div>
                                <div class="doc-desc">${description}</div>
                                ${examples.length > 0 ? html`
                                    <div class="doc-examples">
                                        ${examples.map(ex => html`
                                            <sl-button size="small" outline class="btn-example" onclick=${() => actions.insertExample(ex)}>
                                                ${ex}
                                            </sl-button>
                                        `)}
                                    </div>
                                ` : null}
                            </div>
                        `})}
                        ${filteredFunctions.length === 0 ? html`<div class="docs-empty" style="padding: 2rem; text-align: center; color: var(--text-muted);">No functions found matching your criteria.</div>` : null}
                    </div>
                </sl-tab-panel>

                <sl-tab-panel name="ops" onscroll=${onPanelScroll}>
                    <div class="ops-table-container">
                        <table class="ops-table">
                            <thead>
                                <tr>
                                    ${renderSortHeader('Symbol', 'Symbol')}
                                    ${renderSortHeader('Name', 'Name')}
                                    ${renderSortHeader('Category', 'Category')}
                                    ${renderSortHeader('Precedence', 'Precedence')}
                                    ${renderSortHeader('Assoc', 'Associativity')}
                                </tr>
                            </thead>
                            <tbody>
                                ${sortedOperators.map(op => {
            const symbol = op.Symbol || op.symbol || '';
            const name = op.Name || op.name || 'Unknown';
            const category = op.Category || op.category || 'General';
            const precedence = op.Precedence !== undefined ? op.Precedence : (op.precedence !== undefined ? op.precedence : 0);
            const associativity = op.Associativity || op.associativity || 'Left';

            return html`
                                    <tr>
                                        <td><code class="op-symbol">${symbol}</code></td>
                                        <td>${name}</td>
                                        <td>
                                            <sl-badge size="small" class="shy-badge op-category">
                                                <sl-icon src="${getCategoryIconUrl(category)}" class="cat-icon"></sl-icon> ${category}
                                            </sl-badge>
                                        </td>
                                        <td class="num">${precedence}</td>
                                        <td>${associativity === 'Left' ? '← Left' : '→ Right'}</td>
                                    </tr>
                                `;
        })}
                            </tbody>
                        </table>
                    </div>
                </sl-tab-panel>

                <sl-tab-panel name="settings">
                    <form class="settings-form" onsubmit=${onSaveSettings}>
                        <div class="form-group">
                            <label>
                                <sl-icon src="https://api.iconify.design/lucide/calendar-clock.svg?color=%23cbd5e1" class="setting-icon"></sl-icon>
                                Date Format (e.g. dd/MM/yyyy, MM/dd/yyyy)
                            </label>
                            <sl-input name="dateFormat" value=${state.settings.value.dateFormat} onsl-input=${onInputChange} clearable></sl-input>
                            <div class="settings-presets">
                                ${['dd/MM/yyyy', 'MM/dd/yyyy', 'yyyy-MM-dd', 'd.M.yyyy', 'yyyy/MM/dd', 'd-M-yyyy', 'dd.MM.yyyy', 'yyyy.MM.dd', 'dd-MM-yyyy', 'M-d-yyyy'].map(f => html`
                                    <sl-button size="small" variant="primary" outline=${state.settings.value.dateFormat !== f} onclick=${() => setInputValue('dateFormat', f)}>${f}</sl-button>
                                `)}
                            </div>
                        </div>

                        <div class="form-group">
                            <label>
                                <sl-icon src="https://api.iconify.design/lucide/languages.svg?color=%23cbd5e1" class="setting-icon"></sl-icon>
                                Culture (e.g. en-US, de-DE)
                            </label>
                            <sl-input name="culture" value=${state.settings.value.culture} onsl-input=${onInputChange} clearable></sl-input>
                            <div class="settings-presets">
                                ${[
            { v: 'en-US', l: '🇺🇸 US' }, { v: 'en-GB', l: '🇬🇧 UK' }, { v: 'de-DE', l: '🇩🇪 DE' },
            { v: 'nl-NL', l: '🇳🇱 NL' }, { v: 'it-IT', l: '🇮🇹 IT' }, { v: 'hr-HR', l: '🇭🇷 HR' },
            { v: 'es-ES', l: '🇪🇸 ES' }, { v: 'fr-FR', l: '🇫🇷 FR' }, { v: 'ru-RU', l: '🇷🇺 RU' },
            { v: 'en-CA', l: '🇨🇦 CA' }
        ].map(c => html`
                                    <sl-button size="small" variant="primary" outline=${state.settings.value.culture !== c.v} onclick=${() => setInputValue('culture', c.v)}>${c.l}</sl-button>
                                `)}
                            </div>
                        </div>
                        <div>
                            <div class="form-group" style="flex-direction: row; align-items: center; justify-content: space-between; margin-bottom: 0.5rem;">
                                <label>
                                    <sl-icon src="https://api.iconify.design/lucide/history.svg?color=%23cbd5e1" class="setting-icon"></sl-icon>
                                    Enable History Recording
                                </label>
                                 <sl-switch name="enableHistory" checked=${state.settings.value.enableHistory} onsl-change=${(e) => {
            const isChecked = e.target.checked;
            let newLen = state.settings.value.historyLength;
            if (isChecked && (!newLen || parseInt(newLen) <= 0)) {
                newLen = 10;
            }
            state.settings.value = { ...state.settings.value, enableHistory: isChecked, historyLength: newLen };
        }}></sl-switch>
                            </div>

                            ${state.settings.value.enableHistory ? html`
                                <div class="form-group" style="margin-bottom: 2rem; animation: fadeIn 0.15s ease-out;">
                                    <sl-input name="historyLength" type="number" min="1" max="100" value=${state.settings.value.historyLength}>
                                        <div slot="help-text" class="subtle-help">Maximum number of calculations to keep in history memory.</div>
                                        <sl-button slot="prefix" variant="text" style="padding: 0; margin-left: 0.25rem;" 
                                            onclick=${(e) => { e.preventDefault(); state.settings.value = { ...state.settings.value, historyLength: Math.max(1, parseInt(state.settings.value.historyLength) - 1) }; }}>
                                            <sl-icon name="dash-lg" style="font-size: 0.8rem;"></sl-icon>
                                        </sl-button>
                                        <sl-button slot="suffix" variant="text" style="padding: 0; margin-right: 0.25rem;" 
                                            onclick=${(e) => { e.preventDefault(); state.settings.value = { ...state.settings.value, historyLength: Math.min(100, parseInt(state.settings.value.historyLength) + 1) }; }}>
                                            <sl-icon name="plus-lg" style="font-size: 0.8rem;"></sl-icon>
                                        </sl-button>
                                    </sl-input>
                                </div>
                            ` : html`
                                <div class="form-group" style="margin-bottom: 2rem; opacity: 0.5; pointer-events: none;">
                                    <sl-input name="historyLength" type="number" value=${state.settings.value.historyLength} disabled>
                                        <div slot="help-text" class="subtle-help">Enable history recording to adjust record limit.</div>
                                        <sl-button slot="prefix" variant="text" style="padding: 0; margin-left: 0.25rem;" disabled>
                                            <sl-icon name="dash-lg" style="font-size: 0.8rem;"></sl-icon>
                                        </sl-button>
                                        <sl-button slot="suffix" variant="text" style="padding: 0; margin-right: 0.25rem;" disabled>
                                            <sl-icon name="plus-lg" style="font-size: 0.8rem;"></sl-icon>
                                        </sl-button>
                                    </sl-input>
                                </div>
                            `}
                        </div>
                        <div class="form-actions" style="margin-top: 2rem;">
                            <sl-button variant="primary" type="submit" class="btn-calculate">
                                Save Settings
                            </sl-button>
                        </div>
                    </form>
                </sl-tab-panel>

                <sl-tab-panel name="about" onscroll=${onPanelScroll}>
                    <div class="about-container">
                        <section class="about-section">
                            <div class="about-header" style="align-items: flex-start; gap: 1rem;">
                                <sl-icon src="https://api.iconify.design/lucide/cpu.svg?color=%23cbd5e1" class="about-icon"></sl-icon>
                                <div>
                                    <strong style="color: #cbd5e1; margin-right: 0.5rem; font-size: 1rem;">The Product.</strong>
                                    <span>ShYCalculator is a high-performance .NET expression evaluator (Recursive Shunting Yard) designed for zero-allocation parsing and lightning-fast execution (~380,000 ops/sec). It supports complex mathematical functions, string manipulation, date-time operations, and custom variable injection.</span>
                                </div>
                            </div>
                        </section>

                        <section class="about-section">
                            <div class="about-alert">
                                <sl-icon src="https://api.iconify.design/lucide/alert-triangle.svg?color=%23f59e0b" class="alert-icon"></sl-icon>
                                <div style="display: flex; flex-direction: column; gap: 1rem;">
                                    <div>
                                        <strong style="color: #f59e0b; margin-right: 0.5rem; font-size: 1rem;">The Demo App.</strong>
                                        <span>This web application is a technology demonstration specifically built to test the ShYCalculator WebAssembly (WASM) build.</span>
                                    </div>
                                    <div>
                                        <strong>Sandbox Mode:</strong>
                                        <span>This environment is not intended for production calculations or secure data processing. It's a playground for the engine!</span>
                                    </div>
                                </div>
                            </div>
                        </section>

                        <section class="about-section">
                            <div class="about-header" style="align-items: flex-start; gap: 1rem;">
                                <sl-icon src="https://api.iconify.design/lucide/scroll.svg?color=%23cbd5e1" class="about-icon"></sl-icon>
                                <div>
                                    <strong style="color: #cbd5e1; margin-right: 0.5rem; font-size: 1rem;">Licensing & Vibe.</strong>
                                    <span>This project is released under the <strong>MIT License</strong>. Feel free to download the DLL, integrate it into your projects, and enjoy the speed!</span>
                                </div>
                            </div>
                            <div class="vibe-card">
                                <pre class="vibe-text">
  _  _         _  _  ____                 _   _  _  _            
 | \| | _  _  | || ||_  / ___  _ _  ___  | | | |(_)| |__  ___  _  
 | .  || || | | || | / / / -_)| '_|/ _ \ | |_| || || '_ \/ -_)(_) 
 |_|\_| \_,_| |_||_|/___|\___||_|  \___/  \___/ |_||_.__/\___|(_)
 </pre>
                                <div class="vibe-description">
                                    VIBE CHECK: This code was orchestrated through intent.<br/>
                                    You are free to use, modify, and distribute it.<br/>
                                    Keep the legacy alive. Keep the vibe open.
                                </div>
                            </div>
                        </section>

                        <div class="about-footer">
                            <sl-button href="https://github.com/nullzerovibe/ShYCalculator/blob/main/LICENSE" target="_blank" size="small" outline class="btn-about">
                                <sl-icon slot="prefix" src="https://api.iconify.design/lucide/scale.svg?color=%23cbd5e1"></sl-icon> License
                            </sl-button>
                            <sl-button href="https://github.com/nullzerovibe/ShYCalculator" target="_blank" size="small" outline class="btn-about">
                                <sl-icon slot="prefix" name="github"></sl-icon> Repository
                            </sl-button>
                            <sl-button href="https://x.com/nullzerovibe" target="_blank" size="small" outline class="btn-about">
                                <sl-icon slot="prefix" name="twitter"></sl-icon> Twitter
                            </sl-button>
                            <sl-button href="mailto:nullzerovibe@gmail.com" size="small" outline class="btn-about">
                                <sl-icon slot="prefix" name="envelope"></sl-icon> Email
                            </sl-button>
                        </div>
                    </div>
                </sl-tab-panel>
            </sl-tab-group>

            <sl-button circle class="scroll-top-btn ${state.showScrollTop.value ? 'visible' : ''}" onclick=${scrollToTop}>
                <sl-icon name="chevron-up"></sl-icon>
            </sl-button>
        </sl-dialog>
    `;
};

export const ContactFooter = () => html`
    <div class="contact-footer">
        <a href="mailto:nullzerovibe@gmail.com" class="contact-link">
            <sl-icon name="envelope"></sl-icon>
            <span>nullzerovibe@gmail.com</span>
        </a>
        <a href="https://github.com/nullzerovibe" target="_blank" class="contact-link">
            <sl-icon name="github"></sl-icon>
            <span>github.com/nullzerovibe</span>
        </a>
        <a href="https://x.com/nullzerovibe" target="_blank" class="contact-link">
            <sl-icon name="twitter"></sl-icon>
            <span>x.com/nullzerovibe</span>
        </a>
    </div>
`;

export const App = ({ state, actions }) => {
    useEffect(() => {
        if (state.status.value === 'Initializing...') {
            actions.init();
        }
    }, []);

    return html`
        <div class="app-container single-column">
            <${Header} state=${state} />
            
            <${MainCard} state=${state} actions=${actions} />
            
            <${Documentation} state=${state} actions=${actions} />

            <${ContactFooter} />
        </div>
    `;
};
