// Small progressive enhancements. The site works without any of this.
(function () {
  // Event pickers submit as soon as a different event is chosen.
  document.querySelectorAll('select[data-autosubmit]').forEach(function (select) {
    select.addEventListener('change', function () { select.form.submit(); });
  });

  // Destructive buttons ask first.
  document.querySelectorAll('form[data-confirm]').forEach(function (form) {
    form.addEventListener('submit', function (event) {
      if (!window.confirm(form.getAttribute('data-confirm'))) event.preventDefault();
    });
  });

  // Show the participant-only and organiser-only registration fields for the chosen role.
  var roleInputs = document.querySelectorAll('input[name="Role"]');
  var participantFields = document.getElementById('participantFields');
  var organiserFields = document.getElementById('organiserFields');
  function syncRole() {
    var selected = document.querySelector('input[name="Role"]:checked');
    if (!selected) return;
    if (participantFields) participantFields.hidden = selected.value !== 'Participant';
    if (organiserFields) organiserFields.hidden = selected.value !== 'Organiser';
  }
  roleInputs.forEach(function (input) { input.addEventListener('change', syncRole); });
  syncRole();

  // Preview a chosen image before upload.
  document.querySelectorAll('input[type="file"][data-preview]').forEach(function (input) {
    input.addEventListener('change', function () {
      var target = document.getElementById(input.getAttribute('data-preview'));
      if (!target || !input.files || !input.files[0]) return;
      target.src = URL.createObjectURL(input.files[0]);
      target.hidden = false;
      var initials = document.getElementById('pictureInitials');
      if (initials) initials.hidden = true;
    });
  });
})();
