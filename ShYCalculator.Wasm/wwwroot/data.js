/* Static catalogues: seed library, icon vocabulary, date presets. */

export const REPO = 'https://github.com/nullzerovibe/ShYCalculator';
export const NUGET = 'https://www.nuget.org/packages/ShYCalculator';

export const EXAMPLE_GROUPS = [
    {
        label: 'Basic', icon: 'calculator', items: [
            { label: 'Simple Arithmetic', value: '1 + 2 * 3' },
            { label: 'Division & Addition', value: '10 / 2 + 5' },
            { label: 'Power (2^3)', value: '2 ^ 3' },
        ],
    },
    {
        label: 'Date & Time', icon: 'calendar', items: [
            { label: 'Current Date', value: 'dt_now()' },
            { label: 'Today (Midnight)', value: 'dt_today()' },
            { label: 'Add 1 Year', value: "dt_add(dt_today(), '1y')" },
            { label: 'Day of Week', value: 'dt_dayofweek(dt_now())' },
            { label: 'Current Year', value: 'dt_year(dt_now())' },
        ],
    },
    {
        label: 'String Operations', icon: 'type', items: [
            { label: "Contains 'World'?", value: "str_contains('Hello World', 'World')" },
            { label: 'Case-Insensitive Equal', value: "str_equal('abc', 'ABC', true)" },
            { label: "Starts With 'Hello'", value: "str_starts('Hello World', 'Hello')" },
            { label: "Ends With '.txt'", value: "str_ends('test.txt', '.txt')" },
        ],
    },
    {
        label: 'Scientific', icon: 'sigma', items: [
            { label: 'Trigonometry', value: 'sin(0) + cos(0)' },
            { label: 'Logarithm (base 10)', value: 'log(100, 10)' },
            { label: 'Roots & Absolute', value: 'sqrt(16) + abs(-5)' },
        ],
    },
    {
        label: 'Logic', icon: 'cpu', items: [
            { label: 'Logical AND', value: 'true && false' },
            { label: 'Logical OR', value: 'true || false' },
            { label: 'Logical NOT', value: '!true' },
            { label: 'Combined Logic', value: '(5 > 3) && (2 < 4)' },
            { label: 'Simple If/Else', value: 'if(5 > 3, 100, 0)' },
            { label: 'Max & Min', value: 'max(10, 20) + min(5, 1)' },
            { label: 'Ternary Operator', value: 'true ? 1 : 0' },
        ],
    },
    {
        label: 'Bitwise Operations', icon: 'binary', items: [
            { label: 'Bitwise AND', value: '5 & 3' },
            { label: 'Bitwise OR', value: '5 | 3' },
            { label: 'Bitwise XOR', value: '5 ^^ 3' },
            { label: 'Bitwise NOT', value: '~5' },
            { label: 'Bitwise Left Shift', value: '8 << 2' },
            { label: 'Bitwise Right Shift', value: '16 >> 2' },
        ],
    },
    {
        label: 'Expressions (Uses Variables)', icon: 'square-function', items: [
            { label: 'Area of Circle', value: 'pi * r * r', vars: [{ name: 'r', value: '5' }] },
            { label: 'Pythagoras', value: 'a * a + b * b', vars: [{ name: 'a', value: '3' }, { name: 'b', value: '4' }] },
            { label: 'Linear Equation', value: 'offset + scale * x', vars: [{ name: 'offset', value: '10' }, { name: 'scale', value: '2' }, { name: 'x', value: '5' }] },
        ],
    },
    {
        label: 'Complex Scenarios', icon: 'layers', items: [
            {
                label: 'Driver Qualification',
                value: "(age >= 18 && any(hasLicense, false)) ? (if(max(score, 0) > 80, 'Qualified', 'Needs Retake')) : default",
                vars: [{ name: 'age', value: '20' }, { name: 'hasLicense', value: 'true' }, { name: 'score', value: '85' }, { name: 'default', value: 'Ineligible' }],
            },
            {
                label: 'Shipping Calculator',
                value: "if(weight <= 0, 'Invalid Weight', 'Total: $' + round(max(5, weight * rate) + (express ? 10 : 0), 2))",
                vars: [{ name: 'weight', value: '12.5' }, { name: 'rate', value: '0.8' }, { name: 'express', value: 'true' }],
            },
        ],
    },
];

