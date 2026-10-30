import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, School } from 'lucide-react';
import { schoolApi } from '../services/endpoints';
import { extractErrorMessage } from '../services/api';
import { Alert, LoadingPanel } from '../components/ui/Feedback';
import StudentRegistrationForm from '../components/auth/StudentRegistrationForm';

export default function SchoolRegistrationPage() {
  const { schoolSlug } = useParams();
  const [school, setSchool] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [receipt, setReceipt] = useState('');
  const [registrationRole, setRegistrationRole] = useState('Student');

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
                  Registration
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
              <div className="mt-7">
                {error && (
                  <Alert variant="error" title="Registration failed" onDismiss={() => setError('')}>
                    {error}
                  </Alert>
                )}
                <p className="rounded-xl border border-brand-100 bg-brand-50 p-3 text-sm leading-6 text-brand-900 ">
                  Your application will be sent only to {school.name}. The school will contact you
                  after reviewing it.
                </p>
                <StudentRegistrationForm
                  gradeLevels={school.grades}
                  initialRole={registrationRole}
                  onRoleChange={setRegistrationRole}
                  onSubmitRegistration={(payload) => schoolApi.registerBySlug(schoolSlug, payload)}
                  onSubmitted={(message) => setReceipt(message ?? 'Your application has been submitted for review.')}
                />
              </div>
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
