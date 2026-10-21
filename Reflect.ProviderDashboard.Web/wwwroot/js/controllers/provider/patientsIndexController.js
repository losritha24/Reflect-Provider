window.PatientsIndexController = {
  init: function (options) {
    document.querySelectorAll('[data-patient-row]').forEach(function (row) {
      row.addEventListener('click', function () {
        var id = row.getAttribute('data-patient-id');
        if (id && options.detailsUrlTemplate) {
          window.location.href = options.detailsUrlTemplate.replace('__ID__', id);
        }
      });
      row.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          row.click();
        }
      });
    });
  }
};
