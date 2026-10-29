const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const source = fs.readFileSync('Reflect.ProviderDashboard.Web/wwwroot/standins/js/layoutController.js', 'utf8');
function setup(stored = '{}', storageThrows = false) {
  const elements = {};
  const document = { activeElement: null, addEventListener() {}, getElementById: id => elements[id], querySelectorAll: () => [] };
  function element(id) {
    const handlers = {}, attrs = {}, classes = new Set();
    return elements[id] = {
      handlers, attrs, value: '100%', checked: false, style: {},
      classList: { add: c => classes.add(c), remove: c => classes.delete(c), contains: c => classes.has(c) },
      addEventListener: (type, fn) => { handlers[type] = fn; },
      setAttribute: (k,v) => { attrs[k] = v; }, removeAttribute: k => delete attrs[k], getAttribute: k => attrs[k],
      focus() { document.activeElement = this; },
      querySelectorAll: () => [],
      fire(type, event = {}) { handlers[type]?.(event); }
    };
  }
  document.documentElement = element('root');
  for (const id of ['a11yPanelToggle', 'a11yPanel', 'a11yBackdrop', 'a11yPanelClose', 'textSizeSelect', 'colorBlindToggle', 'panelThemeSelect', 'appShell']) element(id);
  const controls = [elements.a11yPanelClose, elements.panelThemeSelect, elements.textSizeSelect, elements.colorBlindToggle];
  elements.a11yPanel.querySelectorAll = () => controls;
  let saved;
  const window = { localStorage: {
    getItem() { if (storageThrows) throw Error('Storage denied'); return stored; },
    setItem(k,v) { if (storageThrows) throw Error('Storage denied'); saved = JSON.parse(v); }
  }};
  vm.runInNewContext(source, { window, document });
  window.LayoutController.init();
  return { elements, document, saved: () => saved };
}
test('modal traps focus, closes on Escape, and restores its trigger', () => {
  const { elements: e, document: d } = setup();
  e.a11yPanelToggle.focus();
  e.a11yPanelToggle.fire('click');
  assert.equal(d.activeElement, e.a11yPanelClose);
  assert.equal(e.appShell.inert, true);
  let prevented = false;
  e.a11yPanel.fire('keydown', { key: 'Tab', shiftKey: true, preventDefault() { prevented = true; } });
  assert.equal(prevented, true);
  assert.equal(d.activeElement, e.colorBlindToggle);
  e.a11yPanel.fire('keydown', { key: 'Tab', preventDefault() {} });
  assert.equal(d.activeElement, e.a11yPanelClose);
  e.a11yPanel.fire('keydown', { key: 'Escape', preventDefault() {} });
  assert.equal(d.activeElement, e.a11yPanelToggle);
  assert.equal(e.appShell.inert, false);
  assert.equal(e.a11yPanel.inert, true);
  assert.equal(e.a11yPanelToggle.attrs['aria-expanded'], 'false');
});
test('preferences restore and persist only display settings', () => {
  const state = setup('{"textSize":"125%","colorBlind":true}');
  assert.equal(state.elements.root.style.fontSize, '125%');
  assert.equal(state.elements.colorBlindToggle.checked, true);
  state.elements.textSizeSelect.value = '112.5%';
  state.elements.textSizeSelect.fire('change');
  assert.deepEqual(state.saved(), { textSize: '112.5%', colorBlind: true });
});
test('corrupt or unavailable storage does not break controls', () => {
  for (const state of [setup('{bad'), setup('{}', true), setup('{"textSize":"999%"}')]) {
    state.elements.a11yPanelToggle.fire('click');
    assert.equal(state.elements.appShell.inert, true);
    state.elements.textSizeSelect.fire('change');
  }
});
