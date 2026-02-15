import { h, render } from 'https://esm.sh/preact@10.19.3';
import { useEffect } from 'https://esm.sh/preact@10.19.3/hooks';
import htm from 'https://esm.sh/htm@3.1.1';
import { FLAT_MAP, EXAMPLE_GROUPS, getCategoryIconUrl, getTypeIconUrl, appState, actions } from './logic.js';

const html = htm.bind(h);

// --- HELPERS ---

const highlightExpression = (text, knownNames, variables) => {
    if (!text) return '';
    const varNames = new Set(variables.map(v => v.name));

    // Simple tokenizer for highlighting
    // String: Amber, Operators: Cyan, Numbers: White, Functions: Slate, Booleans: Rose, Variables: Blue, Unknown: Underlined Red
    return text.replace(/('.*?')|\b(true|false)\b|([a-zA-Z_][a-zA-Z0-9_]*)|(\d+(?:\.\d+)?)|([+\-*/^%<>=!&|?:]+)|(\s+)|(.)/g, (match, str, bool, id, num, op, space, other) => {
        if (str) return `<span class="hl-str">${str}</span>`;
        if (bool) return `<span class="hl-bool">${bool}</span>`;
        if (id) {
            if (knownNames.has(id)) return `<span class="hl-func">${id}</span>`;
            if (varNames.has(id)) return `<span class="hl-var">${id}</span>`;
            return `<span class="hl-unknown">${id}</span>`;
        }
        if (num) return `<span class="hl-num">${num}</span>`;
        if (op) return `<span class="hl-op">${op}</span>`;
        if (space) return space;
        return other;
    });
};

const detectVariables = (text, knownNames, variables) => {
    if (!text) return [];
    // Remove strings before detecting variables to avoid false positives
    const cleanText = text.replace(/'.*?'/g, '');
    const varNames = new Set(variables.map(v => v.name));
    const cand = new Set();
    const matches = cleanText.matchAll(/\b([a-zA-Z_][a-zA-Z0-9_]*)\b/g);
    for (const m of matches) {
        const id = m[1];
        if (!knownNames.has(id) && !varNames.has(id)) {
            cand.add(id);
        }
    }
    return Array.from(cand);
};

// --- COMPONENTS ---

export const SyntaxEditor = ({ value, onInput, onKeyDown, state }) => {
    const onInternalInput = (e) => {
        onInput(e);
        // Detect suggestions
        const cands = detectVariables(e.target.value, state.knownNames.value, state.variables.value);
        state.suggestions.value = cands;
    };

    const htmlContent = highlightExpression(value, state.knownNames.value, state.variables.value);

    return html`
        <div class="syntax-editor">
            <div class="highlight-overlay" dangerouslySetInnerHTML=${{ __html: htmlContent }}></div>
            <textarea 
                placeholder="Enter expression..." 
                value=${value}
                oninput=${onInternalInput}
                onkeydown=${onKeyDown}
                spellcheck="false"
                autocomplete="off"
                autocorrect="off"
                autocapitalize="off"
            ></textarea>
        </div>
    `;
};

