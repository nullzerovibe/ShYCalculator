import {
    html, render, Icon, TopBar, Footer, Palette, Overlays, bind, hideSplash, registerWorker, themePref, THEMES,
    installable, install, worker, applyUpdate, useMemo, effect, engine,
} from './nzv/nzv.js';
import { Workbench, focusEditor, insertCall } from './views/workbench.js';
import { HistoryCard } from './views/history.js';
import { HelpDialog, TABS } from './views/help.js';
import { SnippetDialog } from './views/snippet.js';
import { REPO, NUGET, iconFor } from './data.js';
import {
    boot, calculate, ui, docs, snippets, settings, load, openSnippet, clearExpr, toggleAst, copyResult, copyEquation, copyJson,
    shareResult, clearHistory, exportHistory, importSnippets, exportSnippets, appVersion, expr,
} from './state.js';

const help = (tab) => { ui.help.value = tab; };
const nextTheme = { auto: 'light', light: 'dark', dark: 'auto' };

bind([
    { keys: 'enter', label: 'Calculate (in the editor or a variable)', group: 'Editor', whenTyping: true, run: () => calculate() },
    { keys: 'mod+enter', label: 'Calculate', group: 'Editor', whenTyping: true, run: () => calculate() },
    { keys: 'mod+s', label: 'Save expression to library', group: 'Editor', whenTyping: true, run: () => openSnippet() },
    { keys: '/', label: 'Focus the editor', group: 'Editor', run: focusEditor },
    { keys: 'c', label: 'Copy result', group: 'Result', run: copyResult },
    { keys: 'shift+c', label: 'Copy equation', group: 'Result', run: copyEquation },
    { keys: 'a', label: 'Toggle syntax tree', group: 'Result', run: toggleAst },
    { keys: 'f', label: 'Functions reference', group: 'Navigate', run: () => help('functions') },
    { keys: 'l', label: 'Expression library', group: 'Navigate', run: () => help('library') },
    { keys: ',', label: 'Settings', group: 'Navigate', run: () => help('settings') },
    { keys: '?', label: 'Keyboard shortcuts', group: 'Navigate', run: () => help('shortcuts') },
    { keys: 't', label: 'Cycle theme', group: 'Navigate', run: () => { themePref.value = nextTheme[themePref.value]; } },
]);

const commands = (lib, fns) => [
    { id: 'calc', label: 'Calculate', icon: 'zap', group: 'Actions', keys: 'mod+enter', run: () => calculate() },
    { id: 'save', label: 'Save expression to library', icon: 'bookmark-plus', group: 'Actions', keys: 'mod+s', run: () => openSnippet() },
    { id: 'focus', label: 'Focus the editor', icon: 'terminal', group: 'Actions', keys: '/', run: focusEditor },
    { id: 'clear', label: 'Clear expression', icon: 'x', group: 'Actions', run: clearExpr },
    { id: 'copy', label: 'Copy result', icon: 'copy', group: 'Actions', keys: 'c', run: copyResult },
    { id: 'copy-eq', label: 'Copy equation', icon: 'calculator', group: 'Actions', keys: 'shift+c', run: copyEquation },
    { id: 'copy-json', label: 'Copy result as JSON', icon: 'braces', group: 'Actions', run: copyJson },
    { id: 'share', label: 'Share result', icon: 'share-2', group: 'Actions', run: shareResult },
    { id: 'ast', label: 'Toggle syntax tree', icon: 'network', group: 'Actions', keys: 'a', run: toggleAst },
    { id: 'hist-csv', label: 'Export history as CSV', icon: 'download', group: 'Actions', run: () => exportHistory('csv') },
    { id: 'hist-json', label: 'Export history as JSON', icon: 'download', group: 'Actions', run: () => exportHistory('json') },
    { id: 'hist-clear', label: 'Clear history', icon: 'trash-2', group: 'Actions', run: clearHistory },
    { id: 'lib-import', label: 'Import library (JSON)', icon: 'upload', group: 'Actions', run: importSnippets },
    { id: 'lib-export', label: 'Export library (JSON)', icon: 'download', group: 'Actions', run: exportSnippets },
    ...lib.map((s) => ({ id: `snip-${s.id}`, label: s.label, hint: s.value, icon: s.icon, group: 'Library', keywords: s.group, run: () => load(s) })),
    ...fns.map((f) => ({ id: `fn-${f.name}`, label: `${f.name}(${f.args.join(', ')})`, hint: f.description, icon: iconFor(f.category), group: 'Insert function', keywords: f.category, run: () => insertCall(f.name) })),
    ...THEMES.map((t) => ({ id: `theme-${t.value}`, label: `Theme: ${t.label}`, icon: t.icon, group: 'Settings', run: () => { themePref.value = t.value; } })),
    { id: 'history-toggle', label: settings.value.enableHistory ? 'Turn history off' : 'Turn history on', icon: 'history', group: 'Settings', run: () => { settings.value = { ...settings.value, enableHistory: !settings.value.enableHistory }; } },
    { id: 'settings', label: 'Open settings', icon: 'settings-2', group: 'Settings', keys: ',', run: () => help('settings') },
    ...(installable.value ? [{ id: 'install', label: 'Install app', icon: 'download', group: 'Settings', run: install }] : []),
    ...(worker.value === 'update' ? [{ id: 'update', label: 'Update to the new version', icon: 'refresh-cw', group: 'Settings', run: applyUpdate }] : []),
    ...TABS.filter((t) => t.id !== 'settings').map((t) => ({ id: `help-${t.id}`, label: t.label, icon: t.icon, group: 'Help', run: () => help(t.id) })),
    { id: 'repo', label: 'Source on GitHub', icon: 'github', group: 'Help', run: () => open(REPO, '_blank', 'noopener') },
];

const App = () => {
    const cmds = useMemo(() => commands(snippets.value, docs.value.functions),
        [snippets.value, docs.value, settings.value.enableHistory, installable.value, worker.value]);
    return html`
        <div class="nzv-shell">
            <${TopBar} name=${html`ShY<b>Calculator</b>`} version=${appVersion.value}>
                <button type="button" class="nzv-btn nzv-btn--ghost nzv-btn--icon u-hide-sm" aria-label="Functions reference" data-tip="Reference (F)" data-tip-pos="bottom"
                    onClick=${() => help('functions')}><${Icon} name="book-open" /></button>
                <button type="button" class="nzv-btn nzv-btn--ghost nzv-btn--icon u-hide-sm" aria-label="Settings" data-tip="Settings (,)" data-tip-pos="bottom"
                    onClick=${() => help('settings')}><${Icon} name="settings-2" /></button>
            <//>
            <main class="nzv-main shy-main">
                <${Workbench} />
                <aside class="shy-side"><${HistoryCard} /></aside>
            </main>
            <${Footer} repo=${REPO} links=${[{ label: 'NuGet', href: NUGET, icon: 'nuget' }]} />
            <${HelpDialog} />
            <${SnippetDialog} />
            <${Palette} commands=${cmds} placeholder="Run a command, load an expression, insert a function…" />
            <${Overlays} />
        </div>`;
};

render(html`<${App} />`, document.getElementById('app'));
registerWorker('sw.js');

effect(() => {
    if (engine.value !== 'boot') hideSplash();
});
// WHY: auto-focus only with a fine pointer; on phones it would pop the keyboard over the page
boot().then(() => { if (!expr.value && matchMedia('(pointer: fine)').matches) focusEditor(); });
