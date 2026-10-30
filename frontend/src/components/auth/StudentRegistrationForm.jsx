import { useState } from 'react';
import { UserPlus } from 'lucide-react';
import { authApi } from '../../services/endpoints';
import { extractErrorMessage } from '../../services/api';
import { Alert, Spinner } from '../ui/Feedback';
import ProfilePhotoField from './ProfilePhotoField';

/**
 * Public application for a place at the school.
 *
 * No password is collected: the school issues the sign-in address and a temporary password when
 * an administrator approves the application, and emails them to the address given here. Nothing
 * is signed in on submit, so this form reports back instead of redirecting.
 *
 * @param gradeLevels grades from /api/public/overview.
 * @param sections    sections from the same payload.
 * @param onSubmitted called with the API's receipt message once the queue accepts it.
 */
<<<<<<< HEAD
export default function StudentRegistrationForm({
  gradeLevels = [],
  sections = [],
  initialRole = 'Student',
  onRoleChange,
  onSubmitRegistration = authApi.registerStudent,
  onSubmitted,
}) {
  const [form, setForm] = useState({
    fullName: '',
    email: '',
    phoneNumber: '',
    requestedRole: initialRole,
    gradeLevelId: '',
    sectionId: '',
    departmentSubject: '',
=======
export default function StudentRegistrationForm({ gradeLevels = [], sections = [], onSubmitted }) {
  const [form, setForm] = useState({
    fullName: '',
    email: '',
    requestedRole: 'Student',
    gradeLevelId: '',
    sectionId: '',
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
  });
  const [error, setError] = useState(null);
  const [photoError, setPhotoError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [photo, setPhoto] = useState(null);
  const [photoPending, setPhotoPending] = useState(false);

  const update = (key) => (event) => setForm((prev) => ({ ...prev, [key]: event.target.value }));
<<<<<<< HEAD
  const selectedGrade = gradeLevels.find((grade) => String(grade.id) === form.gradeLevelId);
  const availableSections = selectedGrade?.sections
    ?? sections.filter((section) =>
      !section.gradeLevelIds?.length || section.gradeLevelIds.includes(Number(form.gradeLevelId)),
    );
  const isStudent = form.requestedRole === 'Student';

  function updateRole(event) {
    const requestedRole = event.target.value;
    setForm((previous) => ({
      ...previous,
      requestedRole,
      gradeLevelId: '',
      sectionId: '',
      departmentSubject: '',
    }));
    onRoleChange?.(requestedRole);
  }
=======
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478

  async function submit(event) {
    event.preventDefault();
    setError(null);

    if (!photo || photoPending) {
<<<<<<< HEAD
      setPhotoError('A profile photo is required to generate your Smart ID.');
=======
      setPhotoError('Please upload a profile photo that will be used to generate your digital Smart ID. You can only change it later by contacting the administrator.');
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
      if (photoPending) {
        setError('Confirm or cancel the selected photo before submitting your registration.');
      }
      return;
    }

    setPhotoError('');
    setSubmitting(true);

    try {
<<<<<<< HEAD
      const receipt = await onSubmitRegistration({
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phoneNumber: form.phoneNumber.trim(),
        requestedRole: form.requestedRole,
        gradeLevelId: isStudent ? Number(form.gradeLevelId) : null,
        sectionId: isStudent ? Number(form.sectionId) : null,
        departmentSubject: isStudent ? null : form.departmentSubject.trim(),
=======
      const receipt = await authApi.registerStudent({
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        requestedRole: form.requestedRole,
        gradeLevelId: form.requestedRole === 'Student' ? Number(form.gradeLevelId) : null,
        sectionId: form.requestedRole === 'Student' ? Number(form.sectionId) : null,
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        photo,
      });

      onSubmitted(
        receipt?.message ??
          'Your registration has been received and is waiting for the school to review it.',
      );
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
<<<<<<< HEAD
    <form onSubmit={submit} className="space-y-4">
=======
    <form onSubmit={submit} noValidate className="space-y-4">
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
      {error && (
        <Alert variant="error" title="Registration failed" onDismiss={() => setError(null)}>
          {error}
        </Alert>
      )}

      <div>
        <label className="label" htmlFor="register-name">
          Full name
        </label>
        <input
          id="register-name"
          className="input"
          required
          maxLength={150}
          autoComplete="name"
          value={form.fullName}
          onChange={update('fullName')}
        />
      </div>

      <div>
        <label className="label" htmlFor="register-email">
<<<<<<< HEAD
          {isStudent ? 'Parent / personal email address' : 'Personal email address'}
=======
          Your email address
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        </label>
        <input
          id="register-email"
          type="email"
          className="input"
          required
          autoComplete="email"
          value={form.email}
          onChange={update('email')}
          placeholder="you@example.com"
        />
        <p className="mt-1 text-xs text-slate-500">
<<<<<<< HEAD
          {isStudent
            ? 'The school will send registration updates and sign-in details to this address.'
            : 'The school will send your registration updates and sign-in details here.'}
=======
          Where the school will send your school sign-in address and temporary password once your
          registration is approved.
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        </p>
      </div>

      <div>
<<<<<<< HEAD
        <label className="label" htmlFor="register-phone">
          {isStudent ? 'Guardian mobile number' : 'Teacher mobile number'}
        </label>
        <input
          id="register-phone"
          type="tel"
          className="input"
          required
          maxLength={30}
          autoComplete="tel"
          value={form.phoneNumber}
          onChange={update('phoneNumber')}
        />
      </div>

      <div>
=======
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        <label className="label" htmlFor="register-role">
          Applying as
        </label>
        <select
          id="register-role"
          className="input"
          required
          value={form.requestedRole}
<<<<<<< HEAD
          onChange={updateRole}
=======
          onChange={update('requestedRole')}
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        >
          <option value="Student">Student</option>
          <option value="Teacher">Teacher</option>
        </select>
      </div>

<<<<<<< HEAD
      {isStudent ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="label" htmlFor="register-grade">
              Grade
            </label>
            <select
              id="register-grade"
              className="input"
              required
              value={form.gradeLevelId}
              onChange={(event) => {
                setForm((previous) => ({
                  ...previous,
                  gradeLevelId: event.target.value,
                  sectionId: '',
                }));
              }}
            >
              <option value="">Choose…</option>
              {gradeLevels.map((grade) => (
                <option key={grade.id} value={grade.id}>
                  {grade.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="label" htmlFor="register-section">
              Section
            </label>
            <select
              id="register-section"
              className="input"
              required
              value={form.sectionId}
              onChange={update('sectionId')}
            >
              <option value="">Choose…</option>
              {availableSections.map((section) => (
                <option key={section.id} value={section.id}>
                  {section.name}
                </option>
              ))}
            </select>
            <p className="mt-1 text-xs text-slate-500">The school may move you to another section.</p>
          </div>
        </div>
      ) : (
        <div>
          <label className="label" htmlFor="register-department">
            Department / subject
          </label>
          <input
            id="register-department"
            className="input"
            required
            maxLength={120}
            value={form.departmentSubject}
            onChange={update('departmentSubject')}
            placeholder="e.g. Mathematics"
          />
        </div>
      )}
=======
      {form.requestedRole === 'Student' && <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="label" htmlFor="register-grade">
            Grade
          </label>
          <select
            id="register-grade"
            className="input"
            required
            value={form.gradeLevelId}
            onChange={update('gradeLevelId')}
          >
            <option value="">Choose…</option>
            {gradeLevels.map((grade) => (
              <option key={grade.id} value={grade.id}>
                {grade.name}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="label" htmlFor="register-section">
            Section
          </label>
          <select
            id="register-section"
            className="input"
            required
            value={form.sectionId}
            onChange={update('sectionId')}
          >
            <option value="">Choose…</option>
            {sections.map((section) => (
              <option key={section.id} value={section.id}>
                {section.name}
              </option>
            ))}
          </select>
          <p className="mt-1 text-xs text-slate-500">The school may move you to another section.</p>
        </div>
      </div>}
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478

      <div>
        <ProfilePhotoField
          file={photo}
          onChange={(nextPhoto) => {
            setPhoto(nextPhoto);
            if (nextPhoto) {
              setPhotoError('');
            }
          }}
          onPendingChange={(isPending) => {
            setPhotoPending(isPending);
            if (!isPending && photo) {
              setPhotoError('');
            }
          }}
          disabled={submitting}
        />
<<<<<<< HEAD
        {!photo && <span className="sr-only">A profile photo is required.</span>}
=======
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
        {photoError && (
          <p role="alert" className="mt-2 text-sm font-medium text-red-700">{photoError}</p>
        )}
      </div>

      <p className="rounded-lg border border-brand-100 bg-brand-50 px-3 py-2 text-sm text-brand-800">
<<<<<<< HEAD
        A JPG or PNG profile photo (up to 2 MB) is required. It will be attached to your account
        and Smart ID when your registration is approved.
=======
        A unique Smart ID is generated automatically when your registration is approved. You can
        include your optional photo now; it will be attached to your account and Smart ID if approved.
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
      </p>

      <button type="submit" className="btn-primary w-full" disabled={submitting}>
        {submitting ? <Spinner className="size-4" /> : <UserPlus className="size-4" aria-hidden="true" />}
        {submitting ? 'Submitting…' : 'Submit registration'}
      </button>
    </form>
  );
}
