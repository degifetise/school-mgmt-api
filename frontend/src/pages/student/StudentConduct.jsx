import { useCallback, useEffect, useState } from 'react';
import { Award, ShieldCheck, ShieldAlert } from 'lucide-react';
import { Alert, LoadingPanel } from '../../components/ui/Feedback';
import { ConductHistory, ConductSummary } from '../../components/conduct/ConductManager';
import { conductApi } from '../../services/conduct';
import { extractErrorMessage } from '../../services/api';

function standingLabel(standing) {
  if (typeof standing === 'string' || typeof standing === 'number') return String(standing);
  if (standing && typeof standing === 'object') {
    return standing.label ?? standing.name ?? standing.status ?? 'Standing available';
  }
  return 'Not yet assessed';
}

export default function StudentConduct() {
  const [status, setStatus] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState('All');

  const loadStatus = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await conductApi.myStatus();
      setStatus({
        ...result,
        logs: Array.isArray(result?.logs) ? result.logs : [],
      });
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadStatus();
  }, [loadStatus]);

  if (loading) return <LoadingPanel label="Loading your conduct record…" />;

  const logs = status?.logs ?? [];
  const standing = standingLabel(status?.standing);
  const positiveStanding = /good|excellent|commend|positive/i.test(standing);

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <header>
        <p className="text-sm font-semibold tracking-wide text-brand-700 uppercase">Student portal</p>
        <h1 className="mt-1 text-2xl font-bold text-slate-950 sm:text-3xl">My conduct</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
          Review your conduct standing and the school records shared with you.
        </p>
      </header>

      {error && (
        <Alert variant="error" title="Could not load conduct records">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <span>{error}</span>
            <button type="button" className="btn-secondary" onClick={loadStatus}>Try again</button>
          </div>
        </Alert>
      )}

      <section className={`overflow-hidden rounded-2xl border p-5 sm:p-7 ${positiveStanding ? 'border-emerald-200 bg-gradient-to-br from-emerald-50 to-white' : 'border-brand-200 bg-gradient-to-br from-brand-50 to-white'}`}>
        <div className="flex flex-wrap items-center gap-4">
          <span className={`rounded-2xl p-3 ${positiveStanding ? 'bg-emerald-100 text-emerald-700' : 'bg-brand-100 text-brand-700'}`}>
            {positiveStanding ? <Award className="size-7" aria-hidden="true" /> : <ShieldCheck className="size-7" aria-hidden="true" />}
          </span>
          <div className="min-w-0 flex-1">
            <p className="text-sm font-medium text-slate-600">Current standing</p>
            <h2 className="mt-0.5 text-2xl font-bold text-slate-950">{standing}</h2>
          </div>
          <span className="rounded-full bg-white/80 px-3 py-1.5 text-sm font-medium text-slate-600">
            {logs.length} shared {logs.length === 1 ? 'record' : 'records'}
          </span>
        </div>
      </section>

      {status?.message && (
        <div className="flex items-start gap-3 rounded-xl border border-slate-200 bg-white p-4 text-sm text-slate-600">
          <ShieldAlert className="mt-0.5 size-4 shrink-0 text-brand-700" aria-hidden="true" />
          <p>{status.message}</p>
        </div>
      )}

      <section className="card space-y-5 p-4 sm:p-6">
        <div>
          <h2 className="text-lg font-bold text-slate-900">Conduct summary</h2>
          <p className="mt-1 text-sm text-slate-500">Only records your school has marked visible to you are shown.</p>
        </div>
        <ConductSummary logs={logs} />
        <ConductHistory
          logs={logs}
          filter={filter}
          onFilterChange={setFilter}
          emptyText={logs.length ? 'There are no shared records in this category.' : 'No conduct records have been shared with you yet.'}
        />
      </section>
    </div>
  );
}
