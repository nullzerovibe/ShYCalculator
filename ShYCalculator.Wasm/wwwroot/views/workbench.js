/* The hero card (library picker, editor, variables, run) and the readout under it. */
import { html, cx, Icon, Combobox, Menu, Readout, Kbd, engine, useMemo } from '../nzv/nzv.js';
import { Editor, VarGrid, Code } from '../editor.js';
import { TYPE_ICON } from '../data.js';
import {
    expr, setExpr, output, check, busy, showAst, toggleAst, calculate, snippets, selectedSnippet, load, togglePin, openSnippet,
    copyResult, copyEquation, copyJson, shareResult, clearExpr, vars,
} from '../state.js';

export const editorRef = { current: null };

// WHY: a closing <dialog> restores focus to its opener; wait until none is open or that would steal the caret back
const afterDialogs = (fn) => {
    const tick = () => (document.querySelector('dialog[open]') ? requestAnimationFrame(tick) : fn());
    requestAnimationFrame(tick);
};

export const focusEditor = () => afterDialogs(() => {
    const el = editorRef.current;
    if (!el) return;
    el.focus();
    el.setSelectionRange(el.value.length, el.value.length);
});

/** Inserts `name()` at the caret (replacing any selection) and leaves the caret between the parentheses. */
export const insertCall = (name) => {
    const el = editorRef.current;
    const src = expr.value;
    const start = el?.selectionStart ?? src.length;
    const end = el?.selectionEnd ?? src.length;
    const inner = src.slice(start, end);
    setExpr(`${src.slice(0, start)}${name}(${inner})${src.slice(end)}`);
    afterDialogs(() => {
        if (!el) return;
        el.focus();
        const caret = start + name.length + 1 + inner.length;
        el.setSelectionRange(caret, caret);
    });
};

export const snippetOptions = (list) => {
    const pinned = list.filter((s) => s.pinned).map((s) => ({ ...s, group: 'Pinned' }));
    const rest = list.filter((s) => !s.pinned);
    const groups = [...new Set(rest.map((s) => s.group))];
    return [...pinned, ...groups.flatMap((g) => rest.filter((s) => s.group === g))]
        .map((s) => ({ value: s.id, label: s.label, icon: s.icon, group: s.group, keywords: s.value, pinned: s.pinned }));
};

const Library = () => {
    const options = useMemo(() => snippetOptions(snippets.value), [snippets.value]);
    return html`
        <${Combobox} class="shy-lib" label="Expression library" placeholder="Load from library…" icon="bookmark" searchable=${true}
            value=${selectedSnippet.value} options=${options} emptyText="No saved expression matches"
            onChange=${(id) => { const s = snippets.value.find((x) => x.id === id); if (s) load(s); }}
            renderEnd=${(o) => html`
                <button type="button" class=${cx('nzv-btn nzv-btn--ghost nzv-btn--icon nzv-btn--sm shy-pin', o.pinned && 'is-on')}
                    aria-label=${o.pinned ? `Unpin ${o.label}` : `Pin ${o.label}`} aria-pressed=${o.pinned}
                    onClick=${(e) => { e.stopPropagation(); togglePin(o.value); }}>
                    <${Icon} name=${o.pinned ? 'pin-off' : 'pin'} />
                </button>`} />`;
};

const Status = () => {
    const c = check.value;
    if (engine.value === 'boot') return html`<span class="shy-status"><span class="shy-dot is-boot"></span>Engine starting…</span>`;
    if (engine.value === 'error') return html`<span class="shy-status is-bad"><span class="shy-dot is-bad"></span>Engine unavailable</span>`;
    if (!expr.value.trim()) return html`<span class="shy-status"><span class="shy-dot"></span>Ready</span>`;
    if (!c) return html`<span class="shy-status"><span class="shy-dot is-boot"></span>Checking…</span>`;
    if (c.ok) return html`<span class="shy-status is-ok"><span class="shy-dot is-ok"></span>Valid syntax</span>`;
    const e = c.errors[0];
    return html`<span class="shy-status is-bad" title=${c.message}><span class="shy-dot is-bad"></span>
        ${e?.message || c.message}${e?.start >= 0 ? ` · col ${e.start + 1}` : ''}</span>`;
};

const Examples = () => html`
    <div class="shy-try">
        <span class="nzv-eyebrow">Try</span>
        ${snippets.value.filter((s) => s.pinned).concat(snippets.value.filter((s) => !s.pinned)).slice(0, 6).map((s) => html`
            <button type="button" class="nzv-chip shy-try__chip" onClick=${() => load(s)}><${Icon} name=${s.icon} />${s.label}</button>`)}
    </div>`;

