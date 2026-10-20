// TO2 redesign demo helpers: theme toggle, top bar, menus, dialogs, toasts. Demo only, no backend calls.

(function () {
  var THEME_KEY = 'to2-demo-theme';

  function readTheme() {
    try { return localStorage.getItem(THEME_KEY) || 'system'; } catch (e) { return 'system'; }
  }
  function applyTheme(mode) {
    if (mode === 'system') document.documentElement.removeAttribute('data-theme');
    else document.documentElement.setAttribute('data-theme', mode);
    try { localStorage.setItem(THEME_KEY, mode); } catch (e) { /* private mode */ }
    var btn = document.getElementById('theme-btn');
    if (btn) btn.title = 'Theme: ' + mode;
  }
  applyTheme(readTheme());

  window.cycleTheme = function () {
    var order = ['system', 'light', 'dark'];
    applyTheme(order[(order.indexOf(readTheme()) + 1) % order.length]);
  };

  var ICONS = {
    theme: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="9"/><path d="M12 3v18" /><path d="M12 3a9 9 0 0 1 0 18z" fill="currentColor"/></svg>',
    menu: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 7h16M4 12h16M4 17h16"/></svg>'
  };

  // Top bar shared by app pages: renderTopbar('tournaments' | 'teams' | 'organization')
  window.renderTopbar = function (active) {
    var el = document.getElementById('topbar');
    if (!el) return;
    function link(key, label, href) {
      return '<a href="' + href + '" class="' + (active === key ? 'active' : '') + '">' + label + '</a>';
    }
    el.className = 'topbar';
    el.innerHTML =
      '<button class="icon-btn menu-toggle" aria-label="Open navigation">' + ICONS.menu + '</button>' +
      '<a class="logo" href="tournaments.html"><span class="logo-mark">T2</span>TO2</a>' +
      '<span class="org">Northside Esports</span>' +
      '<nav class="nav">' +
        link('tournaments', 'Tournaments', 'tournaments.html') +
        link('teams', 'Teams', '#') +
        link('organization', 'Organization', '#') +
      '</nav>' +
      '<span class="spacer"></span>' +
      '<span class="live-dot" title="Live updates connected"><span>Live</span></span>' +
      '<button class="icon-btn" id="theme-btn" onclick="cycleTheme()" aria-label="Toggle theme">' + ICONS.theme + '</button>' +
      '<div class="dropdown">' +
        '<button class="avatar" onclick="toggleMenu(\'user-menu\', event)" aria-label="User menu">MK</button>' +
        '<div class="menu" id="user-menu">' +
          '<div class="menu-head"><strong>mara.k</strong><div class="muted small">mara@northside.gg</div></div>' +
          '<button onclick="openInvite()">Invite code</button>' +
          '<a href="index.html">Design overview</a>' +
          '<hr><a href="login.html">Sign out</a>' +
        '</div>' +
      '</div>';
    applyTheme(readTheme());
  };

  window.toggleMenu = function (id, ev) {
    if (ev) ev.stopPropagation();
    document.querySelectorAll('.menu.open').forEach(function (m) { if (m.id !== id) m.classList.remove('open'); });
    document.getElementById(id).classList.toggle('open');
  };
  document.addEventListener('click', function () {
    document.querySelectorAll('.menu.open').forEach(function (m) { m.classList.remove('open'); });
  });

  window.openDialog = function (id) { document.getElementById(id).classList.add('open'); };
  window.closeDialog = function (id) { document.getElementById(id).classList.remove('open'); };
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') document.querySelectorAll('.backdrop.open').forEach(function (b) { b.classList.remove('open'); });
  });

  // toast(kind, title, text): kind = '' | 'success' | 'danger' | 'warning'
  window.toast = function (kind, title, text) {
    var host = document.getElementById('toasts');
    if (!host) { host = document.createElement('div'); host.id = 'toasts'; host.className = 'toasts'; document.body.appendChild(host); }
    var t = document.createElement('div');
    t.className = 'toast ' + (kind || '');
    t.setAttribute('role', 'status');
    t.innerHTML = '<div><strong></strong><span class="muted"></span></div>';
    t.querySelector('strong').textContent = title;
    t.querySelector('span').textContent = text || '';
    host.appendChild(t);
    setTimeout(function () { t.remove(); }, 5000);
  };

  // Invite code dialog (shared): injected on demand
  window.openInvite = function () {
    if (!document.getElementById('invite-dialog')) {
      var d = document.createElement('div');
      d.className = 'backdrop'; d.id = 'invite-dialog';
      d.innerHTML =
        '<div class="dialog" role="dialog" aria-modal="true" aria-labelledby="invite-title">' +
          '<div class="dialog-head"><h3 id="invite-title">Invite to Northside Esports</h3>' +
          '<button class="icon-btn" onclick="closeDialog(\'invite-dialog\')" aria-label="Close">&#x2715;</button></div>' +
          '<div class="dialog-body" id="invite-step1">' +
            '<p>New members register with the organization name <strong>Northside Esports</strong> and an invite code.</p>' +
            '<div class="alert warning"><div><strong>Replaces the current code.</strong> Generating a new code makes the previous one stop working. Codes are valid for 24 hours and can be used by several people.</div></div>' +
          '</div>' +
          '<div class="dialog-body hidden" id="invite-step2">' +
            '<div class="field"><label>Invite code</label>' +
              '<div class="row-flex"><input class="input num" readonly value="K7QM-X2PA-9HTE-WRZC-4BN8-FJ3D" style="font-weight:600;letter-spacing:.04em">' +
              '<button class="btn" onclick="toast(\'success\',\'Copied\',\'Invite code copied to clipboard\')">Copy</button></div>' +
              '<span class="hint">Valid until 21 Oct 2026, 14:05. Shown only once: it cannot be displayed again.</span></div>' +
            '<button class="btn btn-block" onclick="toast(\'success\',\'Copied\',\'Invite message with organization name, code and sign-up link copied\')">Copy invite message</button>' +
          '</div>' +
          '<div class="dialog-foot">' +
            '<button class="btn" onclick="closeDialog(\'invite-dialog\')">Close</button>' +
            '<button class="btn btn-primary" id="invite-gen" onclick="document.getElementById(\'invite-step1\').classList.add(\'hidden\');document.getElementById(\'invite-step2\').classList.remove(\'hidden\');this.classList.add(\'hidden\')">Generate code</button>' +
          '</div>' +
        '</div>';
      document.body.appendChild(d);
    }
    openDialog('invite-dialog');
  };
})();
