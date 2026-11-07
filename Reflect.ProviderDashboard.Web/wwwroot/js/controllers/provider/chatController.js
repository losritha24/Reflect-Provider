(function () {
    const form = document.getElementById('chatForm');
    if (!form) return;
    const panel = document.getElementById('chatPanel');
    const launcher = document.getElementById('chatLauncher');
    const input = document.getElementById('chatInput');
    const log = document.getElementById('chatLog');
    const chips = document.getElementById('chatChips');
    const tokenInput = document.querySelector('#chatWidget input[name="__RequestVerificationToken"]');
    if (!tokenInput) return;
    const token = tokenInput.value;
    let greeted = false;

    // textContent only: replies are never interpreted as HTML.
    function add(role, text) {
        const p = document.createElement('p');
        p.className = 'chat-msg chat-' + role;
        p.textContent = text;
        log.appendChild(p);
        log.scrollTop = log.scrollHeight;
    }

    function setOpen(open) {
        panel.hidden = !open;
        launcher.setAttribute('aria-expanded', String(open));
        launcher.setAttribute('aria-label', open ? 'Close assistant' : 'Open assistant');
        if (open) {
            if (!greeted) { add('bot', 'Hi, I can help you find your way around the dashboard.'); greeted = true; }
            input.focus();
        } else {
            launcher.focus();
        }
    }

    async function send(message) {
        add('user', message);
        chips.hidden = true;
        try {
            const res = await fetch('/Provider/Chat/Send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ message: message })
            });
            if (!res.ok) throw new Error(String(res.status));
            const data = await res.json();
            add('bot', data.reply);
            if (data.redacted && data.redacted.length)
                add('note', 'Removed before processing: ' + data.redacted.join(', ') + '.');
        } catch {
            add('note', 'The assistant is unavailable. Please try again.');
        }
    }

    launcher.addEventListener('click', () => setOpen(panel.hidden));
    document.getElementById('chatClose').addEventListener('click', () => setOpen(false));
    document.addEventListener('keydown', e => { if (e.key === 'Escape' && !panel.hidden) setOpen(false); });
    chips.addEventListener('click', e => {
        const chip = e.target.closest('.chat-chip');
        if (chip) send(chip.textContent);
    });
    form.addEventListener('submit', e => {
        e.preventDefault();
        const message = input.value.trim();
        if (!message) return;
        input.value = '';
        send(message).then(() => input.focus());
    });
})();
