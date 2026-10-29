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
    var appShell = document.getElementById('appShell');
    var mobileMenuToggle = document.getElementById('mobileMenuToggle');
    var sidebarCollapseToggle = document.getElementById('sidebarCollapseToggle');
    var sidebarBackdrop = document.getElementById('sidebarBackdrop');
    var themeStorageKey = 'reflect-provider-theme';
    var preferencesKey = 'reflect-provider-accessibility';
    var returnFocus;
    var preferences = {};
    try {
      preferences = JSON.parse(window.localStorage.getItem(preferencesKey)) || {};
    } catch { }
    function savePreferences() {
      try {
        window.localStorage.setItem(preferencesKey, JSON.stringify({
          textSize: textSizeSelect?.value || '100%',
          colorBlind: colorBlindToggle?.checked || false
        }));
      } catch { }
    }
    if (textSizeSelect && ['100%', '112.5%', '125%'].includes(preferences.textSize)) {
      textSizeSelect.value = preferences.textSize;
      root.style.fontSize = preferences.textSize;
    }
    if (colorBlindToggle && preferences.colorBlind === true) {
      colorBlindToggle.checked = true;
      root.setAttribute('data-a11y-colorblind', 'true');
    }

    document.querySelectorAll('[data-navigation-link]').forEach(function (link) {
      link.addEventListener('keydown', function (event) {
        if (event.key === ' ') {
          event.preventDefault();
          link.click();
        }
      });
    });
    document.querySelectorAll('[data-navigation-row]').forEach(function (row) {
      row.addEventListener('click', function (event) {
        if (event.target.closest('a, button, input, select, textarea')) return;
        row.querySelector('[data-navigation-link]')?.click();
      });
    });

    function closeMobileMenu() {
      if (!appShell || !sidebarBackdrop || !mobileMenuToggle) return;
      appShell.classList.remove('mobile-menu-open');
      sidebarBackdrop.classList.remove('open');
      mobileMenuToggle.setAttribute('aria-expanded', 'false');
    }

    if (mobileMenuToggle && appShell && sidebarBackdrop) {
      mobileMenuToggle.addEventListener('click', function () {
        var isOpen = appShell.classList.toggle('mobile-menu-open');
        sidebarBackdrop.classList.toggle('open', isOpen);
        mobileMenuToggle.setAttribute('aria-expanded', String(isOpen));
      });
      sidebarBackdrop.addEventListener('click', closeMobileMenu);
      document.querySelectorAll('#providerSidebar a').forEach(function (link) {
        link.addEventListener('click', closeMobileMenu);
      });
      document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') closeMobileMenu();
      });
    }

    if (sidebarCollapseToggle && appShell) {
      sidebarCollapseToggle.addEventListener('click', function () {
        var isCollapsed = appShell.classList.toggle('sidebar-collapsed');
        sidebarCollapseToggle.setAttribute('aria-expanded', String(!isCollapsed));
        sidebarCollapseToggle.textContent = isCollapsed ? 'Expand navigation' : 'Collapse navigation';
      });
    }

    function setTheme(theme) {
      if (theme !== 'light' && theme !== 'dark') return;

      root.setAttribute('data-theme-mode', theme);
      if (panelThemeSelect) panelThemeSelect.value = theme;
      try {
        window.localStorage.setItem(themeStorageKey, theme);
      } catch { }
    }

    if (panelThemeSelect) {
      panelThemeSelect.value = root.getAttribute('data-theme-mode') || 'light';
    }

    if (themeToggle) {
      themeToggle.addEventListener('click', function () {
        var next = root.getAttribute('data-theme-mode') === 'dark' ? 'light' : 'dark';
        setTheme(next);
      });
    }

    if (panelThemeSelect) {
      panelThemeSelect.addEventListener('change', function () {
        setTheme(panelThemeSelect.value);
      });
    }

    if (a11yToggle && a11yPanel && a11yBackdrop) {
      a11yToggle.addEventListener('click', function () {
        returnFocus = document.activeElement;
        a11yPanel.inert = false;
        a11yPanel.removeAttribute('aria-hidden');
        a11yToggle.setAttribute('aria-expanded', 'true');
        if (appShell) appShell.inert = true;
        a11yPanel.classList.add('open');
        a11yBackdrop.classList.add('open');
        document.getElementById('a11yPanelClose')?.focus();
      });
      a11yPanel.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
          event.preventDefault();
          closePanel();
        }
        if (event.key !== 'Tab') return;
        var controls = Array.from(a11yPanel.querySelectorAll('button, select, input, a[href], [tabindex="0"]'))
          .filter(function (control) { return !control.disabled && !control.hidden; });
        var first = controls[0];
        var last = controls[controls.length - 1];
        if (event.shiftKey && document.activeElement === first) {
          event.preventDefault();
          last?.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
          event.preventDefault();
          first?.focus();
        }
      });
      a11yBackdrop.addEventListener('click', closePanel);
      document.getElementById('a11yPanelClose')?.addEventListener('click', closePanel);
    }

    function closePanel() {
      if (appShell) appShell.inert = false;
      returnFocus?.focus();
      a11yPanel.inert = true;
      a11yPanel.setAttribute('aria-hidden', 'true');
      a11yToggle.setAttribute('aria-expanded', 'false');
      a11yPanel.classList.remove('open');
      a11yBackdrop.classList.remove('open');
    }

    if (textSizeSelect) {
      textSizeSelect.addEventListener('change', function () {
        root.style.fontSize = textSizeSelect.value;
        savePreferences();
      });
    }

    if (colorBlindToggle) {
      colorBlindToggle.addEventListener('change', function () {
        if (colorBlindToggle.checked) {
          root.setAttribute('data-a11y-colorblind', 'true');
        } else {
          root.removeAttribute('data-a11y-colorblind');
        }
        savePreferences();
      });
    }
  }
};

document.addEventListener('DOMContentLoaded', function () {
  LayoutController.init();
});
