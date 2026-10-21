window.LayoutController = {
  init: function () {
    var root = document.documentElement;
    var themeToggle = document.getElementById('themeToggle');
    var a11yToggle = document.getElementById('a11yPanelToggle');
    var a11yPanel = document.getElementById('a11yPanel');
    var a11yBackdrop = document.getElementById('a11yBackdrop');
    var textSizeSelect = document.getElementById('textSizeSelect');
    var colorBlindToggle = document.getElementById('colorBlindToggle');
    var panelThemeSelect = document.getElementById('panelThemeSelect');

    if (themeToggle) {
      themeToggle.addEventListener('click', function () {
        var next = root.getAttribute('data-theme-mode') === 'dark' ? 'light' : 'dark';
        root.setAttribute('data-theme-mode', next);
        if (panelThemeSelect) panelThemeSelect.value = next;
      });
    }

    if (panelThemeSelect) {
      panelThemeSelect.addEventListener('change', function () {
        root.setAttribute('data-theme-mode', panelThemeSelect.value);
      });
    }

    if (a11yToggle && a11yPanel && a11yBackdrop) {
      a11yToggle.addEventListener('click', function () {
        a11yPanel.classList.add('open');
        a11yBackdrop.classList.add('open');
      });
      a11yBackdrop.addEventListener('click', closePanel);
      document.getElementById('a11yPanelClose')?.addEventListener('click', closePanel);
    }

    function closePanel() {
      a11yPanel.classList.remove('open');
      a11yBackdrop.classList.remove('open');
    }

    if (textSizeSelect) {
      textSizeSelect.addEventListener('change', function () {
        root.style.fontSize = textSizeSelect.value;
      });
    }

    if (colorBlindToggle) {
      colorBlindToggle.addEventListener('change', function () {
        if (colorBlindToggle.checked) {
          root.setAttribute('data-a11y-colorblind', 'true');
        } else {
          root.removeAttribute('data-a11y-colorblind');
        }
      });
    }
  }
};

document.addEventListener('DOMContentLoaded', function () {
  LayoutController.init();
});
