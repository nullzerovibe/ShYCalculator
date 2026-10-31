/* One dialog for everything that is not the workbench: reference, library management, shortcuts, settings, about. */
import {
    html, cx, Icon, Dialog, Tabs, TabPanel, Table, Combobox, Row, Switch, Stepper, ThemePicker, ShortcutList, About,
    copy, confirm, resetApp, worker, installable, install, useState, useMemo, fuzzy,
} from '../nzv/nzv.js';
import { Code } from '../editor.js';
import { iconFor, DATE_FORMATS, CULTURES, REPO, NUGET } from '../data.js';
import {
    ui, docs, settings, snippets, load, openSnippet, togglePin, deleteSnippet, importSnippets, exportSnippets, appVersion, clearStore,
} from '../state.js';
import { insertCall } from './workbench.js';

export const TABS = [
    { id: 'functions', label: 'Functions', icon: 'square-function' },
    { id: 'operators', label: 'Operators', icon: 'percent' },
    { id: 'library', label: 'Library', icon: 'bookmark' },
    { id: 'shortcuts', label: 'Shortcuts', icon: 'keyboard' },
    { id: 'settings', label: 'Settings', icon: 'settings-2' },
    { id: 'about', label: 'About', icon: 'info' },
];

const close = () => { ui.help.value = null; };

const Search = ({ value, onInput, label, placeholder }) => html`
    <div class="nzv-input-group u-grow">
        <${Icon} name="search" />
        <input class="nzv-input" type="search" value=${value} placeholder=${placeholder} aria-label=${label}
            autocomplete="off" spellcheck="false" onInput=${(e) => onInput(e.currentTarget.value)} />
    </div>`;

const Functions = () => {
    const [q, setQ] = useState('');
    const [cat, setCat] = useState('all');
    const fns = docs.value.functions;
    const cats = useMemo(() => [
        { value: 'all', label: 'All categories', icon: 'layout-grid' },
        ...[...new Set(fns.map((f) => f.category))].sort().map((c) => ({ value: c, label: c, icon: iconFor(c) })),
    ], [fns]);
    const shown = fuzzy(fns.filter((f) => cat === 'all' || f.category === cat), q, (f) => `${f.name} ${f.description}`);
    return html`
        <div class="u-col u-gap-4">
            <div class="shy-toolbar">
                <${Search} value=${q} onInput=${setQ} label="Search functions" placeholder=${`Search ${fns.length} functions…`} />
                <${Combobox} class="shy-toolbar__combo" label="Category" value=${cat} options=${cats} onChange=${setCat} searchable=${false} />
            </div>
            ${!fns.length && html`<div class="nzv-empty"><${Icon} name="loader" />Loading the function catalogue…</div>`}
            ${fns.length > 0 && !shown.length && html`<div class="nzv-empty"><${Icon} name="search" />No function matches “${q}”</div>`}
            <div class="shy-fns">
                ${shown.map((f) => html`
                    <article class="shy-fn" key=${f.name}>
                        <div class="shy-fn__head">
                            <button type="button" class="shy-fn__name" onClick=${() => { close(); insertCall(f.name); }}
                                aria-label=${`Insert ${f.name} into the editor`} data-tip="Insert into editor">
                                <span class="hl-fn">${f.name}</span><span class="shy-fn__sig">(${f.args.join(', ')})</span>
                            </button>
                            <span class="nzv-chip shy-fn__cat"><${Icon} name=${iconFor(f.category)} />${f.category}</span>
                        </div>
                        ${f.description && html`<p class="u-small u-dim">${f.description}</p>`}
                        ${f.examples.length > 0 && html`
                            <div class="shy-fn__ex">
                                ${f.examples.map((ex) => html`
                                    <button type="button" class="shy-code" aria-label=${`Run example ${ex}`} onClick=${() => { close(); load({ value: ex }); }}>
                                        <${Icon} name="arrow-right" /><span><${Code} src=${ex} /></span>
                                    </button>`)}
                            </div>`}
                    </article>`)}
            </div>
        </div>`;
};

const Operators = () => {
    const [sort, setSort] = useState({ key: 'precedence', dir: 'desc' });
    return html`
        <${Table} caption="Operators" rows=${docs.value.operators} sort=${sort} onSort=${setSort} rowKey=${(r) => r.symbol}
            columns=${[
                { key: 'symbol', label: 'Symbol', sortable: true, render: (r) => html`
                    <button type="button" class="shy-code shy-code--op" aria-label=${`Copy operator ${r.symbol}`} onClick=${() => copy(r.symbol, `Copied ${r.symbol}`)}>
                        <span class="hl-op">${r.symbol}</span></button>` },
                { key: 'name', label: 'Name', sortable: true },
                { key: 'category', label: 'Category', sortable: true, render: (r) => html`<span class="nzv-chip"><${Icon} name=${iconFor(r.category)} />${r.category}</span>` },
                { key: 'precedence', label: 'Prec.', sortable: true, class: 'u-mono' },
                { key: 'associativity', label: 'Assoc.', sortable: true, render: (r) => (r.associativity === 'Right' ? '→ Right' : '← Left') },
            ]} />`;
};

