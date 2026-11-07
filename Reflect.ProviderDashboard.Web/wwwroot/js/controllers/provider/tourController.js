(function () {
    const dialog = document.getElementById('tourDialog');
    if (!dialog || typeof dialog.showModal !== 'function') return;
    const KEY = 'reflect-provider-tour-seen';
    const steps = [
        ['Welcome', 'This dashboard shows how your clients use Reflect on mobile and in XR. All records here are synthetic.'],
        ['Dashboard', 'Start here. The follow-up queue explains why each person needs attention, and recent activity lists the latest sessions.'],
        ['Patients', 'Search your caseload, filter by engagement trend, then open a person to see their session timeline. Reports lets you pick a date range, then print or download a PDF.'],
        ['Assistant', 'The round Reflect button at the bottom right opens the assistant. It is a prototype: common identifiers are removed, names are not reliably detected, and messages are not saved.'],
        ['Display options', 'Use Account & display to change theme and text size, or to sign out. Times are shown with their time zone (EST/EDT).']
    ];
    let i = 0;
    const $ = id => document.getElementById(id);
    function render() {
        $('tourTitle').textContent = steps[i][0];
        $('tourBody').textContent = steps[i][1];
        $('tourStep').textContent = 'Step ' + (i + 1) + ' of ' + steps.length;
        $('tourBack').disabled = i === 0;
        $('tourNext').textContent = i === steps.length - 1 ? 'Done' : 'Next';
    }
    function close() {
        try { localStorage.setItem(KEY, '1'); } catch { }
        dialog.close();
    }
    function open() { i = 0; render(); dialog.showModal(); }
    $('tourNext').addEventListener('click', () => { if (i === steps.length - 1) close(); else { i++; render(); } });
    $('tourBack').addEventListener('click', () => { if (i > 0) { i--; render(); } });
    $('tourSkip').addEventListener('click', close);
    $('tourOpen').addEventListener('click', open);
    let seen = false;
    try { seen = localStorage.getItem(KEY) === '1'; } catch { }
    if (!seen) open();
})();
