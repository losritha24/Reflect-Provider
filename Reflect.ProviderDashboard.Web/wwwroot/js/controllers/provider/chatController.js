(function () {
    const form = document.getElementById('chatForm');
    if (!form) return;
    const input = document.getElementById('chatInput');
    const log = document.getElementById('chatLog');
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // textContent only: replies are never interpreted as HTML.
    function add(role, text) {
        const p = document.createElement('p');
        p.className = 'chat-msg chat-' + role;
        p.textContent = text;
        log.appendChild(p);
        log.scrollTop = log.scrollHeight;
    }

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        const message = input.value.trim();
        if (!message) return;
        add('user', message);
        input.value = '';
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
        input.focus();
    });
})();
