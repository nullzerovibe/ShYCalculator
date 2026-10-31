/* Create / edit a library expression. */
import { html, cx, Icon, Dialog, Switch, useState, useEffect, useRef } from '../nzv/nzv.js';
import { Editor } from '../editor.js';
import { ICON_CHOICES, groupOfIcon } from '../data.js';
import { ui, snippets, saveSnippet, deleteSnippet, validate } from '../state.js';

const FLAT = ICON_CHOICES.flatMap((c) => c.icons.map((icon) => ({ icon, group: c.group })));

/** WAI-ARIA radio group: one tab stop, arrow keys move and select. */
const IconPicker = ({ value, onChange, invalid }) => {
    const ref = useRef();
    const at = Math.max(0, FLAT.findIndex((f) => f.icon === value));
    const onKey = (e) => {
        const cols = getComputedStyle(ref.current).gridTemplateColumns.split(' ').length;
        const d = { ArrowRight: 1, ArrowLeft: -1, ArrowDown: cols, ArrowUp: -cols }[e.key];
        if (!d) return;
        e.preventDefault();
        const next = FLAT[(at + d + FLAT.length) % FLAT.length];
        onChange(next.icon);
        requestAnimationFrame(() => ref.current?.querySelector('[aria-checked="true"]')?.focus());
    };
    return html`
        <div ref=${ref} class=${cx('shy-icons', invalid && 'is-invalid')} role="radiogroup" aria-label="Icon" aria-describedby="snip-icon-hint" onKeyDown=${onKey}>
            ${FLAT.map((f, i) => html`
                <button type="button" role="radio" class="shy-icons__opt" aria-checked=${f.icon === value} tabindex=${i === at ? 0 : -1}
                    aria-label=${`${f.icon.replace(/-/g, ' ')} (${f.group})`} data-tip=${f.group} onClick=${() => onChange(f.icon)}>
                    <${Icon} name=${f.icon} />
                </button>`)}
        </div>`;
};

export const SnippetDialog = () => {
    const open = ui.snippet.value;
    const [form, setForm] = useState(null);
    const [errors, setErrors] = useState({});
    const [check, setCheck] = useState(null);
    const nameRef = useRef();

    useEffect(() => {
        if (!open) return undefined;
        setForm({ ...open });
        setErrors({});
        setCheck(null);
        // WHY: the kit Dialog mounts its body after showModal() on first open, so `autofocus` misses and the close button wins
        const t = setTimeout(() => nameRef.current?.focus(), 30);
        return () => clearTimeout(t);
    }, [open]);

    useEffect(() => {
        if (!form?.value?.trim()) { setCheck(null); return undefined; }
        const t = setTimeout(() => validate(form.value).then(setCheck).catch(() => setCheck(null)), 250);
        return () => clearTimeout(t);
    }, [form?.value]);

    const f = form ?? { label: '', value: '', icon: '', pinned: false };
    const editing = !!f.id;
    const patch = (p) => {
        setForm({ ...f, ...p });
        setErrors({ ...errors, ...Object.fromEntries(Object.keys(p).map((k) => [k, ''])) });
    };
    const done = () => { ui.snippet.value = null; };

    const submit = (e) => {
        e.preventDefault();
        const label = f.label.trim();
        const next = {
            label: !label ? 'Give the expression a name.'
                : snippets.value.some((s) => s.label.toLowerCase() === label.toLowerCase() && s.id !== f.id) ? `“${label}” already exists.` : '',
            value: f.value.trim() ? '' : 'The expression cannot be empty.',
            icon: f.icon ? '' : 'Pick an icon — it also sets the group.',
        };
        if (Object.values(next).some(Boolean)) {
            setErrors(next);
            return;
        }
        saveSnippet(f);
        done();
    };

    const remove = async () => {
        if (await deleteSnippet(f)) done();
    };

    return html`
        <${Dialog} open=${!!open} onClose=${done} title=${editing ? 'Edit expression' : 'Save to library'} icon="bookmark-plus" class="shy-snip-dialog"
            foot=${html`
                ${editing && html`<button type="button" class="nzv-btn nzv-btn--ghost shy-danger" onClick=${remove}><${Icon} name="trash-2" />Delete</button>`}
                <span class="u-grow"></span>
                <button type="button" class="nzv-btn nzv-btn--ghost" onClick=${done}>Cancel</button>
                <button type="submit" form="snip-form" class="nzv-btn nzv-btn--primary"><${Icon} name="check" />${editing ? 'Update' : 'Save'}</button>`}>
            <form id="snip-form" class="u-col u-gap-5" onSubmit=${submit} novalidate>
                <label class="nzv-field">
                    <span class="nzv-label">Name</span>
                    <input ref=${nameRef} class="nzv-input" value=${f.label} placeholder="e.g. Monthly compound interest" autocomplete="off"
                        aria-invalid=${errors.label ? 'true' : undefined} aria-describedby="snip-name-hint"
                        onInput=${(e) => patch({ label: e.currentTarget.value })} />
                    <span id="snip-name-hint" class=${cx('nzv-hint', errors.label && 'is-error')}>${errors.label || 'A short, descriptive title.'}</span>
                </label>
                <div class="nzv-field">
                    <label class="nzv-label" for="snip-expr">Expression</label>
                    <div class="shy-frame shy-frame--compact">
                        <${Editor} id="snip-expr" compact value=${f.value} onInput=${(v) => patch({ value: v })} label="Expression to save"
                            errors=${check?.ok === false ? check.errors : []} describedBy="snip-expr-hint" invalid=${!!errors.value} placeholder="Expression" />
                    </div>
                    <span id="snip-expr-hint" class=${cx('nzv-hint', (errors.value || check?.ok === false) && 'is-error')} aria-live="polite">
                        ${errors.value || (check ? (check.ok ? html`<span class="shy-ok"><${Icon} name="check-circle" /> Valid syntax — variables are saved with their current values</span>`
                            : check.errors.map((x) => `${x.message}${x.start >= 0 ? ` (col ${x.start + 1})` : ''}`).join(' · ') || check.message) : 'Validated as you type.')}
                    </span>
                </div>
                <div class="nzv-field">
                    <span class="nzv-label">Icon <span class="u-muted">· group: ${f.icon ? groupOfIcon(f.icon) : '—'}</span></span>
                    <${IconPicker} value=${f.icon} invalid=${!!errors.icon} onChange=${(icon) => patch({ icon })} />
                    <span id="snip-icon-hint" class=${cx('nzv-hint', errors.icon && 'is-error')}>${errors.icon || 'Arrow keys move through the grid.'}</span>
                </div>
                <${Switch} checked=${f.pinned} onChange=${(pinned) => patch({ pinned })} label="Pin to the top" desc="Pinned expressions lead the library picker and the Try row." />
            </form>
        <//>`;
};