const Library = () => {
    const [q, setQ] = useState('');
    const list = fuzzy(snippets.value, q, (s) => `${s.label} ${s.group} ${s.value}`)
        .sort((a, b) => (q ? 0 : (b.pinned - a.pinned)));
    return html`
        <div class="u-col u-gap-4">
            <div class="shy-toolbar">
                <${Search} value=${q} onInput=${setQ} label="Search library" placeholder=${`Search ${snippets.value.length} expressions…`} />
                <div class="u-row u-gap-2">
                    <button type="button" class="nzv-btn" onClick=${importSnippets}><${Icon} name="upload" />Import</button>
                    <button type="button" class="nzv-btn" onClick=${exportSnippets}><${Icon} name="download" />Export</button>
                    <button type="button" class="nzv-btn nzv-btn--primary" onClick=${() => openSnippet()}><${Icon} name="plus" />New</button>
                </div>
            </div>
            ${!list.length && html`<div class="nzv-empty"><${Icon} name="bookmark" />${q ? `Nothing matches “${q}”` : 'Your library is empty — save an expression to see it here'}</div>`}
            <ul class="shy-snips">
                ${list.map((s) => html`
                    <li class=${cx('shy-snip', s.pinned && 'is-pinned')} key=${s.id}>
                        <span class="shy-snip__icon"><${Icon} name=${s.icon} /></span>
                        <button type="button" class="shy-snip__main" onClick=${() => { close(); load(s); }} aria-label=${`Load ${s.label}`}>
                            <span class="shy-snip__label">${s.label}<span class="nzv-chip">${s.group}</span></span>
                            <code class="shy-snip__code"><${Code} src=${s.value} /></code>
                        </button>
                        <div class="shy-snip__tools">
                            <button type="button" class=${cx('nzv-btn nzv-btn--ghost nzv-btn--icon nzv-btn--sm shy-pin', s.pinned && 'is-on')} aria-pressed=${s.pinned}
                                aria-label=${s.pinned ? `Unpin ${s.label}` : `Pin ${s.label}`} onClick=${() => togglePin(s.id)}><${Icon} name=${s.pinned ? 'pin-off' : 'pin'} /></button>
                            <button type="button" class="nzv-btn nzv-btn--ghost nzv-btn--icon nzv-btn--sm" aria-label=${`Edit ${s.label}`} onClick=${() => openSnippet(s)}><${Icon} name="pencil" /></button>
                            <button type="button" class="nzv-btn nzv-btn--ghost nzv-btn--icon nzv-btn--sm shy-danger" aria-label=${`Delete ${s.label}`} onClick=${() => deleteSnippet(s)}><${Icon} name="trash-2" /></button>
                        </div>
                    </li>`)}
            </ul>
        </div>`;
};

const Presets = ({ items, value, onPick, label }) => html`
    <div class="shy-presets" role="group" aria-label=${label}>
        ${items.map((it) => html`
            <button type="button" class=${cx('nzv-chip', it === value && 'nzv-chip--accent')} aria-pressed=${it === value} onClick=${() => onPick(it)}>${it}</button>`)}
    </div>`;

const set = (patch) => { settings.value = { ...settings.value, ...patch }; };

