import { useCallback, useEffect, useState } from 'react';
import { Check, KeyRound, RefreshCw, X } from 'lucide-react';
import { extractErrorMessage } from '../../services/api';
import { passwordResetAdminApi } from '../../services/endpoints';
import { Alert, Badge, EmptyState, Spinner } from '../ui/Feedback';

export default function PendingPasswordResets() {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadRequests = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const result = await passwordResetAdminApi.pending();
      setRequests(Array.isArray(result) ? result : []);
    } catch (loadError) {
      setError(loadError.friendlyMessage ?? extractErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadRequests();
  }, [loadRequests]);

  async function processRequest(request, approve) {
    setBusyId(request.id);
    setError('');
    setNotice('');
    try {
      if (approve) {
        const result = await passwordResetAdminApi.approve(request.id);
        setNotice(result.message);
      } else {
        await passwordResetAdminApi.reject(request.id);
        setNotice(`The password reset request for ${request.fullName} was rejected.`);
      }
      setRequests((current) => current.filter((item) => item.id !== request.id));
    } catch (actionError) {
      setError(actionError.friendlyMessage ?? extractErrorMessage(actionError));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <section className="card">
      <div className="flex flex-wrap items-center gap-3 border-b border-slate-200 px-5 py-4">
        <KeyRound className="size-5 text-brand-700" aria-hidden="true" />
        <div>
          <h2 className="font-semibold text-slate-900">Admin Pending Password Resets</h2>
          <p className="text-sm text-slate-500">Review account reset requests from students and teachers.</p>
        </div>
        <button type="button" className="btn-secondary ml-auto" onClick={loadRequests} disabled={loading}>
          {loading ? <Spinner className="size-4" /> : <RefreshCw className="size-4" aria-hidden="true" />}
          Refresh
        </button>
      </div>

      <div className="space-y-3 p-5">
        {error && <Alert variant="error" title="Could not process password resets">{error}</Alert>}
        {notice && <Alert variant="success">{notice}</Alert>}
        {loading ? (
          <div className="flex justify-center py-5"><Spinner label="Loading requests…" /></div>
        ) : requests.length === 0 ? (
          <EmptyState title="No pending password resets" description="New requests will appear here for review." />
        ) : (
          requests.map((request) => (
            <article
              key={request.id}
              className="flex flex-col gap-3 rounded-lg border border-slate-200 p-4 sm:flex-row sm:items-center"
            >
              <div className="min-w-0 flex-1">
                <p className="font-semibold text-slate-900">{request.fullName}</p>
                <p className="break-all text-sm text-slate-600">{request.email}</p>
                <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-slate-500">
                  <Badge tone={request.role === 'Teacher' ? 'brand' : 'green'}>{request.role}</Badge>
                  <span>Requested {new Date(request.requestDate).toLocaleString()}</span>
                </div>
              </div>
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  className="btn-primary"
                  onClick={() => processRequest(request, true)}
                  disabled={busyId !== null}
                >
                  {busyId === request.id ? <Spinner className="size-4" /> : <Check className="size-4" aria-hidden="true" />}
                  Approve Reset
                </button>
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={() => processRequest(request, false)}
                  disabled={busyId !== null}
                >
                  <X className="size-4" aria-hidden="true" />
                  Reject Request
                </button>
              </div>
            </article>
          ))
        )}
      </div>
    </section>
  );
}
