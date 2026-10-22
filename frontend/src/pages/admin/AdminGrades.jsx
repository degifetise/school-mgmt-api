import { useCallback, useEffect, useState } from 'react';
import { Layers, Save } from 'lucide-react';
import { schoolGradeConfigurationApi } from '../../services/endpoints';
import { extractErrorMessage } from '../../services/api';
import { Alert, EmptyState, ErrorState, LoadingPanel, Spinner } from '../../components/ui/Feedback';

export default function AdminGrades() {
  const [grades, setGrades] = useState([]);
  const [supportedGrades, setSupportedGrades] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const config = await schoolGradeConfigurationApi.get();
      setGrades(config.grades.map((grade) => ({
        ...grade,
        sectionIds: grade.sectionIds ?? [],
        availableSections: grade.availableSections ?? [],
      })));
      setSupportedGrades(config.supportedGrades ?? []);
    } catch (loadError) {
      setError(loadError.friendlyMessage ?? extractErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  function toggleGrade(gradeLevelId) {
    setGrades((current) => current.map((grade) => grade.gradeLevelId === gradeLevelId
      ? { ...grade, isActive: !grade.isActive }
      : grade));
  }

  function toggleSection(gradeLevelId, sectionId) {
    setGrades((current) => current.map((grade) => {
      if (grade.gradeLevelId !== gradeLevelId) return grade;
      const sectionIds = grade.sectionIds.includes(sectionId)
        ? grade.sectionIds.filter((id) => id !== sectionId)
        : [...grade.sectionIds, sectionId];
      return { ...grade, sectionIds };
    }));
  }

  async function save(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    setNotice('');
    try {
      const saved = await schoolGradeConfigurationApi.update(grades.map((grade) => ({
        gradeLevelId: grade.gradeLevelId,
        isActive: grade.isActive,
        sectionIds: grade.isActive ? grade.sectionIds : [],
      })));
      setGrades(saved.grades.map((grade) => ({
        ...grade,
        sectionIds: grade.sectionIds ?? [],
        availableSections: grade.availableSections ?? [],
      })));
      setSupportedGrades(saved.supportedGrades ?? []);
      setNotice('Grade and section settings saved for this school.');
    } catch (saveError) {
      setError(saveError.friendlyMessage ?? extractErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <LoadingPanel label="Loading grade and section settings…" />;
  if (grades.length === 0 && error) {
    return <ErrorState title="Could not load grades" message={error} onRetry={load} />;
  }

  const activeCount = grades.filter((grade) => grade.isActive).length;

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="flex items-center gap-2 text-xl font-bold text-slate-900">
            <Layers className="size-5 text-brand-600" aria-hidden="true" />
            Manage grades & sections
          </h1>
          <p className="mt-1 text-sm text-slate-600">
            Choose which grades are offered by this school and select the sections available in each grade.
            Student registration and grade selectors use these settings.
          </p>
        </div>
        <p className="rounded-full bg-brand-50 px-3 py-1.5 text-sm font-semibold text-brand-800">
          {activeCount} of {grades.length} grades active
        </p>
      </header>

      {error && <Alert variant="error" title="Could not save grade settings" onDismiss={() => setError('')}>{error}</Alert>}
      {notice && <Alert variant="success" onDismiss={() => setNotice('')}>{notice}</Alert>}

      {grades.length === 0 ? (
        <EmptyState title="No grade levels are configured" description="The grade catalog is empty. Contact your system administrator." />
      ) : (
        <form onSubmit={save} className="space-y-4">
          <div className="grid gap-4 xl:grid-cols-2">
            {grades.map((grade) => (
              <section key={grade.gradeLevelId} className={`card overflow-hidden ${grade.isActive ? '' : 'opacity-80'}`}>
                <div className="flex items-start gap-3 border-b border-slate-100 p-4">
                  <input
                    id={`grade-${grade.gradeLevelId}`}
                    type="checkbox"
                    className="mt-1 size-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                    checked={grade.isActive}
                    onChange={() => toggleGrade(grade.gradeLevelId)}
                  />
                  <label htmlFor={`grade-${grade.gradeLevelId}`} className="min-w-0 flex-1 cursor-pointer">
                    <span className="block font-semibold text-slate-900">{grade.name}</span>
                    <span className="text-xs text-slate-500">Academic level {grade.level}</span>
                  </label>
                  <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                    grade.isActive ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-600'
                  }`}>
                    {grade.isActive ? 'Active' : 'Not offered'}
                  </span>
                </div>
                <fieldset disabled={!grade.isActive} className="space-y-2 p-4">
                  <legend className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Sections available
                  </legend>
                  {grade.availableSections.length === 0 ? (
                    <p className="text-sm text-slate-500">No sections are available in the section catalog.</p>
                  ) : (
                    <div className="flex flex-wrap gap-2">
                      {grade.availableSections.map((section) => (
                        <label
                          key={section.id}
                          className={`inline-flex cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm transition-colors ${
                            grade.sectionIds.includes(section.id)
                              ? 'border-brand-200 bg-brand-50 text-brand-800'
                              : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
                          }`}
                        >
                          <input
                            type="checkbox"
                            className="size-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                            checked={grade.sectionIds.includes(section.id)}
                            onChange={() => toggleSection(grade.gradeLevelId, section.id)}
                          />
                          {section.name}
                        </label>
                      ))}
                    </div>
                  )}
                </fieldset>
              </section>
            ))}
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-xs text-slate-500">
              Active grades: {supportedGrades.join(', ') || 'none'}
            </p>
            <button type="submit" className="btn-primary" disabled={saving}>
              {saving ? <Spinner className="size-4" /> : <Save className="size-4" aria-hidden="true" />}
              Save grade settings
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