const Settings = () => {
    const s = settings.value;
    const standalone = matchMedia('(display-mode: standalone)').matches;
    const reset = async () => {
        if (await confirm({ title: 'Reset ShYCalculator?', message: 'Deletes history, library and settings on this device, removes the offline copy and reloads.', danger: true, confirmLabel: 'Reset everything' })) {
            resetApp({ clearStorage: clearStore });
        }
    };
    const dropOffline = async () => {
        if (await confirm({ title: 'Remove offline copy?', message: 'Unregisters the service worker and clears cached files. Your history and library stay. To remove an installed app icon, use your browser or OS uninstall.', confirmLabel: 'Remove' })) {
            resetApp();
        }
    };
    return html`
        <div class="u-col u-gap-5">
            <section class="u-col u-gap-2">
                <h3 class="nzv-eyebrow">Appearance</h3>
                <div><${Row} title="Theme" desc="Shared by every NullZeroVibe app on this device."><${ThemePicker} /><//></div>
            </section>
            <section class="u-col u-gap-4">
                <h3 class="nzv-eyebrow">Dates</h3>
                <label class="nzv-field">
                    <span class="nzv-label"><${Icon} name="calendar-clock" />Date format</span>
                    <input class="nzv-input nzv-input--mono" value=${s.dateFormat} spellcheck="false" autocomplete="off"
                        onChange=${(e) => e.currentTarget.value.trim() && set({ dateFormat: e.currentTarget.value.trim() })} />
                    <span class="nzv-hint">How the engine parses and prints dates, e.g. dd/MM/yyyy.</span>
                </label>
                <${Presets} label="Date format presets" items=${DATE_FORMATS} value=${s.dateFormat} onPick=${(v) => set({ dateFormat: v })} />
                <label class="nzv-field">
                    <span class="nzv-label"><${Icon} name="languages" />Culture</span>
                    <input class="nzv-input nzv-input--mono" value=${s.culture} spellcheck="false" autocomplete="off"
                        onChange=${(e) => e.currentTarget.value.trim() && set({ culture: e.currentTarget.value.trim() })} />
                    <span class="nzv-hint">Month and day names, e.g. de-DE or fr-FR.</span>
                </label>
                <${Presets} label="Culture presets" items=${CULTURES} value=${s.culture} onPick=${(v) => set({ culture: v })} />
            </section>
            <section class="u-col u-gap-2">
                <h3 class="nzv-eyebrow">History</h3>
                <div>
                    <${Row} title="Record history" desc="Keep successful calculations on this device.">
                        <${Switch} checked=${s.enableHistory} onChange=${(v) => set({ enableHistory: v })} /><//>
                    ${s.enableHistory && html`
                        <${Row} title="History size" desc="Oldest entries drop off beyond this.">
                            <${Stepper} label="History size" value=${s.historyLength} min=${1} max=${100} onChange=${(v) => set({ historyLength: v })} /><//>`}
                </div>
            </section>
            <section class="u-col u-gap-2">
                <h3 class="nzv-eyebrow">App</h3>
                <div>
                    <${Row} title="Offline copy" desc=${worker.value === 'none' ? 'Not cached yet — it is stored after the first visit.' : 'Cached — works without a connection.'}>
                        ${installable.value
                            ? html`<button type="button" class="nzv-btn nzv-btn--sm" onClick=${install}><${Icon} name="download" />Install</button>`
                            : html`<span class=${cx('nzv-chip', worker.value !== 'none' && 'nzv-chip--ok')}><${Icon} name=${worker.value !== 'none' ? 'cloud-check' : 'cloud-off'} />${standalone ? 'Installed' : worker.value !== 'none' ? 'Ready' : 'Online only'}</span>`}
                    <//>
                </div>
            </section>
            <section class="shy-danger-zone u-col u-gap-3" aria-labelledby="danger-h">
                <h3 class="nzv-eyebrow" id="danger-h"><${Icon} name="alert-triangle" /> Danger zone</h3>
                <div class="u-row u-wrap">
                    <button type="button" class="nzv-btn" onClick=${dropOffline}><${Icon} name="cloud-off" />Remove offline copy</button>
                    <button type="button" class="nzv-btn nzv-btn--danger" onClick=${reset}><${Icon} name="trash-2" />Reset everything</button>
                </div>
            </section>
        </div>`;
};

const AboutTab = () => html`
    <${About} app="ShYCalculator" version=${appVersion.value}
        description="A high-performance .NET expression evaluator — maths, logic, strings, dates and variables — running entirely in your browser on WebAssembly."
        links=${[
            { label: 'Source', href: REPO, icon: 'github' },
            { label: 'NuGet', href: NUGET, icon: 'nuget' },
            { label: 'MIT License', href: `${REPO}/blob/main/LICENSE`, icon: 'scale' },
            { label: 'The Vibe', href: `${REPO}/blob/main/VIBE.md`, icon: 'sparkles' },
            { label: 'Follow', href: 'https://x.com/nullzerovibe', icon: 'x-brand' },
        ]}>
        <p class="u-small u-dim">Recursive shunting-yard parsing with zero-allocation hot paths — around <strong>380,000 ops/sec</strong> single-threaded in compiled batch mode (Ryzen 7 5800X3D), with full logic and branch coverage.</p>
        <p class="u-small u-muted">This playground runs the engine client-side: no server round trips, nothing leaves your device. It is a sandbox for exploring features, not a production data processor.</p>
    <//>`;

const PANELS = { functions: Functions, operators: Operators, library: Library, shortcuts: ShortcutList, settings: Settings, about: AboutTab };

export const HelpDialog = () => {
    const tab = ui.help.value;
    const [last, setLast] = useState('functions');
    const current = tab ?? last;
    const Panel = PANELS[current];
    return html`
        <${Dialog} open=${!!tab} onClose=${() => { setLast(current); close(); }} title="Reference & settings" icon="book-open" class="shy-help">
            <div class="shy-help__tabs">
                <${Tabs} label="Sections" idPrefix="help" tabs=${TABS} value=${current} onChange=${(id) => { ui.help.value = id; }} />
            </div>
            <${TabPanel} id=${current} idPrefix="help"><${Panel} /><//>
        <//>`;
};
