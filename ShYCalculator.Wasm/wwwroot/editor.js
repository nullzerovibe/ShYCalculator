/* Highlighted expression editor and the variable grid that grows out of it. */
import { html, cx, Icon, useRef, useId } from './nzv/nzv.js';
import { tokenize, valueType } from './syntax.js';
import { TYPE_ICON } from './data.js';
import { fnNames, vars, setVar } from './state.js';

const overlaps = (k, errors) => errors.some((e) => e.start >= 0 && k.i < e.start + Math.max(1, e.length) && k.i + k.s.length > e.start);

/** Read-only highlighted code (history, library, examples). */
export const Code = ({ src, errors = [] }) => tokenize(src, fnNames.value).map((k) => (k.t === 'ws' || k.t === 'other'
    ? k.s
    : html`<span class=${cx(`hl-${k.t}`, errors.length && overlaps(k, errors) && 'hl-err')}>${k.s}</span>`));

/**
 * Textarea over a highlighted <pre>. Both share one grid cell, so the <pre> sizes the editor and no JS
 * measuring or scroll syncing is needed.
 */
export const Editor = ({ value, onInput, errors = [], label = 'Expression', id, inputRef, placeholder = 'Type an expression — e.g. sqrt(a * a + b * b)', compact, describedBy, invalid }) => {
    const local = useRef();
    const ref = inputRef ?? local;
    const onKeyDown = (e) => {
        if (e.key === 'Enter' && e.shiftKey) {
            // WHY: the kit's Enter binding (Calculate) would also match Shift+Enter; claim it here for a newline
            e.preventDefault();
            e.currentTarget.setRangeText('\n', e.currentTarget.selectionStart, e.currentTarget.selectionEnd, 'end');
            onInput(e.currentTarget.value);
        } else if (e.key === 'Escape' && value && !compact) {
            e.preventDefault();
            onInput('');
        }
    };
    return html`
        <div class=${cx('shy-editor', compact && 'shy-editor--compact')}>
            <pre class="shy-editor__hl" aria-hidden="true"><${Code} src=${value} errors=${errors} />${'\n'}</pre>
            <textarea ref=${ref} id=${id} class="shy-editor__input" aria-label=${label} rows="1" value=${value} placeholder=${placeholder}
                spellcheck="false" autocomplete="off" autocapitalize="off" autocorrect="off" enterkeyhint="go"
                aria-describedby=${describedBy} aria-invalid=${invalid ? 'true' : undefined}
                onInput=${(e) => onInput(e.currentTarget.value)} onKeyDown=${onKeyDown}></textarea>
        </div>`;
};

const Var = ({ v }) => {
    const id = useId();
    const type = valueType(v.value);
    const isBool = type === 'boolean';
    return html`
        <div class=${cx('shy-var', v.auto && 'is-auto')} data-type=${type}>
            <label class="shy-var__name" for=${id}>
                <span class="u-truncate">${v.name}</span>
                <span class="shy-var__type">${v.auto ? 'auto · ' : ''}${type}</span>
            </label>
            <div class="nzv-input-group">
                <${Icon} name=${TYPE_ICON[type]} />
                <input id=${id} class="nzv-input nzv-input--mono shy-var__input" value=${v.value} spellcheck="false" autocomplete="off"
                    autocapitalize="off" inputmode=${type === 'number' ? 'decimal' : undefined}
                    title=${v.auto ? 'Inferred default — type to override' : undefined}
                    onInput=${(e) => setVar(v.name, e.currentTarget.value)} />
                ${isBool && html`
                    <span class="nzv-input-group__end">
                        <button type="button" class="nzv-btn nzv-btn--ghost nzv-btn--icon nzv-btn--sm" aria-label=${`Flip ${v.name} to ${v.value.toLowerCase() === 'true' ? 'false' : 'true'}`}
                            onClick=${() => setVar(v.name, v.value.toLowerCase() === 'true' ? 'false' : 'true')}>
                            <${Icon} name="refresh-cw" />
                        </button>
                    </span>`}
            </div>
        </div>`;
};

export const VarGrid = () => {
    const list = vars.value;
    if (!list.length) return null;
    return html`
        <section class="shy-vars" aria-label="Variables">
            <div class="shy-vars__head">
                <span class="nzv-eyebrow"><${Icon} name="variable" /> Variables</span>
                <span class="u-tiny u-muted">${list.length} detected · Enter to run</span>
            </div>
            <div class="shy-vars__grid">${list.map((v) => html`<${Var} key=${v.name} v=${v} />`)}</div>
        </section>`;
};