export const seedSnippets = () => EXAMPLE_GROUPS.flatMap((g) => g.items.map((it) => ({
    label: it.label, value: it.value, vars: it.vars ?? [], icon: g.icon, group: g.label, isSeeded: true,
}))).map((s, i) => ({ id: `seeded_${i}`, ...s }));

/** Icon picker for saved expressions; the section name becomes the snippet group. */
export const ICON_CHOICES = [
    { group: 'Arithmetical', icons: ['calculator', 'plus', 'minus', 'equal', 'parentheses', 'superscript'] },
    { group: 'Numeric', icons: ['hash', 'list-ordered', 'arrow-up-0-1', 'tally-5', 'percent', 'dice-5'] },
    { group: 'Scientific', icons: ['sigma', 'flask-conical', 'atom', 'infinity', 'brain', 'activity'] },
    { group: 'String', icons: ['type', 'text-select', 'message-square', 'quote', 'regex', 'tags'] },
    { group: 'Logical', icons: ['circuit-board', 'binary', 'cpu', 'list-tree', 'power', 'network'] },
    { group: 'Date', icons: ['calendar', 'calendar-clock', 'timer', 'history', 'sun', 'moon'] },
    { group: 'Custom Expressions', icons: ['bookmark', 'heart', 'trophy', 'star', 'zap', 'rocket', 'wrench', 'layers'] },
];

export const groupOfIcon = (icon) => ICON_CHOICES.find((c) => c.icons.includes(icon))?.group ?? 'Custom Expressions';

// WHY: stored snippets and engine categories use the old Shoelace/Iconify vocabulary; map it once onto the kit sprite
const ALIAS = {
    arithmetic: 'calculator', arithmetical: 'calculator', exponent: 'superscript', power: 'power', divide: 'calculator', multiply: 'calculator',
    unary: 'plus', comparison: 'equal', grouping: 'parentheses', logical: 'circuit-board', logic: 'circuit-board',
    bitwise: 'binary', 'bitwise operations': 'binary', 'power-button': 'power', switch: 'power', 'on/off': 'power',
    scientific: 'sigma', trigonometric: 'sigma', trigonometry: 'sigma', math: 'sigma', chemistry: 'flask-conical', physics: 'atom',
    sort: 'arrow-up-0-1', 'sort-asc': 'arrow-up-0-1', tally: 'tally-5', count: 'tally-5', 'logic tree': 'list-tree',
    numeric: 'hash', number: 'hash', list: 'list-ordered', dice: 'dice-5', random: 'dice-5',
    string: 'type', 'string operations': 'type', text: 'type', 'select text': 'text-select', message: 'message-square',
    'string quote': 'quote', 'regular expression': 'regex',
    date: 'calendar', datetime: 'calendar', 'date & time': 'calendar', time: 'calendar-clock', clock: 'calendar-clock', stopwatch: 'timer',
    array: 'layers', aggregate: 'layers', 'complex scenarios': 'layers', all_categories: 'layout-grid',
    'function-square': 'square-function', 'expressions (uses variables)': 'square-function',
    winner: 'trophy', king: 'crown', lightning: 'zap', flash: 'zap', tools: 'wrench', tool: 'wrench',
};
const SPRITE = new Set(ICON_CHOICES.flatMap((c) => c.icons).concat(['square-function', 'layout-grid', 'crown', 'hourglass', 'sigma', 'cpu']));

export const iconFor = (name) => {
    const k = String(name ?? '').trim().toLowerCase();
    return SPRITE.has(k) ? k : ALIAS[k] ?? 'square-function';
};

export const TYPE_ICON = { number: 'hash', boolean: 'toggle-left', date: 'calendar', string: 'type', list: 'layers' };

export const DATE_FORMATS = ['dd/MM/yyyy', 'MM/dd/yyyy', 'yyyy-MM-dd', 'd.M.yyyy', 'yyyy/MM/dd', 'd-M-yyyy', 'dd.MM.yyyy', 'yyyy.MM.dd', 'dd-MM-yyyy', 'M-d-yyyy'];
export const CULTURES = ['en-US', 'en-GB', 'de-DE', 'nl-NL', 'it-IT', 'hr-HR', 'es-ES', 'fr-FR', 'ru-RU', 'en-CA'];
