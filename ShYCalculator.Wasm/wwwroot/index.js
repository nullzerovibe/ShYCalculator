import { h, render } from 'https://esm.sh/preact@10.19.3';
import { useEffect, useMemo, useRef, useState } from 'https://esm.sh/preact@10.19.3/hooks';
import htm from 'https://esm.sh/htm@3.1.1';
import { FLAT_MAP, EXAMPLE_GROUPS, getCategoryIconUrl, getTypeIconUrl, appState, actions } from './logic.js';

const html = htm.bind(h);

// --- HELPERS ---

const highlightExpression = (text, knownNames, variables, forceKnown = false) => {
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
            if (forceKnown) return `<span class="hl-var">${id}</span>`;
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

const toggleTransparency = (active, closingDialogName = null) => {
    const appContainer = document.querySelector('.app-container');
    if (appContainer) {
        if (active) {
            appContainer.classList.add('panel-transparent');
        } else {
            // Only remove transparency if NO other dialogs are open
            let anyOpen = false;
            if (closingDialogName !== 'docs' && appState.docsOpen.value) anyOpen = true;
            if (closingDialogName !== 'save' && appState.saveSnippetOpen.value) anyOpen = true;

            if (!anyOpen) {
                // Force instant transition removal to prevent flicker
                appContainer.style.transition = 'none';
                appContainer.style.opacity = '1';
                // Trigger reflow
                void appContainer.offsetHeight;

                appContainer.classList.remove('panel-transparent');

                // restore natural behavior after paint
                requestAnimationFrame(() => {
                    appContainer.style.transition = '';
                    appContainer.style.opacity = '';
                });
            }
        }
    }
};

// --- COMPONENTS ---

export const SyntaxEditor = ({ value, onInput, onKeyDown, state, name, forceKnown = false }) => {
    const onInternalInput = (e) => {
        onInput(e);
    };

    const htmlContent = highlightExpression(value, state.knownNames.value, state.variables.value, forceKnown);

    return html`
        <div class="syntax-editor">
            <div class="highlight-overlay" dangerouslySetInnerHTML=${{ __html: htmlContent }}></div>
            <textarea 
                name=${name}
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
                <sl-badge class="version-tag shy-badge" size="small">v${state.version?.value || '...'}</sl-badge>
            </p>
        </div>
    </header>
`;

export const SaveSnippetDialog = ({ state, actions }) => {
    // Default to empty object if null to prevent crash, but keep dialog mounted
    const editing = state.editingSnippet.value || { label: '', value: '', icon: '', id: null };
    const isEdit = !!editing.id;

    // Validation state
    const [validationResult, setValidationResult] = useState(null);
    const [isValidating, setIsValidating] = useState(false);

    // Reset validation when dialog opens or value changes
    useEffect(() => {
        if (state.saveSnippetOpen.value && editing.value) {
            // Auto-validate on open
            setIsValidating(true);
            actions.validateExpression(editing.value, true).then(res => {
                // Ensure we haven't closed or changed
                if (state.saveSnippetOpen.value) {
                    setValidationResult(res);
                }
                setIsValidating(false);
            });
        } else {
            setValidationResult(null);
            setIsValidating(false);
        }
    }, [state.saveSnippetOpen.value]);

    // Clear validation when user types
    useEffect(() => {
        if (state.saveSnippetOpen.value) {
            setValidationResult(null);
        }
    }, [editing.value]);


    const onHide = () => {
        state.saveSnippetOpen.value = false;
        state.editingSnippet.value = null;
        setValidationResult(null);
    };

    const onTest = async () => {
        if (!editing.value) return;
        setIsValidating(true);
        setValidationResult(null);

        // Short delay to show loading state
        await new Promise(r => setTimeout(r, 300));

        const result = await actions.validateExpression(editing.value, true);
        setValidationResult(result);
        setIsValidating(false);
    };

    const onSubmit = (e) => {
        e.preventDefault();
        const formData = new FormData(e.target);
        const name = formData.get('name')?.trim();
        const value = formData.get('value')?.trim();
        const icon = formData.get('icon');

        const errors = { name: '', value: '', icon: '' };

        // --- VALIDATION ---
        if (!name) {
            errors.name = "Please enter a name for the expression.";
        } else {
            // Check for unique name
            const isDuplicate = state.snippets.value.some(s =>
                s.label.toLowerCase() === name.toLowerCase() &&
                (!isEdit || s.id !== editing.id)
            );
            if (isDuplicate) {
                errors.name = `An expression named "${name}" already exists.`;
            }
        }

        if (!value) {
            errors.value = "Expression cannot be empty.";
        }

        if (!icon) {
            errors.icon = "Please select an icon category.";
        }

        if (errors.name || errors.value || errors.icon) {
            state.snippetErrors.value = errors;
            return;
        }

        // Find the group based on the icon selection
        let group = 'Custom Expressions';
        for (const cat of catIcons) {
            if (cat.icons.some(i => i.value === icon)) {
                group = cat.name === 'Custom' ? 'Custom Expressions' : cat.name;
                break;
            }
        }

        actions.saveSnippet(name, icon, value, group);
        state.saveSnippetOpen.value = false;
    };

    const catIcons = [
        {
            name: 'Arithmetical',
            icons: [
                { name: 'Arithmetical', value: 'arithmetical' },
                { name: 'Plus', value: 'plus' },
                { name: 'Minus', value: 'minus' },
                { name: 'Equal', value: 'equal' },
                { name: 'Parentheses', value: 'parentheses' }
            ]
        },
        {
            name: 'Numeric',
            icons: [
                { name: 'Numeric', value: 'numeric' },
                { name: 'Hash', value: 'hash' },
                { name: 'List', value: 'list-ordered' },
                { name: 'Percent', value: 'percent' },
                { name: 'Dice', value: 'dice-5' }
            ]
        },
        {
            name: 'Scientific',
            icons: [
                { name: 'Scientific', value: 'scientific' },
                { name: 'Chemistry', value: 'flask-conical' },
                { name: 'Physics', value: 'atom' },
                { name: 'Infinity', value: 'infinity' },
                { name: 'Brain', value: 'brain' },
                { name: 'Activity', value: 'activity' }
            ]
        },
        {
            name: 'String',
            icons: [
                { name: 'String', value: 'type' },
                { name: 'Select Text', value: 'text-select' },
                { name: 'Message', value: 'message-square' },
                { name: 'Tags', value: 'tags' }
            ]
        },
        {
            name: 'Logical',
            icons: [
                { name: 'Logical', value: 'logical' },
                { name: 'Binary', value: 'binary' },
                { name: 'CPU', value: 'cpu' },
                { name: 'Logic Tree', value: 'list-tree' },
                { name: 'Network', value: 'network' }
            ]
        },
        {
            name: 'Date',
            icons: [
                { name: 'Date', value: 'calendar' },
                { name: 'Clock', value: 'clock' },
                { name: 'History', value: 'history' },
                { name: 'Sun', value: 'sun' },
                { name: 'Moon', value: 'moon' }
            ]
        },
        {
            name: 'Custom',
            icons: [
                { name: 'Bookmark', value: 'bookmark' },
                { name: 'Heart', value: 'heart' },
                { name: 'Star', value: 'star' },
                { name: 'Flash', value: 'zap' },
                { name: 'Rocket', value: 'rocket' },
                { name: 'Tool', value: 'wrench' },
                { name: 'Layers', value: 'layers' }
            ]
        }
    ];

    const errors = state.snippetErrors.value;

    useEffect(() => {
        // Transparency handled via events for precise timing
    }, [state.saveSnippetOpen.value]);

    const timerRef = useRef(null);

    return html`
        <sl-dialog 
            class="save-dialog"
            open=${state.saveSnippetOpen.value} 
            label=${isEdit ? 'Edit expression' : 'Create new expression'}
            onsl-show=${(e) => {
            // Prevent tooltips from re-triggering show logic
            if (e.target !== e.currentTarget) return;

            if (timerRef.current) clearTimeout(timerRef.current);
            timerRef.current = setTimeout(() => toggleTransparency(true), 100);
        }}
            onsl-request-close=${(e) => {
            if (e.target !== e.currentTarget) return;
        }}
            onsl-hide=${(e) => {
            if (e.target !== e.currentTarget) return;
            if (timerRef.current) clearTimeout(timerRef.current);
            toggleTransparency(false, 'save');
        }}
            onsl-after-hide=${(e) => {
            if (e.target !== e.currentTarget) return;
            onHide();
        }}
        >
            <div slot="label" class="u-flex u-items-center u-gap-075">
                <sl-icon src="https://api.iconify.design/lucide/bookmark-plus.svg?color=%23cbd5e1" class="doc-header-icon"></sl-icon>
                ${isEdit ? 'Edit expression' : 'Create new expression'}
            </div>
            <form id="save-snippet-form" onsubmit=${onSubmit} class="snippet-form" novalidate>
                <div class="dialog-section">
                    <div class="form-group">
                        <label>Name</label>
                        <sl-input 
                            name="name" 
                            placeholder="E.g., Monthly Compound Interest" 
                            autocomplete="off"
                            value=${editing.label}
                            onsl-input=${(e) => {
            state.editingSnippet.value = { ...state.editingSnippet.value, label: e.target.value };
            if (state.snippetErrors.value.name) {
                state.snippetErrors.value = { ...state.snippetErrors.value, name: '' };
            }
        }}
                        ></sl-input>
                        ${errors.name ? html`<div class="error-text">${errors.name}</div>` : html`<div class="subtle-help">Give your expression a descriptive title.</div>`}
                    </div>
                </div>
                <div class="dialog-section">
                    <div class="form-group">
                        <label>Expression</label>
                        <${SyntaxEditor} 
                            name="value" 
                            value=${editing.value}
                            onInput=${(e) => {
            state.editingSnippet.value = { ...state.editingSnippet.value, value: e.target.value };
            if (state.snippetErrors.value.value) {
                state.snippetErrors.value = { ...state.snippetErrors.value, value: '' };
            }
        }}
                            state=${state}
                            forceKnown=${true}
                        />
                        ${errors.value ? html`<div class="error-text">${errors.value}</div>` : html`<div class="subtle-help">You can refine the expression before saving.</div>`}
                        
                        ${validationResult ? html`
                            <div class="validation-result ${validationResult.success || validationResult.Success ? 'valid' : 'invalid'}">
                                ${validationResult.success || validationResult.Success ? html`
                                    <div class="u-flex u-items-center u-gap-05">
                                        <sl-icon name="check-circle" class="valid-icon"></sl-icon>
                                        <span>Syntax is valid. Ready to save.</span>
                                    </div>
                                ` : html`
                                    <div class="u-flex u-flex-col u-gap-05">
                                        <div class="u-flex u-items-center u-gap-05">
                                            <sl-icon name="x-circle" class="invalid-icon"></sl-icon>
                                            <span class="u-font-semibold">Validation Errors:</span>
                                        </div>
                                        <ul class="error-list">
                                            ${(validationResult.errors || validationResult.Errors || []).map(err => html`
                                                <li>${err.message || err.Message} ${err.startIndex >= 0 ? `(at col ${err.startIndex})` : ''}</li>
                                            `)}
                                        </ul>
                                    </div>
                                `}
                            </div>
                        ` : null}
                    </div>
                </div>
                
                <div class="dialog-section">
                    <div class="form-group">
                        <label>Select Category</label>
                        <sl-radio-group 
                            name="icon" 
                            value=${editing.icon} 
                            class="icon-selector"
                            onsl-change=${(e) => {
            state.editingSnippet.value = { ...state.editingSnippet.value, icon: e.target.value };

            if (state.snippetErrors.value.icon) {
                state.snippetErrors.value = { ...state.snippetErrors.value, icon: '' };
            }
        }}
                        >
                            <div class="icon-grid icon-grid-flat">
                                ${catIcons.flatMap(cat => cat.icons).map(icon => {
            const iconSrc = getCategoryIconUrl(icon.value);
            return html`
                                        <sl-tooltip content=${icon.name} hoist>
                                            <sl-radio-button value=${icon.value} class="icon-tile">
                                                <div class="tile-content">
                                                    <sl-icon src=${iconSrc}></sl-icon>
                                                </div>
                                            </sl-radio-button>
                                        </sl-tooltip>
                                    `;
        })}
                            </div>
                        </sl-radio-group>
                        ${errors.icon ? html`<div class="error-text u-mt-05">${errors.icon}</div>` : ''}
                    </div>
                </div>
            </form>

            <div slot="footer" class="snippet-dialog-footer">
                <sl-button variant="neutral" outline class="btn-test u-mr-auto" onclick=${onTest} loading=${isValidating} disabled=${!editing.value}>
                    <sl-icon slot="prefix" name="cpu"></sl-icon> Test
                </sl-button>

                <sl-button variant="text" class="btn-cancel" onclick=${onHide}>
                    Cancel
                </sl-button>
                <sl-button variant="primary" type="submit" form="save-snippet-form" class="premium-save-btn">
                    ${isEdit ? 'Update' : 'Create'}
                </sl-button>
            </div>
        </sl-dialog>
    `;
};

export const ExpressionCombobox = ({ state, actions }) => {
    const dropdownRef = useRef();

    useEffect(() => {
        const updateWidth = () => {
            if (!dropdownRef.current) return;

            const container = dropdownRef.current.closest('.form-section');
            const trigger = dropdownRef.current.querySelector('[slot="trigger"]');

            if (container && trigger) {
                const cRect = container.getBoundingClientRect();
                const tRect = trigger.getBoundingClientRect();

                if (cRect.width > 0) {
                    dropdownRef.current.style.setProperty('--panel-width', `${cRect.width}px`);
                    dropdownRef.current.skidding = cRect.left - tRect.left;
                }
            }
        };

        // Window resize is still important
        globalThis.addEventListener('resize', updateWidth);

        // Sync when dropdown opens
        const handleShow = () => updateWidth();
        dropdownRef.current?.addEventListener('sl-show', handleShow);

        // One-time initial sync
        updateWidth();

        return () => {
            globalThis.removeEventListener('resize', updateWidth);
            dropdownRef.current?.removeEventListener('sl-show', handleShow);
        };
    }, []);

    const onSnippetSelect = (snippet) => {
        actions.loadSnippet(snippet);
        state.selectedIdx.value = snippet.id;
        if (dropdownRef.current) dropdownRef.current.hide();
    };

    const query = state.snippetSearch.value.toLowerCase().trim();
    const snippets = state.snippets.value;

    const filtered = snippets.filter(s =>
        s.label.toLowerCase().includes(query) ||
        s.value.toLowerCase().includes(query) ||
        (s.group && s.group.toLowerCase().includes(query))
    );

    const pinned = filtered.filter(s => s.pinned);
    const others = filtered.filter(s => !s.pinned);

    const otherGroups = others.reduce((acc, s) => {
        const g = s.group || 'Others';
        if (!acc[g]) acc[g] = { label: g, icon: s.icon, items: [] };
        acc[g].items.push(s);
        return acc;
    }, {});

    const groups = [];
    if (pinned.length > 0) {
        groups.push({ label: 'Pinned', icon: 'pin', items: pinned });
    }
    groups.push(...Object.values(otherGroups));

    const selectedSnippet = snippets.find(s => s.id === state.selectedIdx.value);

    return html`
        <sl-dropdown ref=${dropdownRef} class="expression-combobox-dropdown" distance="8" placement="bottom-start" hoist>
            <div slot="trigger" class="combobox-trigger">
                <sl-icon src="${getCategoryIconUrl(selectedSnippet?.icon || 'list-plus')}" class="trigger-icon"></sl-icon>
                <div class="trigger-label">
                    ${selectedSnippet ? selectedSnippet.label : 'Select an expression...'}
                </div>
                <sl-icon name="chevron-down" class="ml-auto opacity-50"></sl-icon>
            </div>

            <div class="combobox-panel">
                <div class="combobox-search">
                    <sl-input 
                        placeholder="Search expressions..." 
                        size="small" 
                        value=${state.snippetSearch.value}
                        oninput=${(e) => state.snippetSearch.value = e.target.value}
                        clearable
                        onsl-clear=${() => state.snippetSearch.value = ''}
                        autocomplete="off"
                        autocorrect="off"
                        autocapitalize="off"
                        spellcheck="false"
                    >
                        <sl-icon name="search" slot="prefix"></sl-icon>
                    </sl-input>
                </div>
                
                <sl-menu class="combobox-menu">
                    ${groups.length === 0 ? html`<div class="empty-state-small u-p-1">No matches found</div>` : null}
                    ${groups.map((group, gIdx, gArr) => html`
                        <sl-menu-label>
                            <sl-icon src="${getCategoryIconUrl(group.icon || 'collection')}" class="ex-group-icon"></sl-icon>
                            ${group.label}
                        </sl-menu-label>
                        ${group.items.map(s => html`
                            <sl-menu-item class="snippet-item ${s.id === state.selectedIdx.value ? 'is-selected' : ''}" onclick=${() => onSnippetSelect(s)}>
                                <sl-icon src="${getCategoryIconUrl(s.icon)}" slot="prefix" class="snippet-icon"></sl-icon>
                                <div class="snippet-info">
                                    <div class="snippet-label">${s.label}</div>
                                    <div class="snippet-preview u-mono" dangerouslySetInnerHTML=${{ __html: highlightExpression(s.value, state.knownNames.value, state.variables.value, true) }}></div>
                                </div>
                                <div slot="suffix" class="snippet-actions" onclick=${(e) => e.stopPropagation()}>
                                    <sl-tooltip content=${s.pinned ? 'Unpin' : 'Pin'}>
                                        <sl-icon-button 
                                            name=${s.pinned ? 'pin-fill' : 'pin'} 
                                            class="action-btn ${s.pinned ? 'pinned' : ''}"
                                            onclick=${() => actions.togglePinSnippet(s.id)}
                                        ></sl-icon-button>
                                    </sl-tooltip>
                                    <sl-tooltip content="Edit">
                                        <sl-icon-button 
                                            name="pencil" 
                                            class="action-btn"
                                            onclick=${() => actions.editSnippet(s)}
                                        ></sl-icon-button>
                                    </sl-tooltip>
                                    <sl-tooltip content="Delete">
                                        <sl-icon-button 
                                            name="trash" 
                                            class="action-btn danger"
                                            onclick=${() => actions.openConfirm(
        'Delete Expression',
        `Are you sure you want to permanently delete "${s.label}"?`,
        () => actions.deleteSnippet(s.id),
        { variant: 'danger', confirmLabel: 'Delete' }
    )}
                                        ></sl-icon-button>
                                    </sl-tooltip>
                                </div>
                            </sl-menu-item>
                        `)}
                        ${gIdx < gArr.length - 1 ? html`<sl-divider></sl-divider>` : null}
                    `)}
                </sl-menu>
            </div>
        </sl-dropdown>
    `;
};

export const MainCard = ({ state, actions }) => {
    const onInput = (e) => {
        state.input.value = e.target.value;
        if (state.selectedIdx.value) {
            state.selectedIdx.value = '';
        }
    };

    const onSnippetSelect = (e) => {
        const id = e.target.value;
        const snippet = state.snippets.value.find(s => s.id === id);
        if (snippet) {
            actions.loadSnippet(snippet);
            state.selectedIdx.value = id;
        }
    };

    const vars = state.variables.value;

    useEffect(() => {
        const cands = detectVariables(state.input.value, state.knownNames.value, state.variables.value);
        // Only update if changed to avoid loop (though signal setting usually handles check, array ref is always new)
        // Simple check: length and content
        const current = state.suggestions.value;
        const changed = cands.length !== current.length || cands.some((c, i) => c !== current[i]);

        if (changed) {
            state.suggestions.value = cands;
        }
    }, [state.input.value, state.variables.value, state.knownNames.value]);

    return html`
        <sl-card class="main-card">
            <div class="form-section">
                <label class="section-label">
                    <sl-icon src="https://api.iconify.design/lucide/list-plus.svg?color=%23cbd5e1" class="section-icon"></sl-icon>
                    Select an expression
                </label>
                <div class="controls-top">
                    <${ExpressionCombobox} state=${state} actions=${actions} />
                    <sl-button outline class="btn-secondary" onclick=${actions.openDocs}>
                        <sl-icon slot="prefix" name="book"></sl-icon> Docs & Settings
                    </sl-button>
                </div>
            </div>


            <div class="form-section">
                <div class="section-header">
                    <label class="section-label">
                        <sl-icon src="https://api.iconify.design/lucide/terminal.svg?color=%23cbd5e1" class="section-icon"></sl-icon>
                        Mathematical Expression
                    </label>
                    ${!state.selectedIdx.value && state.input.value ? html`
                        <sl-tooltip content="Create new expression" hoist trigger="hover">
                            <sl-button size="small" variant="neutral" outline class="btn-save-snippet btn-secondary" onclick=${() => actions.openSaveSnippet()}>
                                <sl-icon slot="prefix" name="bookmark-star"></sl-icon> Create
                            </sl-button>
                        </sl-tooltip>
                    ` : null}
                </div>
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
                            <sl-button size="small" variant="neutral" outline class="btn-clear-all btn-secondary u-mr-05" onclick=${actions.clearHistory}>
                                <sl-icon slot="prefix" name="trash"></sl-icon> Clear
                            </sl-button>
                            <sl-dropdown hoist>
                                <sl-button slot="trigger" size="small" variant="neutral" outline class="btn-secondary u-mr-05" caret>
                                    <sl-icon slot="prefix" name="download"></sl-icon> Export
                                </sl-button>
                                <sl-menu>
                                    <sl-menu-item onclick=${() => actions.exportHistory('csv')}>Export as CSV</sl-menu-item>
                                    <sl-menu-item onclick=${() => actions.exportHistory('json')}>Export as JSON</sl-menu-item>
                                </sl-menu>
                            </sl-dropdown>
                        </div>
                    </div>
                    <div class="history-list">
                        ${state.history.value.map(item => html`
                            <div class="history-item" onclick=${() => actions.loadHistoryItem(item)}>
                                <div class="history-item-main">
                                    <span class="hist-expr" dangerouslySetInnerHTML=${{ __html: highlightExpression(item.expr, state.knownNames.value, state.variables.value, true) }}></span>
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

    // State reset moved to actions.openDocs used to be here

    // Defer heavy content rendering until animation is stable
    const [contentReady, setContentReady] = useState(false);
    const timerRef = useRef(null);
    const contentTimerRef = useRef(null);

    const categories = useMemo(() => [ALL_CATS, ...new Set(functions.map(f => {
        const c = f.Category || f.category;
        return c ? c.trim().replaceAll(' ', '_') : null;
    }).filter(Boolean))], [functions]);

    const filteredFunctions = useMemo(() => (functions || []).filter(f => {
        const name = (f.Name || f.name || '').trim().toLowerCase();
        const desc = (f.Description || f.description || '').trim().toLowerCase();
        const rawFCat = (f.Category || f.category || '').trim();
        const fCategory = rawFCat.replaceAll(' ', '_');
        const isWildcard = !category || category === ALL_CATS || category === '';
        const matchesCategory = isWildcard || (fCategory === category);
        const matchesQuery = !query || name.includes(query) || desc.includes(query);
        return matchesQuery && matchesCategory;
    }), [functions, query, category]);

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

    const onLibrarySort = (field) => {
        if (state.librarySortBy.value === field) {
            state.librarySortDir.value = state.librarySortDir.value === 'asc' ? 'desc' : 'asc';
        } else {
            state.librarySortBy.value = field;
            state.librarySortDir.value = 'asc';
        }
    };

    const sortedSnippets = useMemo(() => {
        const q = (state.librarySearch.value || '').trim().toLowerCase();
        let list = [...state.snippets.value];
        if (q) {
            list = list.filter(s =>
                (s.label || '').toLowerCase().includes(q) ||
                (s.group || '').toLowerCase().includes(q) ||
                (s.value || '').toLowerCase().includes(q)
            );
        }
        return list.sort((a, b) => {
            if (a.pinned !== b.pinned) return b.pinned ? 1 : -1;
            const field = state.librarySortBy.value;
            const dir = state.librarySortDir.value;
            let v1 = '', v2 = '';
            if (field === 'Name') { v1 = a.label; v2 = b.label; }
            else if (field === 'Expression') { v1 = a.value; v2 = b.value; }
            else if (field === 'Group') { v1 = a.group; v2 = b.group; }
            if (typeof v1 === 'string') { v1 = v1.toLowerCase(); v2 = v2.toLowerCase(); }
            if (v1 < v2) return dir === 'asc' ? -1 : 1;
            if (v1 > v2) return dir === 'asc' ? 1 : -1;
            return 0;
        });
    }, [state.snippets.value, state.librarySortBy.value, state.librarySortDir.value, state.librarySearch.value]);

    const sortedOperators = useMemo(() => [...operators].sort((a, b) => {
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
    }), [operators, state.operatorSortBy.value, state.operatorSortDir.value]);

    const renderSortHeader = (label, field, sortBySignal, sortDirSignal, onSortFn) => {
        const isActive = sortBySignal.value === field;
        const dir = sortDirSignal.value;
        return html`
            <th onclick=${() => onSortFn(field)} class="sortable-header ${isActive ? 'active' : ''}">
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
        actions.saveSettings(state.settings.value);
    };

    const isSettingsDirty = () => {
        const s1 = state.settings.value;
        const s2 = state.settingsOriginal.value;
        return JSON.stringify(s1) !== JSON.stringify(s2);
    };

    const onInputChange = (e) => {
        const { name, value } = e.target;
        const finalValue = name === 'historyLength' ? (parseInt(value) || 0) : value;
        state.settings.value = { ...state.settings.value, [name]: finalValue };
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
            onsl-show=${(e) => {
            // Critical: Prevent tooltips/children from triggering full reload of content!
            if (e.target !== e.currentTarget) return;

            if (timerRef.current) clearTimeout(timerRef.current);
            if (contentTimerRef.current) clearTimeout(contentTimerRef.current);
            setContentReady(false);

            timerRef.current = setTimeout(() => toggleTransparency(true), 100);
            // Render content slightly after transparency triggers to strictly separate workload
            contentTimerRef.current = setTimeout(() => setContentReady(true), 150);
        }}
            onsl-request-close=${(e) => {
            // Ensure only dialog-level close requests are honored
            if (e.target !== e.currentTarget) return;
        }}
            onsl-hide=${(e) => {
            if (e.target !== e.currentTarget) return;
            if (timerRef.current) clearTimeout(timerRef.current);
            toggleTransparency(false, 'docs');
        }}
            onsl-after-hide=${(e) => {
            // Ensure events from children (like sl-select) don't close the dialog
            if (e.target !== e.currentTarget) return;
            state.docsOpen.value = false;
            state.showScrollTop.value = false;
        }}>
            <div slot="label" class="u-flex u-items-center u-gap-075">
                <sl-icon src="https://api.iconify.design/lucide/book-open.svg?color=%23cbd5e1" class="doc-header-icon"></sl-icon>
                Docs & Settings
            </div>
            <sl-tab-group onsl-tab-show=${(e) => {
            state.showScrollTop.value = false;
            state.docActiveTab.value = e.detail.name;
        }}>
                <sl-tab slot="nav" panel="funcs">
                    <sl-icon src="https://api.iconify.design/lucide/variable.svg?color=currentColor" class="tab-icon"></sl-icon> Functions
                </sl-tab>
                <sl-tab slot="nav" panel="ops">
                    <sl-icon src="https://api.iconify.design/lucide/percent.svg?color=currentColor" class="tab-icon"></sl-icon> Operators
                </sl-tab>
                <sl-tab slot="nav" panel="library">
                    <sl-icon src="https://api.iconify.design/lucide/bookmark.svg?color=currentColor" class="tab-icon"></sl-icon> Expression Library
                </sl-tab>
                <sl-tab slot="nav" panel="settings">
                    <sl-icon src="https://api.iconify.design/lucide/settings-2.svg?color=currentColor" class="tab-icon"></sl-icon> Settings
                </sl-tab>
                <sl-tab slot="nav" panel="about">
                    <sl-icon src="https://api.iconify.design/lucide/info.svg?color=currentColor" class="tab-icon"></sl-icon> About
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
                        <sl-select value=${category} onsl-change=${onCategory} clearable=${category !== ALL_CATS} hoist class="u-flex-1 function-cat-select">
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
                    ${state.docActiveTab.value === 'funcs' && contentReady ? html`
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
                                        <sl-tooltip content="Copy Name" hoist>
                                            <span class="doc-name" onclick=${() => actions.copyToClipboard(name)}>${name}</span>
                                        </sl-tooltip>
                                        <span class="doc-args">(${args.join(', ') || ''})</span>
                                        <sl-badge variant="primary" size="small" outline class="doc-category">
                                            <sl-icon src="${getCategoryIconUrl(fCat)}" class="cat-icon"></sl-icon> ${fCat}
                                        </sl-badge>
                                    </div>
                                    <div class="doc-desc">${description}</div>
                                    ${examples.length > 0 ? html`
                                        <div class="doc-examples">
                                            ${examples.map(ex => html`
                                                <sl-tooltip content="Use Function" hoist>
                                                    <sl-button size="small" outline class="btn-example" onclick=${() => actions.insertExample(ex)}>
                                                        ${ex}
                                                    </sl-button>
                                                </sl-tooltip>
                                            `)}
                                        </div>
                                    ` : null}
                                </div>
                            `;
        })}
                            ${filteredFunctions.length === 0 ? html`<div class="docs-empty">No functions found matching your criteria.</div>` : null}
                        </div>
                    ` : null}
                </sl-tab-panel>

                <sl-tab-panel name="ops" onscroll=${onPanelScroll}>
                    ${state.docActiveTab.value === 'ops' && contentReady ? html`
                        <div class="ops-table-container">
                            <table class="ops-table">
                                <thead>
                                    <tr>
                                        ${renderSortHeader('Symbol', 'Symbol', state.operatorSortBy, state.operatorSortDir, onSort)}
                                        ${renderSortHeader('Name', 'Name', state.operatorSortBy, state.operatorSortDir, onSort)}
                                        ${renderSortHeader('Category', 'Category', state.operatorSortBy, state.operatorSortDir, onSort)}
                                        ${renderSortHeader('Precedence', 'Precedence', state.operatorSortBy, state.operatorSortDir, onSort)}
                                        ${renderSortHeader('Assoc', 'Associativity', state.operatorSortBy, state.operatorSortDir, onSort)}
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
                                                        <sl-tooltip content="Copy Symbol" hoist>
                                                            <code class="op-symbol" onclick=${() => actions.copyToClipboard(symbol)}>${symbol}</code>
                                                        </sl-tooltip>
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
                    ` : null}
                </sl-tab-panel>

                <sl-tab-panel name="settings">
                    <div class="settings-flex-wrapper">
                        <div class="settings-content-area" onscroll=${onPanelScroll}>
                            <form id="settings-form" class="settings-form" onsubmit=${onSaveSettings}>
                                <div class="form-group">
                                    <label>
                                        <sl-icon src="https://api.iconify.design/lucide/palette.svg?color=%23cbd5e1" class="setting-icon"></sl-icon>
                                        Appearance Theme
                                    </label>
                                    <sl-select name="theme" value=${state.settings.value.theme} onsl-change=${onInputChange} hoist>
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

                                <div class="form-group">
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
                                        <div class="form-group u-mb-2">
                                            <sl-input name="historyLength" type="number" min="1" max="100" value=${state.settings.value.historyLength} onsl-input=${onInputChange}>
                                                <div slot="help-text" class="subtle-help">Maximum number of calculations to keep in history memory.</div>
                                                <sl-button slot="prefix" variant="text" class="btn-stepper"
                                                    onclick=${(e) => { e.preventDefault(); state.settings.value = { ...state.settings.value, historyLength: Math.max(1, parseInt(state.settings.value.historyLength) - 1) }; }}>
                                                    <sl-icon name="dash-lg"></sl-icon>
                                                </sl-button>
                                                <sl-button slot="suffix" variant="text" class="btn-stepper"
                                                    onclick=${(e) => { e.preventDefault(); state.settings.value = { ...state.settings.value, historyLength: Math.min(100, parseInt(state.settings.value.historyLength) + 1) }; }}>
                                                    <sl-icon name="plus-lg"></sl-icon>
                                                </sl-button>
                                            </sl-input>
                                        </div>
                                    ` : html`
                                        <div class="form-group u-mb-2" style="opacity: 0.5; pointer-events: none;">
                                            <sl-input name="historyLength" type="number" value=${state.settings.value.historyLength} disabled>
                                                <div slot="help-text" class="subtle-help">Enable history recording to adjust record limit.</div>
                                                <sl-button slot="prefix" variant="text" class="btn-stepper" disabled>
                                                    <sl-icon name="dash-lg"></sl-icon>
                                                </sl-button>
                                                <sl-button slot="suffix" variant="text" class="btn-stepper" disabled>
                                                    <sl-icon name="plus-lg"></sl-icon>
                                                </sl-button>
                                            </sl-input>
                                        </div>
                                    `}
                                </div>
                            </form>
                        </div>
                        <div class="settings-button-footer">
                            <sl-button variant="default" outline class="btn-cancel-settings btn-cancel" onclick=${actions.cancelSettings} disabled=${!isSettingsDirty()}>
                                Cancel
                            </sl-button>
                            <sl-button variant="primary" type="submit" form="settings-form" class="btn-save-settings" disabled=${!isSettingsDirty()}>
                                Save Settings
                            </sl-button>
                        </div>
                    </div>
                </sl-tab-panel>

                <sl-tab-panel name="about">
                    <div class="about-flex-wrapper">
                        <div class="about-content-area" onscroll=${onPanelScroll}>
                            <div class="about-container">
                        <section class="about-section">
                            <div class="about-header">
                                <sl-icon src="https://api.iconify.design/lucide/cpu.svg?color=%23cbd5e1" class="about-icon"></sl-icon>
                                <div>
                                    <strong>The Product.</strong>
                                    <span>ShYCalculator is a high-performance .NET expression evaluator (Recursive Shunting Yard) designed for zero-allocation parsing and lightning-fast execution. It achieves peak throughput of <strong>~380,000 ops/sec</strong> in single-threaded compiled batch mode (Benchmarked on AMD Ryzen™ 7 5800X3D). It supports complex mathematical functions, string manipulation, date-time operations, and custom variable injection with 100% logic and branch coverage.</span>
                                </div>
                            </div>
                        </section>

                        <section class="about-section">
                            <div class="about-alert">
                                <sl-icon src="https://api.iconify.design/lucide/alert-triangle.svg?color=%23f59e0b" class="alert-icon"></sl-icon>
                                <div>
                                    <div class="u-mb-1">
                                        <strong>The Demo App.</strong>
                                        <span>This is a browser-native playground built to test the <strong>WebAssembly (WASM)</strong> implementation of ShYCalculator. By running the engine directly in your client, it eliminates calculation latency and server roundtrips, though the initial WebAssembly module load may require a moment to initialize.</span>
                                    </div>
                                    <div>
                                        <strong>Sandbox Mode.</strong>
                                        <span>This diagnostic environment is intended for feature exploration and engine verification. It is not designed for production-grade security or permanent data processing.</span>
                                    </div>
                                </div>
                            </div>
                        </section>

                        <section class="about-section">
                            <div class="about-header">
                                <sl-icon src="https://api.iconify.design/lucide/scroll.svg?color=%23cbd5e1" class="about-icon"></sl-icon>
                                <div>
                                    <a href="https://github.com/nullzerovibe/ShYCalculator/blob/main/VIBE.md" target="_blank" class="about-link u-mb-025">Licensing & The Vibe. </a>
                                    <span>Built with passion and released under the <a href="https://github.com/nullzerovibe/ShYCalculator/blob/main/LICENSE" target="_blank" class="about-link">MIT License</a>. Open-source is the way—so please <a href="https://github.com/nullzerovibe/ShYCalculator" target="_blank" class="about-link">fork</a> and enjoy! ;) Feel free to contribute or bake the <a href="https://www.nuget.org/packages/ShYCalculator" target="_blank" class="about-link">NuGet package</a> into your own high-performance projects. Let's build something fast!</span>
                                </div>
                            </div>
                        </section>

                        <section class="about-section">
                            <div class="vibe-card">
                                <pre class="vibe-text"> _  _         _  _  ____                 _   _  _  _      
| \\| | _  _  | || ||_  / ___  _ _  ___  | | | |(_)| |__  ___
| .  || || | | || | / / / -_)| '_|/ _ \\ | |_| || || '_ \\/ -_)
|_|\\_| \\_,_| |_||_|/___|\\___||_|  \\___/  \\___/ |_||_.__/\\___|
 
 
                      ▄▀▀▄░█▀▀█░█▀▀▄░█▀▀ 
                      █░░░░█░░█░█░░█░█▀▀ 
                      ▀▄▄▀░█▄▄█░█▄▄▀░█▄▄ 
 
     ░▒▒▓▓ LIFETIME OF SYNTAX // AGENTIC EVOLUTION ▓▓▒▒░ 
 </pre>
                                <div class="vibe-description">
                                    VIBE CHECK: This code was orchestrated through intent.<br/>
                                    You are free to use, modify, and distribute it.<br/>
                                    Keep the legacy alive. Keep the vibe open.
                                </div>
                            </div>
                        </section>
                    </div>
                </div>

                <div class="about-footer">
                    <sl-button variant="neutral" outline size="small" class="brand-github" href="https://github.com/nullzerovibe/ShYCalculator" target="_blank">
                        <sl-icon slot="prefix" src="https://api.iconify.design/simple-icons/github.svg?color=%23ffffff"></sl-icon> GitHub
                    </sl-button>
                    
                    <sl-button variant="neutral" outline size="small" class="brand-nuget" href="https://www.nuget.org/packages/ShYCalculator" target="_blank">
                        <sl-icon slot="prefix" src="https://api.iconify.design/simple-icons/nuget.svg?color=%23004880"></sl-icon> NuGet
                    </sl-button>
                    
                    <sl-button variant="neutral" outline size="small" class="brand-license" href="https://github.com/nullzerovibe/ShYCalculator/blob/main/LICENSE" target="_blank">
                        <sl-icon slot="prefix" src="https://api.iconify.design/lucide/scale.svg?color=%23f59e0b"></sl-icon> MIT License
                    </sl-button>
                    
                    <sl-button variant="neutral" outline size="small" class="brand-vibe" href="https://github.com/nullzerovibe/ShYCalculator/blob/main/VIBE.md" target="_blank">
                        <sl-icon slot="prefix" src="https://api.iconify.design/lucide/sparkles.svg?color=%23ec4899"></sl-icon> The Vibe
                    </sl-button>
                    
                    <sl-button variant="neutral" outline size="small" class="brand-x" href="https://x.com/nullzerovibe" target="_blank">
                        <sl-icon slot="prefix" src="https://api.iconify.design/simple-icons/x.svg?color=%23ffffff"></sl-icon> Follow
                    </sl-button>
                    
                    <sl-button variant="neutral" outline size="small" class="brand-email" href="mailto:nullzerovibe@gmail.com">
                        <sl-icon slot="prefix" src="https://api.iconify.design/lucide/mail.svg?color=%23ea4335"></sl-icon> Contact
                    </sl-button>
                </div>
            </div>
        </sl-tab-panel>

                <sl-tab-panel name="library">
                    ${state.docActiveTab.value === 'library' && contentReady ? html`
                        <div class="docs-header is-static">
                            <sl-input 
                                placeholder="Search library (Name, Group, Formula)..." 
                                class="u-flex-2"
                                value=${state.librarySearch.value}
                                oninput=${(e) => state.librarySearch.value = e.target.value}
                                clearable
                                onsl-clear=${() => state.librarySearch.value = ''}
                                autocomplete="off" autocorrect="off" autocapitalize="off" spellcheck="false"
                            >
                                <sl-icon name="search" slot="prefix"></sl-icon>
                            </sl-input>
                            <div class="u-flex u-gap-05" style="align-self: flex-start; flex-shrink: 0;">
                                <sl-button variant="neutral" outline class="doc-action-btn" onclick=${() => document.getElementById('file-import-lib').click()}>
                                    <sl-icon slot="prefix" src="https://api.iconify.design/lucide/upload.svg?color=%2338bdf8"></sl-icon> Import
                                </sl-button>
                                <input type="file" id="file-import-lib" accept=".json" hidden onchange=${(e) => {
                actions.importSnippets(e.target.files[0]);
                e.target.value = '';
            }} />
                                <sl-button variant="neutral" outline class="doc-action-btn" onclick=${actions.exportSnippets}>
                                    <sl-icon slot="prefix" src="https://api.iconify.design/lucide/download.svg?color=%2310b981"></sl-icon> Export
                                </sl-button>
                            </div>
                        </div>
                        <div class="ops-table-container">
                            <table class="ops-table library-table">
                                <thead>
                                    <tr>
                                        <th style="width: 40px;">Pin</th>
                                        ${renderSortHeader('Name', 'Name', state.librarySortBy, state.librarySortDir, onLibrarySort)}
                                        ${renderSortHeader('Group', 'Group', state.librarySortBy, state.librarySortDir, onLibrarySort)}
                                        ${renderSortHeader('Expression', 'Expression', state.librarySortBy, state.librarySortDir, onLibrarySort)}
                                        <th style="width: 80px;">Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${sortedSnippets.map(s => html`
                                        <tr class="${s.pinned ? 'pinned-row' : ''}">
                                            <td>
                                                <sl-icon-button 
                                                    name=${s.pinned ? 'pin-fill' : 'pin'} 
                                                    class="pin-btn ${s.pinned ? 'active' : ''}" 
                                                    onclick=${() => actions.togglePinSnippet(s.id)}>
                                                </sl-icon-button>
                                            </td>
                                            <td style="font-weight: 600;">${s.label}</td>
                                            <td>
                                                <sl-badge size="small" class="shy-badge op-category">
                                                    <sl-icon src="${getCategoryIconUrl(s.icon)}" class="cat-icon"></sl-icon> ${s.group}
                                                </sl-badge>
                                            </td>
                                            <td>
                                                <sl-tooltip content="Copy Expression" hoist>
                                                    <code class="op-symbol" onclick=${() => actions.copyToClipboard(s.value)} dangerouslySetInnerHTML=${{ __html: highlightExpression(s.value, state.knownNames.value, state.variables.value, true) }}></code>
                                                </sl-tooltip>
                                            </td>
                                            <td>
                                                <div class="u-flex u-gap-025">
                                                    <sl-icon-button name="pencil" onclick=${() => actions.editSnippet(s)}></sl-icon-button>
                                                    <sl-icon-button name="trash" class="danger-icon" onclick=${() => {
                    actions.openConfirm(
                        "Delete Expression",
                        `Are you sure you want to delete "${s.label}"? This action cannot be undone.`,
                        () => actions.deleteSnippet(s.id),
                        { variant: 'danger', confirmLabel: 'Delete' }
                    );
                }}></sl-icon-button>
                                                </div>
                                            </td>
                                        </tr>
                                    `)}
                                    ${state.snippets.value.length === 0 ? html`
                                        <tr>
                                            <td colspan="5" class="u-text-center u-p-3" style="opacity: 0.5;">
                                                Your expression library is empty. Save an expression to see it here!
                                            </td>
                                        </tr>
                                    ` : null}
                                </tbody>
                            </table>
                        </div>
                    ` : null}
                </sl-tab-panel>
            </sl-tab-group>

            <sl-button circle class="scroll-top-btn ${state.showScrollTop.value ? 'visible' : ''}" onclick=${scrollToTop}>
                <sl-icon name="chevron-up"></sl-icon>
            </sl-button>
        </sl-dialog>
    `;
};

export const ConfirmDialog = ({ state, actions }) => {
    const dialog = state.confirmDialog;

    const onHide = () => {
        actions.closeConfirm();
    };

    const handleConfirm = () => {
        if (dialog.onConfirm.value) {
            dialog.onConfirm.value();
        }
        actions.closeConfirm();
    };

    return html`
        <sl-dialog 
            label=${dialog.title.value} 
            open=${dialog.open.value} 
            class="confirm-dialog"
            onsl-after-hide=${onHide}
        >
            <div class="confirm-content">
                ${dialog.message.value}
            </div>
            <sl-button slot="footer" variant="text" class="btn-cancel" onclick=${() => actions.closeConfirm()}>
                ${dialog.cancelLabel.value}
            </sl-button>
            <sl-button slot="footer" variant=${dialog.variant.value} onclick=${handleConfirm} autofocus>
                ${dialog.confirmLabel.value}
            </sl-button>
        </sl-dialog>
    `;
};


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
    <div class="status-indicator">
        ${state.isOfflineReady?.value ? html`
                <sl-tooltip content="Offline Ready — Application is fully cached and available for standalone use without an internet connection." hoist>
                    <sl-icon name="cloud-check" class="pwa-status"></sl-icon>
                </sl-tooltip>
            ` : null
        }
<sl-tooltip content="${state.isReady?.value ? 'Core Engine Active — High-performance .NET WASM runtime is initialized and ready for calculations.' : 'Engine Initializing...'}" hoist>
    <sl-icon name="cpu" class="engine-status ${state.isReady?.value ? '' : 'is-loading'}"></sl-icon>
</sl-tooltip>
        </div>
        <div class="app-container single-column">
            <${Header} state=${state} actions=${actions} />
            <${MainCard} state=${state} actions=${actions} />
        </div>
        <${Documentation} state=${state} actions=${actions} />
        <${SaveSnippetDialog} state=${state} actions=${actions} />
        <${ConfirmDialog} state=${state} actions=${actions} />
`;
};

// --- RENDER ---
render(html`<${App} state=${appState} actions=${actions} />`, document.getElementById('app'));
