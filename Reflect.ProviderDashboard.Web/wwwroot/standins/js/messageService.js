window.MessageService = {
  showToastrMessage: function (message, messageType, timeOut) {
    if (!message) return;
    var type = messageType === 'danger' ? 'error' : (messageType || 'info');
    console.log('[toast:' + type + ']', message);
  },
  showConfirmationDialogSuccess: function (title, message, yesText, noText, onConfirm, onCancel) {
    if (window.confirm(title + '\n\n' + message.replace(/<[^>]+>/g, ''))) {
      if (onConfirm) onConfirm();
    } else if (onCancel) {
      onCancel();
    }
  },
  showConfirmationDialogDelete: function (title, message, yesText, noText, onConfirm, onCancel) {
    MessageService.showConfirmationDialogSuccess(title, message, yesText, noText, onConfirm, onCancel);
  },
  showConfirmationDialogInfo: function (title, message, yesText, noText, onConfirm, onCancel) {
    MessageService.showConfirmationDialogSuccess(title, message, yesText, noText, onConfirm, onCancel);
  },
  cancelAction: function () { },
  clearAllMessages: function () { }
};

window.displayLoading = function (msg) {
  var el = document.getElementById('loadingOverlay');
  if (el) {
    el.textContent = msg || 'Loading…';
    el.classList.add('is-visible');
  }
};

window.hideDisplayLoading = function () {
  var el = document.getElementById('loadingOverlay');
  if (el) el.classList.remove('is-visible');
};

window.renderBadge = function (color, text, iconClass) {
  var icon = iconClass ? '<span aria-hidden="true">●</span> ' : '';
  return '<span class="badge bg-' + color + '-transparent text-' + color + '">' + icon + text + '</span>';
};

document.addEventListener('DOMContentLoaded', function () {
  document.querySelectorAll('table.dt-client').forEach(function (table) {
    table.classList.add('table-bordered');
  });
  document.querySelectorAll('.card-clickable[data-href]').forEach(function (card) {
    card.addEventListener('click', function () {
      window.location.href = card.getAttribute('data-href');
    });
  });
  document.querySelectorAll('tr.row-link[data-href]').forEach(function (row) {
    row.addEventListener('click', function () {
      window.location.href = row.getAttribute('data-href');
    });
    row.addEventListener('keydown', function (e) {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        row.click();
      }
    });
  });
});
