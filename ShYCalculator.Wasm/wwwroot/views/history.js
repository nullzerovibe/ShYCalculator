import { html, Icon, Menu, HistoryList } from '../nzv/nzv.js';
import { Code } from '../editor.js';
import { history, settings, load, removeHistory, clearHistory, exportHistory, ui } from '../state.js';

const ago = (t) => {
    const s = Math.round((Date.now() - t) / 1000);
    if (s < 60) return 'now';
    if (s < 3600) return `${Math.floor(s / 60)}m`;
    if (s < 86400) return `${Math.floor(s / 3600)}h`;
    return new Date(t).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
};

export const HistoryCard = () => {
    const on = settings.value.enableHistory;
    const list = history.value;
    return html`
        <section class="nzv-card shy-history" aria-labelledby="history-title">
            <div class="nzv-card__head">
                <h2 class="nzv-card__title" id="history-title"><${Icon} name="history" />History
                    ${on && list.length > 0 && html`<span class="nzv-chip">${list.length}</span>`}</h2>
                ${on && list.length > 0 && html`
                    <div class="nzv-card__tools">
                        <${Menu} label="History actions" icon="menu" items=${[
                            { label: 'Export CSV', icon: 'download', run: () => exportHistory('csv') },
                            { label: 'Export JSON', icon: 'braces', run: () => exportHistory('json') },
                            '-',
                            { label: 'Clear history', icon: 'trash-2', danger: true, run: clearHistory },
                        ]} />
                    </div>`}
            </div>
            ${on
                ? html`<${HistoryList} items=${list} empty="Results you calculate land here"
                    describe=${(h) => ({
                        key: `${h.timestamp}-${h.expr}`,
                        title: html`<${Code} src=${h.expr} />`,
                        value: html`<span class="u-muted">=</span> ${h.result}`,
                        aside: html`<span class="shy-history__aside">${ago(h.timestamp)}${h.vars.length > 0 && html`<span title=${h.vars.map((v) => `${v.name}=${v.value}`).join(', ')}><${Icon} name="variable" />${h.vars.length}</span>`}</span>`,
                    })}
                    onPick=${(h) => load({ value: h.expr, vars: h.vars })} onRemove=${removeHistory} />`
                : html`
                    <div class="nzv-empty">
                        <${Icon} name="history" />History is turned off
                        <button type="button" class="nzv-btn nzv-btn--sm" onClick=${() => { ui.help.value = 'settings'; }}>Open settings</button>
                    </div>`}
        </section>`;
};