export const Workbench = () => {
    const errs = check.value?.ok === false ? check.value.errors : [];
    return html`
        <section class="nzv-card nzv-card--hero shy-bench" aria-labelledby="bench-title">
            <div class="shy-bench__head">
                <h1 id="bench-title" class="nzv-eyebrow shy-bench__title"><${Icon} name="terminal" />Expression workbench</h1>
                <div class="shy-bench__tools">
                    <${Library} />
                    <button type="button" class="nzv-btn nzv-btn--icon" aria-label="Save expression to library" data-tip="Save to library"
                        disabled=${!expr.value.trim()} onClick=${() => openSnippet()}><${Icon} name="bookmark-plus" /></button>
                </div>
            </div>

            <div class="shy-frame">
                <span class="shy-frame__prompt" aria-hidden="true">›</span>
                <${Editor} id="expr" inputRef=${editorRef} value=${expr.value} onInput=${setExpr} errors=${errs} describedBy="expr-status" invalid=${errs.length > 0} />
                ${expr.value && html`
                    <button type="button" class="nzv-btn nzv-btn--ghost nzv-btn--icon nzv-btn--sm shy-frame__clear" aria-label="Clear expression" data-tip="Clear (Esc)"
                        onClick=${() => { clearExpr(); editorRef.current?.focus(); }}><${Icon} name="x" /></button>`}
                <div class="shy-frame__bar" id="expr-status" aria-live="polite">
                    <${Status} />
                    <span class="u-hide-sm u-tiny u-muted">Shift+Enter new line</span>
                </div>
            </div>

            ${!expr.value.trim() && html`<${Examples} />`}
            <${VarGrid} />

            <div class="shy-run">
                <button type="button" class="nzv-btn nzv-btn--primary nzv-btn--lg shy-run__btn" disabled=${engine.value !== 'ready' || !expr.value.trim()}
                    aria-busy=${busy.value} onClick=${() => calculate()}>
                    <${Icon} name=${busy.value ? 'loader' : 'zap'} class=${busy.value ? 'shy-spin' : ''} />Calculate
                    <span class="shy-run__keys u-hide-sm"><${Kbd} keys="mod+enter" /></span>
                </button>
            </div>

            <${Result} />
        </section>`;
};

const AstNode = ({ n }) => {
    if (!n) return null;
    const label = n.operator ?? n.name ?? (n.value !== undefined ? JSON.stringify(n.value) : '');
    const kids = [
        ['cond', n.condition], ['then', n.true_branch], ['else', n.false_branch], ['', n.left], ['', n.right],
        ...(n.arguments ?? []).map((a, i) => [`arg ${i + 1}`, a]),
    ].filter(([, c]) => c);
    const ev = n.evaluated_value;
    const evText = ev === null || ev === undefined ? '' : typeof ev === 'object' ? (ev.value ?? ev.Value ?? JSON.stringify(ev)) : String(ev);
    return html`
        <li>
            <div class="shy-ast__node" data-type=${n.type}>
                <span class="shy-ast__type">${n.type}</span>
                ${label !== '' && html`<code class="shy-ast__label">${label}</code>`}
                ${evText !== '' && html`<span class="shy-ast__val">→ ${String(evText)}</span>`}
            </div>
            ${kids.length > 0 && html`<ul>${kids.map(([k, c]) => html`<${AstNode} n=${c} key=${k} />`)}</ul>`}
        </li>`;
};

const Result = () => {
    const o = output.value;
    const ready = engine.value === 'ready';
    const placeholder = engine.value === 'error' ? 'Engine unavailable' : ready ? 'Run an expression to see its value' : 'Warming up the .NET engine…';
    const actions = o?.ok && html`
        <${Menu} label="Copy" icon="copy" items=${[
            { label: 'Copy result', icon: 'copy', hint: 'C', run: copyResult },
            { label: 'Copy equation', icon: 'calculator', hint: 'Shift+C', run: copyEquation },
            { label: 'Copy as JSON', icon: 'braces', run: copyJson },
        ]} />
        <button type="button" class="nzv-btn nzv-btn--ghost nzv-btn--icon" aria-label="Share" data-tip="Share" onClick=${shareResult}><${Icon} name="share-2" /></button>
        <button type="button" class=${cx('nzv-btn nzv-btn--ghost nzv-btn--icon', showAst.value && 'is-pressed')} aria-pressed=${showAst.value}
            aria-label="Syntax tree" data-tip="Syntax tree (A)" onClick=${toggleAst}><${Icon} name="network" /></button>`;
    const meta = o?.ok && html`
        ${o.type && html`<span class="nzv-chip"><${Icon} name=${TYPE_ICON[o.type] ?? 'square-function'} />${o.type}</span>`}
        <span class="nzv-chip nzv-chip--accent" title="Round trip through JS interop"><${Icon} name="timer" />${o.ms < 1 ? o.ms.toFixed(2) : o.ms.toFixed(1)} ms</span>
        ${vars.value.length > 0 && html`<span class="nzv-chip u-hide-sm"><${Icon} name="variable" />${vars.value.length} var${vars.value.length > 1 ? 's' : ''}</span>`}`;
    return html`
        <div class="shy-result">
            <${Readout} label="Result" value=${o?.ok ? o.value : undefined} error=${o && !o.ok ? o.error : undefined}
                placeholder=${placeholder} pingKey=${o?.seq} onCopy=${copyResult} meta=${meta} actions=${actions} />
            ${o && !o.ok && o.errors?.some((e) => e.start >= 0) && !o.expanded && html`
                <pre class="shy-errline" aria-label="Error location"><${Code} src=${expr.value} errors=${o.errors} /></pre>`}
            ${showAst.value && o?.ok && html`
                <section class="shy-ast" aria-label="Syntax tree">
                    <div class="u-row"><span class="nzv-eyebrow"><${Icon} name="network" /> Syntax tree</span>
                        <button type="button" class="nzv-btn nzv-btn--ghost nzv-btn--sm u-end" onClick=${toggleAst}>Hide</button></div>
                    ${o.ast ? html`<ul class="shy-ast__tree"><${AstNode} n=${o.ast} /></ul>` : html`<p class="u-small u-muted">Building tree…</p>`}
                </section>`}
        </div>`;
};
