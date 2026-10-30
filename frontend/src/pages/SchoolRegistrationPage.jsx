import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
<<<<<<< HEAD
import { ArrowLeft, School } from 'lucide-react';
import { schoolApi } from '../services/endpoints';
import { extractErrorMessage } from '../services/api';
import { Alert, LoadingPanel } from '../components/ui/Feedback';
import StudentRegistrationForm from '../components/auth/StudentRegistrationForm';
=======
import { ArrowLeft, School, UserPlus } from 'lucide-react';
import { schoolApi } from '../services/endpoints';
import { extractErrorMessage } from '../services/api';
import { Alert, LoadingPanel, Spinner } from '../components/ui/Feedback';
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478

export default function SchoolRegistrationPage() {
  const { schoolSlug } = useParams();
  const [school, setSchool] = useState(null);
<<<<<<< HEAD
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [receipt, setReceipt] = useState('');
  const [registrationRole, setRegistrationRole] = useState('Student');
=======
  const [form, setForm] = useState({ fullName: '', email: '', gradeLevelId: '', sectionId: '' });
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [receipt, setReceipt] = useState('');
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    schoolApi
      .getRegistrationInfo(schoolSlug)
      .then((result) => {
        if (!cancelled) setSchool(result);
      })
      .catch((loadError) => {
        if (!cancelled) setError(loadError.friendlyMessage ?? extractErrorMessage(loadError));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [schoolSlug]);

<<<<<<< HEAD
=======
  const selectedGrade = school?.grades?.find((grade) => String(grade.id) === form.gradeLevelId);

  function update(event) {
    const { name, value } = event.target;
    setForm((current) => ({
      ...current,
      [name]: value,
      ...(name === 'gradeLevelId' ? { sectionId: '' } : {}),
    }));
    setError('');
  }

  async function submit(event) {
    event.preventDefault();
    setError('');

    const fullName = form.fullName.trim();
    const email = form.email.trim();
    const gradeLevelId = Number(form.gradeLevelId);
    const sectionId = Number(form.sectionId);
    if (!fullName || !email || !Number.isInteger(gradeLevelId) || gradeLevelId < 1
        || !Number.isInteger(sectionId) || sectionId < 1) {
      setError('Enter your name and email, then choose a grade and section.');
      return;
    }

    setSubmitting(true);
    try {
      const result = await schoolApi.registerBySlug(schoolSlug, {
        requestedRole: 'Student',
        fullName,
        email,
        gradeLevelId,
        sectionId,
      });
      setReceipt(result.message ?? 'Your application has been submitted for review.');
    } catch (submitError) {
      setError(submitError.friendlyMessage ?? extractErrorMessage(submitError));
    } finally {
      setSubmitting(false);
    }
  }

>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
  if (loading) return <LoadingPanel label="Loading school registration…" />;

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-100 px-4 py-10">
      <section className="w-full max-w-xl rounded-3xl border border-slate-200 bg-white p-6 shadow-sm sm:p-9">
        <Link
          to="/login"
          className="inline-flex items-center gap-2 text-sm font-semibold text-slate-500 hover:text-brand-700 "
        >
          <ArrowLeft className="size-4" aria-hidden="true" />
          Back to sign in
        </Link>
        {school ? (
          <>
            <div className="mt-6 flex items-center gap-3">
              <span className="grid size-11 place-items-center rounded-xl bg-brand-50 text-brand-700 ">
                <School className="size-6" aria-hidden="true" />
              </span>
              <div>
                <p className="text-xs font-bold uppercase tracking-wider text-brand-700 ">
<<<<<<< HEAD
                  Registration
=======
                  Student registration
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
                </p>
                <h1 className="text-xl font-bold text-slate-900 ">{school.name}</h1>
              </div>
            </div>

            {receipt ? (
              <Alert variant="success" title="Application received" className="mt-6">
                {receipt}
              </Alert>
            ) : !school.registrationOpen ? (
              <Alert variant="info" title="Registration is closed" className="mt-6">
                Contact the school administration for enrollment information.
              </Alert>
            ) : error && !school ? (
              <Alert variant="error" title="School unavailable" className="mt-6">
                {error}
              </Alert>
            ) : (
<<<<<<< HEAD
              <div className="mt-7">
=======
              <form onSubmit={submit} className="mt-7 space-y-4">
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
                {error && (
                  <Alert variant="error" title="Registration failed" onDismiss={() => setError('')}>
                    {error}
                  </Alert>
                )}
<<<<<<< HEAD
=======
                <div>
                  <label className="label" htmlFor="public-register-name">
                    Full name
                  </label>
                  <input
                    id="public-register-name"
                    name="fullName"
                    className="input"
                    value={form.fullName}
                    onChange={update}
                    required
                    maxLength={150}
                    autoComplete="name"
                  />
                </div>
                <div>
                  <label className="label" htmlFor="public-register-email">
                    Email address
                  </label>
                  <input
                    id="public-register-email"
                    name="email"
                    type="email"
                    className="input"
                    value={form.email}
                    onChange={update}
                    required
                    maxLength={256}
                    autoComplete="email"
                  />
                </div>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="label" htmlFor="public-register-grade">
                      Grade
                    </label>
                    <select
                      id="public-register-grade"
                      name="gradeLevelId"
                      className="input"
                      value={form.gradeLevelId}
                      onChange={update}
                      required
                    >
                      <option value="">Choose a grade</option>
                      {school.grades.map((grade) => (
                        <option key={grade.id} value={grade.id}>
                          {grade.name}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="label" htmlFor="public-register-section">
                      Section
                    </label>
                    <select
                      id="public-register-section"
                      name="sectionId"
                      className="input"
                      value={form.sectionId}
                      onChange={update}
                      required
                      disabled={!selectedGrade}
                    >
                      <option value="">Choose a section</option>
                      {(selectedGrade?.sections ?? []).map((section) => (
                        <option key={section.id} value={section.id}>
                          {section.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
                <p className="rounded-xl border border-brand-100 bg-brand-50 p-3 text-sm leading-6 text-brand-900 ">
                  Your application will be sent only to {school.name}. The school will contact you
                  after reviewing it.
                </p>
<<<<<<< HEAD
                <StudentRegistrationForm
                  gradeLevels={school.grades}
                  initialRole={registrationRole}
                  onRoleChange={setRegistrationRole}
                  onSubmitRegistration={(payload) => schoolApi.registerBySlug(schoolSlug, payload)}
                  onSubmitted={(message) => setReceipt(message ?? 'Your application has been submitted for review.')}
                />
              </div>
=======
                <button
                  type="submit"
                  className="btn-primary w-full"
                  disabled={submitting || school.grades.length === 0}
                >
                  {submitting ? (
                    <Spinner className="size-4" />
                  ) : (
                    <UserPlus className="size-4" aria-hidden="true" />
                  )}
                  {submitting ? 'Submitting…' : 'Submit application'}
                </button>
              </form>
>>>>>>> e397352cf2d5f56e7041bfcb548de595c76a9478
            )}
          </>
        ) : (
          <Alert variant="error" title="School registration unavailable" className="mt-6">
            {error || 'This school registration page could not be found.'}
          </Alert>
        )}
      </section>
    </main>
  );
}
