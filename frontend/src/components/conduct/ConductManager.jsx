import { useCallback, useEffect, useState } from 'react';
import { Award, Check, Clock3, Eye, EyeOff, Plus, ShieldAlert, Trash2, X } from 'lucide-react';
import { Alert, Spinner } from '../ui/Feedback';
import { CONDUCT_TYPES, conductApi } from '../../services/conduct';
import { extractErrorMessage } from '../../services/api';

const FILTERS = ['All', 'Merit', 'Disciplinary'];

function todayAsDateInputValue() {
  const today = new Date();
  const month = String(today.getMonth() + 1).padStart(2, '0');
  const day = String(today.getDate()).padStart(2, '0');
  return `${today.getFullYear()}-${month}-${day}`;
}

function visibleInFilter(log, filter) {
  if (filter === 'Merit') return log.type === 'Merit';
  if (filter === 'Disciplinary') return log.type === 'Demerit' || log.type === 'Disciplinary Note';
  return true;
}

function formatDate(value) {
  if (!value) return 'Date unavailable';
  const date = new Date(value.length === 10 ? `${value}T00:00:00` : value);
  return Number.isNaN(date.getTime())
    ? 'Date unavailable'
    : new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(date);
}

function ConductLog({ log, onDelete, deleting }) {
  const merit = log.type === 'Merit';
  return (
    <article className="rounded-xl border border-slate-200 bg-white p-4">
      <div className="flex flex-wrap items-start gap-3">
        <span className={`mt-0.5 rounded-lg p-2 ${merit ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'}`}>
          {merit ? <Award className="size-4" aria-hidden="true" /> : <ShieldAlert className="size-4" aria-hidden="true" />}
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-semibold text-slate-900">{log.title}</h3>
            <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${merit ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-800'}`}>
              {log.type}
            </span>
          </div>
          <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-slate-600">{log.description}</p>
          <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-500">
            <span className="inline-flex items-center gap-1"><Clock3 className="size-3.5" aria-hidden="true" />{formatDate(log.incidentDate)}</span>
            {(log.recordedByName || log.recordedByRole) && (
              <span>
                Logged by: {log.recordedByName || 'School staff'}
                {log.recordedByRole ? ` (${log.recordedByRole})` : ''}
              </span>
            )}
            <span className="inline-flex items-center gap-1">
              {log.isVisibleToStudent ? <Eye className="size-3.5" aria-hidden="true" /> : <EyeOff className="size-3.5" aria-hidden="true" />}
              Student {log.isVisibleToStudent ? 'visible' : 'private'}
            </span>
            <span className="inline-flex items-center gap-1">
              {log.isVisibleToParent ? <Eye className="size-3.5" aria-hidden="true" /> : <EyeOff className="size-3.5" aria-hidden="true" />}
              Parent {log.isVisibleToParent ? 'visible' : 'private'}
            </span>
          </div>
        </div>
        {onDelete && (
          <button
            type="button"
            className="btn-secondary !px-2.5 !py-2 text-rose-700"
            onClick={() => onDelete(log)}
            disabled={deleting}
            aria-label={`Delete ${log.title}`}
            title="Delete conduct record"
          >
            {deleting ? <Spinner className="size-4" /> : <Trash2 className="size-4" aria-hidden="true" />}
          </button>
        )}
      </div>
    </article>
  );
}

export function ConductSummary({ logs }) {
  const merits = logs.filter((log) => log.type === 'Merit').length;
  const disciplinary = logs.filter((log) => log.type === 'Demerit' || log.type === 'Disciplinary Note').length;
  return (
    <div className="grid grid-cols-3 gap-2 sm:gap-3">
      {[
        ['Total records', logs.length, 'bg-slate-50 text-slate-900'],
        ['Merits', merits, 'bg-emerald-50 text-emerald-800'],
        ['Disciplinary', disciplinary, 'bg-amber-50 text-amber-900'],
      ].map(([label, count, tone]) => (
        <div key={label} className={`rounded-xl p-3 sm:p-4 ${tone}`}>
          <p className="text-xs font-medium sm:text-sm">{label}</p>
          <p className="mt-1 text-xl font-bold sm:text-2xl">{count}</p>
        </div>
      ))}
    </div>
  );
}

export function ConductHistory({ logs, filter, onFilterChange, onDelete, deletingId, emptyText }) {
  const visible = logs.filter((log) => visibleInFilter(log, filter));
  return (
    <section aria-label="Conduct history">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <h3 className="font-semibold text-slate-900">History</h3>
        <div className="flex flex-wrap gap-2" role="group" aria-label="Filter conduct history">
          {FILTERS.map((item) => (
            <button
              key={item}
              type="button"
              onClick={() => onFilterChange(item)}
              aria-pressed={filter === item}
              className={`rounded-full px-3 py-1.5 text-sm font-medium transition ${filter === item ? 'bg-brand-700 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}
            >
              {item}
            </button>
          ))}
        </div>
      </div>
      {visible.length ? (
        <div className="space-y-3">
          {visible.map((log) => (
            <ConductLog
              key={log.id}
              log={log}
              onDelete={onDelete}
              deleting={deletingId === log.id}
            />
          ))}
        </div>
      ) : (
        <p className="rounded-xl border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500">{emptyText}</p>
      )}
    </section>
  );
}

function ConductEntryForm({ onCreated }) {
  const [form, setForm] = useState({
    type: 'Merit',
    title: '',
    description: '',
    incidentDate: todayAsDateInputValue(),
    isVisibleToStudent: true,
    isVisibleToParent: true,
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  function update(key, value) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function submit(event) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await onCreated({ ...form, title: form.title.trim(), description: form.description.trim() });
      setForm((current) => ({ ...current, title: '', description: '' }));
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <form onSubmit={submit} className="space-y-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
      <h3 className="flex items-center gap-2 font-semibold text-slate-900">
        <Plus className="size-4" aria-hidden="true" /> Record conduct
      </h3>
      {error && <Alert variant="error" title="Could not save record">{error}</Alert>}
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="block text-sm font-medium text-slate-700">
          Record type
          <select className="input mt-1 w-full" value={form.type} onChange={(event) => update('type', event.target.value)}>
            {CONDUCT_TYPES.map((type) => <option key={type}>{type}</option>)}
          </select>
        </label>
        <label className="block text-sm font-medium text-slate-700">
          Incident date
          <input className="input mt-1 w-full" type="date" required value={form.incidentDate} onChange={(event) => update('incidentDate', event.target.value)} />
        </label>
      </div>
      <label className="block text-sm font-medium text-slate-700">
        Title
        <input className="input mt-1 w-full" maxLength={160} required value={form.title} onChange={(event) => update('title', event.target.value)} />
      </label>
      <label className="block text-sm font-medium text-slate-700">
        Description
        <textarea className="input mt-1 min-h-24 w-full resize-y" maxLength={4000} required value={form.description} onChange={(event) => update('description', event.target.value)} />
      </label>
      <div className="flex flex-wrap gap-x-6 gap-y-3">
        {[
          ['isVisibleToStudent', 'Visible to student'],
          ['isVisibleToParent', 'Visible to parent'],
        ].map(([key, label]) => (
          <label key={key} className="inline-flex cursor-pointer items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              className="size-4 accent-brand-700"
              checked={form[key]}
              onChange={(event) => update(key, event.target.checked)}
            />
            {label}
          </label>
        ))}
      </div>
      <div className="flex justify-end">
        <button type="submit" className="btn-primary" disabled={saving}>
          {saving ? <Spinner className="size-4" /> : <Check className="size-4" aria-hidden="true" />}
          Save record
        </button>
      </div>
    </form>
  );
}

export default function ConductManager({ student, onClose }) {
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState('All');
  const [deletingId, setDeletingId] = useState(null);

  const loadLogs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setLogs(await conductApi.listForStudent(student.id));
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [student.id]);

  useEffect(() => {
    loadLogs();
  }, [loadLogs]);

  const counts = logs.length;

  async function createRecord(payload) {
    await conductApi.create({ studentId: Number(student.id), ...payload });
    await loadLogs();
  }

  async function deleteRecord(log) {
    setDeletingId(log.id);
    setError(null);
    try {
      await conductApi.remove(log.id);
      setLogs((current) => current.filter((entry) => entry.id !== log.id));
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-slate-950/50 p-0 sm:items-center sm:p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}>
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="conduct-manager-title"
        className="flex max-h-[95vh] w-full max-w-3xl flex-col overflow-hidden rounded-t-2xl bg-white shadow-2xl sm:rounded-2xl"
      >
        <header className="flex items-start gap-4 border-b border-slate-200 px-5 py-4 sm:px-6">
          <span className="rounded-xl bg-brand-50 p-2.5 text-brand-700"><ShieldAlert className="size-5" aria-hidden="true" /></span>
          <div className="min-w-0 flex-1">
            <h2 id="conduct-manager-title" className="text-lg font-bold text-slate-900">Student conduct</h2>
            <p className="truncate text-sm text-slate-500">{student.fullName} · {student.studentIdNumber ?? `Student #${student.id}`}</p>
          </div>
          <button type="button" className="rounded-lg p-2 text-slate-500 hover:bg-slate-100" onClick={onClose} aria-label="Close conduct records">
            <X className="size-5" aria-hidden="true" />
          </button>
        </header>
        <div className="space-y-5 overflow-y-auto p-4 sm:p-6">
          <ConductSummary logs={logs} />
          <ConductEntryForm onCreated={createRecord} />
          {error && <Alert variant="error" title="Conduct history issue" onDismiss={() => setError(null)}>{error}</Alert>}
          {loading ? (
            <div className="flex justify-center py-8"><Spinner label="Loading conduct history…" /></div>
          ) : (
            <ConductHistory
              logs={logs}
              filter={filter}
              onFilterChange={setFilter}
              onDelete={deleteRecord}
              deletingId={deletingId}
              emptyText={counts ? 'No records in this category.' : 'No conduct records have been added for this student.'}
            />
          )}
        </div>
      </section>
    </div>
  );
}