export const Header = ({ state, actions }) => html`
    <header class="app-header">
        <div class="logo-area">
            <h1 class="app-title">ShYCalculator</h1>
            <p class="app-subtitle">
                High-performance .NET WASM Expression Evaluator 
                <span class="version-tag">v${state.version?.value || '...'}</span>
            </p>
        </div>
        <div class="header-actions">
            <div class="status-indicator">
                ${state.isOfflineReady?.value ? html`
                    <sl-tooltip content="Offline Ready">
                        <sl-icon name="cloud-check" class="offline-icon"></sl-icon>
                    </sl-tooltip>
                ` : null}
                <sl-badge variant="${state.isReady?.value ? 'success' : 'danger'}">
                    ${state.status?.value || 'Loading...'}
                </sl-badge>
            </div>
        </div>
    </header>
`;

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
                        <sl-icon slot="prefix" name="book"></sl-icon> Docs & Settings
                    </sl-button>
                </div>
            </div>

            <div class="form-section">
                <label class="section-label">
                    <sl-icon src="https://api.iconify.design/lucide/terminal.svg?color=%23cbd5e1" class="section-icon"></sl-icon>
                    Mathematical Expression
                </label>
                <${SyntaxEditor} 
                    value=${state.input.value}
                    onInput=${onInput}
                    onKeyDown=${e => e.key === 'Enter' && actions.calculate()}
                    state=${state}
                />
                
            </div>

            <div class="form-section">
                <div class="section-header">
                    <label class="section-label">
                        <sl-icon src="https://api.iconify.design/lucide/variable.svg?color=%23cbd5e1" class="section-icon"></sl-icon>
                        Variables (Context)
                    </label>
                    <div class="section-actions">
                        ${(() => {
            const items = state.suggestions.value;
            const shown = items.slice(0, 3);
            const remaining = items.slice(3);

            return html`
                                ${shown.map(v => html`
                                    <sl-button size="small" variant="primary" outline class="suggestion-pill u-mr-05" onclick=${() => {
                    actions.addVar();
                    const lastIdx = state.variables.value.length - 1;
                    actions.updateVar(lastIdx, 'name', v);
                    state.suggestions.value = state.suggestions.value.filter(s => s !== v);
                }}>
                                        <sl-icon slot="prefix" name="plus"></sl-icon> '${v}'?
                                    </sl-button>
                                `)}
                                
                                ${remaining.length > 0 ? html`
                                    <sl-button size="small" variant="primary" outline class="suggestion-pill u-mr-05" onclick=${() => {
                        items.forEach(v => {
                            actions.addVar();
                            const lastIdx = state.variables.value.length - 1;
                            actions.updateVar(lastIdx, 'name', v);
                        });
                        state.suggestions.value = [];
                    }}>
                                        <sl-icon slot="prefix" name="layers"></sl-icon> Create All (${items.length})
                                    </sl-button>
                                ` : null}
                            `;
        })()}
                        <sl-button size="small" variant="neutral" outline class="btn-clear-all btn-secondary u-mr-05" onclick=${actions.clearVars}>
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

                <div class="result-box ${state.result.value === 'Error' || state.result.value.startsWith('Interop') ? 'error' : ''}">
                    <div class="result-body">
                        <div class="result-value">${state.result.value}</div>
                        <div class="result-actions ${state.result.value === '---' || state.result.value === 'Error' || state.result.value === 'null' ? 'u-hidden' : ''}">
                            <sl-icon-button name="copy" label="Copy Result" onclick=${() => actions.copyToClipboard(state.result.value)} class="copy-btn"></sl-icon-button>
                        </div>
                    </div>

                    <div class="result-footer">
                        <div class="result-badge-area ${state.message.value ? 'u-visible' : 'u-invisible'} ${state.result.value === 'Error' ? 'u-hidden' : ''}">
                            <sl-badge size="small" class="shy-badge">
                                <sl-icon src="${getTypeIconUrl(state.resultType.value)}" class="type-icon-sm"></sl-icon>
                                ${state.resultType.value}
                            </sl-badge>
                            <span class="result-msg">${state.message.value}</span>
                        </div>
                        <div class="result-stats ${state.result.value === 'Error' ? 'u-hidden' : ''}">
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
                <div class="history-section">
                    <div class="history-header">
                        <label class="section-label">History</label>
                        <div class="section-actions">
                            <sl-dropdown hoist>
                                <sl-button slot="trigger" size="small" variant="neutral" outline class="btn-secondary u-mr-05" caret>
                                    <sl-icon slot="prefix" name="download"></sl-icon> Export
                                </sl-button>
                                <sl-menu>
                                    <sl-menu-item onclick=${() => actions.exportHistory('csv')}>Export as CSV</sl-menu-item>
                                    <sl-menu-item onclick=${() => actions.exportHistory('json')}>Export as JSON</sl-menu-item>
                                </sl-menu>
                            </sl-dropdown>
                            <sl-button size="small" variant="neutral" outline class="btn-clear-all btn-secondary" onclick=${actions.clearHistory}>
                                <sl-icon slot="prefix" name="trash"></sl-icon> Clear
                            </sl-button>
                        </div>
                    </div>
                    <div class="history-list">
                        ${state.history.value.map(item => html`
                            <div class="history-item" onclick=${() => actions.loadHistoryItem(item)}>
                                <div class="history-item-main">
                                    <span class="hist-expr">${item.expr}</span>
                                    <span class="hist-res-group">
                                        <span class="hist-eq">=</span>
                                        <span class="hist-res">${item.result}</span>
                                    </span>
                                </div>
                                <div class="history-item-meta">
                                    <span class="hist-vars">
                                        ${item.vars && item.vars.length > 0 ? item.vars.map(v => `${v.name}=${v.value}`).join(', ') : ''}
                                    </span>
                                    ${item.resultType ? html`
                                        <sl-badge size="small" class="shy-badge u-flex u-items-center u-gap-025">
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
        const isWildcard = !category || category === ALL_CATS || category === '';
        const matchesCategory = isWildcard || (fCategory === category);
        const matchesQuery = !query || name.includes(query) || desc.includes(query);
        return matchesQuery && matchesCategory;
    });

    const onSearch = (e) => {
        const val = e.target.value;
        state.docSearch.value = val;
        sessionStorage.setItem('docSearch', val);
    };
    const onCategory = (e) => {
        e.stopPropagation();
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
                             class="${isActive ? 'u-text-accent' : ''}"
                             style="font-size: 0.7rem; opacity: ${isActive ? 1 : 0.4};">
                    </sl-icon>
                </div>
            </th>
        `;
    };

    const onSaveSettings = (e) => {
        e.preventDefault();
        const form = e.target;
        const getVal = (name) => form.querySelector(`[name="${name}"]`)?.value || '';
        const isChecked = (name) => form.querySelector(`[name="${name}"]`)?.checked || false;

        actions.saveSettings({
            theme: getVal('theme'),
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

    return html`
        <sl-dialog class="docs-dialog" 
            open=${state.docsOpen.value} 
            onsl-request-close=${(e) => {
            // Ensure only dialog-level close requests are honored
            if (e.target !== e.currentTarget) return;
        }}
            onsl-after-hide=${(e) => {
            // Ensure events from children (like sl-select) don't close the dialog
            if (e.target !== e.currentTarget) return;
            state.docsOpen.value = false;
            state.showScrollTop.value = false;
        }}>
            <div slot="label" class="u-flex u-items-center u-gap-075">
                <sl-icon src="https://api.iconify.design/lucide/book-open.svg?color=%23cbd5e1" class="doc-header-icon"></sl-icon>
                Reference Guide
                ${state.isOfflineReady?.value ? html`
                    <sl-tooltip content="Offline Ready">
                        <sl-icon name="cloud-check" class="u-text-success u-ml-05"></sl-icon>
                    </sl-tooltip>
                ` : null}
            </div>
            <sl-tab-group onsl-tab-show=${() => state.showScrollTop.value = false}>
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
                            value=${state.docSearch.value} class="u-flex-2">
                            <sl-icon name="search" slot="prefix"></sl-icon>
                        </sl-input>
                        <sl-select value=${category} onsl-change=${onCategory} clearable=${category !== ALL_CATS} hoist class="u-flex-1">
                            <sl-icon src="${getCategoryIconUrl(category)}" slot="prefix" class="cat-icon-sm u-ml-05"></sl-icon>
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
            const fCat = fn.Category || fn.category || 'General';
            const description = fn.Description || fn.description || '';
            const examples = fn.Examples || fn.examples || [];

            return html`
                            <div class="doc-card">
                                <div class="doc-card-header">
                                    <span class="doc-name">${name}</span>
                                    <sl-copy-button value=${name} class="doc-copy-btn"></sl-copy-button>
                                    <span class="doc-args">(${args.join(', ') || ''})</span>
                                    <sl-badge variant="primary" size="small" outline class="doc-category">
                                        <sl-icon src="${getCategoryIconUrl(fCat)}" class="cat-icon"></sl-icon> ${fCat}
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
                        ${filteredFunctions.length === 0 ? html`<div class="docs-empty">No functions found matching your criteria.</div>` : null}
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
                const cat = op.Category || op.category || 'General';
                const precedence = op.Precedence !== undefined ? op.Precedence : (op.precedence !== undefined ? op.precedence : 0);
                const associativity = op.Associativity || op.associativity || 'Left';

                return html`
                                    <tr>
                                        <td>
                                            <div class="u-flex u-items-center u-justify-center u-gap-025">
                                                <code class="op-symbol">${symbol}</code>
                                                <sl-copy-button value=${symbol} class="doc-copy-btn"></sl-copy-button>
                                            </div>
                                        </td>
                                        <td>${name}</td>
                                        <td>
                                            <sl-badge size="small" class="shy-badge op-category">
                                                <sl-icon src="${getCategoryIconUrl(cat)}" class="cat-icon"></sl-icon> ${cat}
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
                        <div class="form-group u-mb-2">
                            <label>
                                <sl-icon src="https://api.iconify.design/lucide/palette.svg?color=%23cbd5e1" class="setting-icon"></sl-icon>
                                Appearance Theme
                            </label>
                            <sl-select name="theme" value=${state.settings.value.theme} onsl-change=${onInputChange}>
                                <sl-option value="auto">System Default (Auto)</sl-option>
                                <sl-option value="light">Industrial White (Light)</sl-option>
                                <sl-option value="dark">Industrial Black (Dark)</sl-option>
                            </sl-select>
                        </div>

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
                                ${[{ v: 'en-US', l: '🇺🇸 US' }, { v: 'en-GB', l: '🇬🇧 UK' }, { v: 'de-DE', l: '🇩🇪 DE' },
        { v: 'nl-NL', l: '🇳🇱 NL' }, { v: 'it-IT', l: '🇮🇹 IT' }, { v: 'hr-HR', l: '🇭🇷 HR' },
        { v: 'es-ES', l: '🇪🇸 ES' }, { v: 'fr-FR', l: '🇫🇷 FR' }, { v: 'ru-RU', l: '🇷🇺 RU' },
        { v: 'en-CA', l: '🇨🇦 CA' }].map(c => html`
                                    <sl-button size="small" variant="primary" outline=${state.settings.value.culture !== c.v} onclick=${() => setInputValue('culture', c.v)}>${c.l}</sl-button>
                                `)}
                            </div>
                        </div>

                        <div class="u-mt-1">
                            <div class="u-flex u-items-center u-justify-between u-mb-05">
                                <label class="u-mt-0">
                                    <sl-icon src="https://api.iconify.design/lucide/history.svg?color=%23cbd5e1" class="setting-icon"></sl-icon>
                                    Enable History Recording
                                </label>
                                 <sl-switch name="enableHistory" checked=${state.settings.value.enableHistory} onsl-change=${(e) => {
            const isChecked = e.target.checked;
            let newLen = state.settings.value.historyLength;
            if (isChecked && (!newLen || Number.parseInt(newLen) <= 0)) newLen = 10;
            state.settings.value = { ...state.settings.value, enableHistory: isChecked, historyLength: newLen };
        }}></sl-switch>
                            </div>

                            ${state.settings.value.enableHistory ? html`
                                <div class="form-group u-mb-2" style="animation: fadeIn 0.15s ease-out;">
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
                                <div class="form-group u-mb-2" style="opacity: 0.5; pointer-events: none;">
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
                        <div class="form-actions u-mt-2">
                            <sl-button variant="primary" type="submit" class="btn-calculate">
                                Save Settings
                            </sl-button>
                        </div>
                    </form>
                </sl-tab-panel>

                <sl-tab-panel name="about">
                    <div class="about-container">
                        <section class="about-section">
                            <div class="about-header">
                                <sl-icon src="https://api.iconify.design/lucide/cpu.svg?color=%23cbd5e1" class="about-icon"></sl-icon>
                                <div>
                                    <strong>The Product.</strong>
                                    <span>ShYCalculator is a high-performance .NET expression evaluator (Recursive Shunting Yard) designed for zero-allocation parsing and lightning-fast execution (~380,000 ops/sec). It supports complex mathematical functions, string manipulation, date-time operations, and custom variable injection.</span>
                                </div>
                            </div>
                        </section>

                        <section class="about-section">
                            <div class="about-alert">
                                <sl-icon src="https://api.iconify.design/lucide/alert-triangle.svg?color=%23f59e0b" class="alert-icon"></sl-icon>
                                <div>
                                    <div class="u-mb-1">
                                        <strong>The Demo App.</strong>
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
                            <div class="about-header">
                                <sl-icon src="https://api.iconify.design/lucide/scroll.svg?color=%23cbd5e1" class="about-icon"></sl-icon>
                                <div>
                                    <a href="https://github.com/nullzerovibe/ShYCalculator/blob/main/VIBE.md" target="_blank" class="about-link u-mb-025">Licensing & Vibe.</a>
                                    <span>This project is released under the <a href="https://github.com/nullzerovibe/ShYCalculator/blob/main/LICENSE" target="_blank" class="about-link">MIT License</a>. Feel free to download the <a href="https://www.nuget.org/packages/ShYCalculator" target="_blank" class="about-link">NuGet package</a>, integrate it into your projects, and enjoy the speed!</span>
                                </div>
                            </div>
                            <div class="vibe-card">
                                <pre class="vibe-text">  _  _         _  _  ____                 _   _  _  _            
 | \\| | _  _  | || ||_  / ___  _ _  ___  | | | |(_)| |__  ___  _  
 | .  || || | | || | / / / -_)| '_|/ _ \\ | |_| || || '_ \\/ -_)(_) 
 |_|\\_| \\_,_| |_||_|/___|\\___||_|  \\___/  \\___/ |_||_.__/\\___|(_)
 </pre>
                                <div class="vibe-description">
                                    VIBE CHECK: This code was orchestrated through intent.<br/>
                                    You are free to use, modify, and distribute it.<br/>
                                    Keep the legacy alive. Keep the vibe open.
                                </div>
                            </div>
                        </section>

                        <div class="about-footer">
                            <a href="https://github.com/nullzerovibe/ShYCalculator" target="_blank" rel="noopener noreferrer" class="footer-badge">
                                <img src="https://img.shields.io/github/stars/nullzerovibe/ShYCalculator?style=flat-square&logo=github&label=github&color=21262d" alt="GitHub Badge" />
                            </a>
                            <a href="https://www.nuget.org/packages/ShYCalculator" target="_blank" rel="noopener noreferrer" class="footer-badge">
                                <img src="https://img.shields.io/nuget/v/ShYCalculator?style=flat-square&logo=nuget&label=nuget&color=21262d" alt="NuGet Badge" />
                            </a>
                            <a href="https://github.com/nullzerovibe/ShYCalculator/blob/main/LICENSE" target="_blank" rel="noopener noreferrer" class="footer-badge">
                                <img src="https://img.shields.io/github/license/nullzerovibe/ShYCalculator?style=flat-square&label=license&color=21262d" alt="License Badge" />
                            </a>
                            <a href="https://github.com/nullzerovibe/ShYCalculator/blob/main/VIBE.md" target="_blank" rel="noopener noreferrer" class="footer-badge">
                                <img src="https://img.shields.io/badge/vibe-MIT-21262d?style=flat-square&logo=sparkles" alt="Vibe Badge" />
                            </a>
                            <a href="https://x.com/nullzerovibe" target="_blank" rel="noopener noreferrer" class="footer-badge">
                                <img src="https://img.shields.io/twitter/follow/nullzerovibe?style=flat-square&logo=twitter&color=21262d" alt="Twitter Badge" />
                            </a>
                            <a href="mailto:nullzerovibe@gmail.com" class="footer-badge">
                                <img src="https://img.shields.io/badge/email-contact-21262d?style=flat-square&logo=gmail" alt="Email Badge" />
                            </a>
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

    useEffect(() => {
        const handleKeyDown = (e) => {
            // / to focus input (if not already focused and not in an input/textarea)
            if (e.key === '/' && document.activeElement.tagName !== 'INPUT' && document.activeElement.tagName !== 'TEXTAREA') {
                e.preventDefault();
                const input = document.querySelector('sl-input[placeholder="Enter expression..."]');
                if (input) {
                    input.focus();
                    // Select all text if there is any
                    input.select();
                }
            }
            // Esc to clear input (if input is focused)
            if (e.key === 'Escape' && document.activeElement.tagName === 'INPUT') {
                state.input.value = '';
                actions.calculate();
            }
            // Ctrl+Enter to trigger calculate
            if (e.key === 'Enter' && e.ctrlKey) {
                actions.calculate();
            }
        };

        globalThis.addEventListener('keydown', handleKeyDown);
        // Listen for SW readiness
        const onSwReady = () => {
            state.isOfflineReady.value = true;
        };
        globalThis.addEventListener('sw-ready', onSwReady);

        return () => {
            globalThis.removeEventListener('keydown', handleKeyDown);
            globalThis.removeEventListener('sw-ready', onSwReady);
        };
    }, []);

    // Reactive Feedback: Result Ping
    useEffect(() => {
        const resEl = document.querySelector('.result-value');
        if (resEl && state.result.value !== '0' && state.result.value !== '') {
            resEl.classList.remove('result-animate');
            void resEl.offsetWidth; // Trigger reflow
            resEl.classList.add('result-animate');
        }
    }, [state.result.value]);

    return html`
        <div class="app-container single-column">
            <${Header} state=${state} actions=${actions} />
            <${MainCard} state=${state} actions=${actions} />
            <${Documentation} state=${state} actions=${actions} />
            <${ContactFooter} />
        </div>
    `;
};

// --- RENDER ---
render(html`<${App} state=${appState} actions=${actions} />`, document.getElementById('app'));
