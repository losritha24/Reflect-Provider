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
