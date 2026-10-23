import { useCallback, useEffect, useState } from 'react';
import { Save, Settings } from 'lucide-react';
import { schoolApi, systemSettingsApi } from '../../services/endpoints';
import { extractErrorMessage } from '../../services/api';
import { useSchoolInfo } from '../../context/SchoolInfoContext';
import { useAuth } from '../../context/AuthContext';
import { Alert, ErrorState, LoadingPanel, Spinner } from '../../components/ui/Feedback';

/** Kestrel's request body limit caps this; the API validates 1 – 28 MB. */
const MAX_UPLOAD_CEILING_MB = 28;

export default function AdminSettings() {
  const { refresh: refreshSchoolInfo } = useSchoolInfo();
  const { user, hasRole } = useAuth();
  const isSuperAdmin = hasRole('SuperAdmin');

  const [form, setForm] = useState(null);
  const [systemAcademicYear, setSystemAcademicYear] = useState('');
  const [lastUpdatedAt, setLastUpdatedAt] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [notice, setNotice] = useState(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const [data, tenant] = await Promise.all([
        isSuperAdmin ? systemSettingsApi.get() : Promise.resolve(null),
        user?.schoolId ? schoolApi.getTenant(user.schoolId) : Promise.resolve(null),
      ]);
      const branding = user?.schoolId ? await schoolApi.getBrandingSettings() : null;
      setForm({
        schoolName: tenant?.schoolName ?? data?.schoolName ?? '',
        contactEmail: data?.contactEmail ?? '',
        academicYear: branding?.academicYear ?? data?.academicYear ?? '',
        heroSubtitle: branding?.heroSubtitle ?? '',
        heroDescription: branding?.heroDescription ?? '',
        primaryColor: branding?.primaryColor ?? '#1e3a8a',
        passMarkPercentage: data?.passMarkPercentage ?? 50,
        maxUploadSizeMb: data?.maxUploadSizeMb ?? 25,
        allowSelfRegistration: Boolean(data?.allowSelfRegistration),
      });
      setSystemAcademicYear(data?.academicYear ?? '');
      setLastUpdatedAt(data?.lastUpdatedAt ?? null);
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [isSuperAdmin, user?.schoolId]);

  useEffect(() => {
    load();
  }, [load]);

  const update = (key) => (event) => {
    const value = event.target.type === 'checkbox' ? event.target.checked : event.target.value;
    setForm((prev) => ({ ...prev, [key]: value }));
  };

  async function submit(event) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setNotice(null);

    try {
      if (!user?.schoolId) {
        throw new Error('This account is not associated with a school tenant.');
      }
      const saved = isSuperAdmin
        ? await systemSettingsApi.update({
            // An empty string clears the address; the API stores it as null.
            contactEmail: form.contactEmail.trim(),
            academicYear: systemAcademicYear,
            passMarkPercentage: Number(form.passMarkPercentage),
            maxUploadSizeMb: Number(form.maxUploadSizeMb),
            allowSelfRegistration: form.allowSelfRegistration,
          })
        : null;

      if (user?.schoolId) {
        await schoolApi.updateBrandingSettings({
          heroSubtitle: form.heroSubtitle.trim(),
          heroDescription: form.heroDescription.trim(),
          primaryColor: form.primaryColor.trim(),
          academicYear: form.academicYear.trim(),
        });
      }
      await schoolApi.updateName(user.schoolId, form.schoolName.trim());
      if (saved) setLastUpdatedAt(saved.lastUpdatedAt);
      setNotice('Settings saved. They take effect on the next request, with no restart needed.');

      await refreshSchoolInfo();
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <LoadingPanel label="Loading settings…" />;
  if (!form) {
    return <ErrorState title="Could not load settings" message={error} onRetry={load} />;
  }

  return (
    /* Rendered as a section of the Settings page rather than a page of its own, which is why
 the heading is an h2 and the width is left to the page. */
    <div className="space-y-4">
      <header>
        <h2 className="flex items-center gap-2 text-lg font-bold text-slate-900 ">
          <Settings className="size-5 text-brand-600 " aria-hidden="true" />
          School settings
        </h2>
        <p className="text-sm text-slate-500 ">
          Update school identity, portal branding, and the active academic year for this tenant.
        </p>
      </header>

      {error && (
        <Alert variant="error" title="Could not save" onDismiss={() => setError(null)}>
          {error}
        </Alert>
      )}

      {notice && (
        <Alert variant="success" onDismiss={() => setNotice(null)}>
          {notice}
        </Alert>
      )}

      <form onSubmit={submit} className="card space-y-5 p-6">
        <div className="grid gap-5 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <label className="label" htmlFor="schoolName">
              School name
            </label>
            <input
              id="schoolName"
              className="input"
              maxLength={200}
              required
              value={form.schoolName}
              onChange={update('schoolName')}
            />
            <p className="mt-1.5 text-xs text-slate-500 ">
              Shown in the portal header, the login screen and printed reports.
            </p>
          </div>

          <div>
            <label className="label" htmlFor="heroSubtitle">
              Portal subtitle
            </label>
            <input
              id="heroSubtitle"
              className="input"
              maxLength={200}
              required
              value={form.heroSubtitle}
              onChange={update('heroSubtitle')}
              placeholder="Nursery through Grade 12, one portal."
            />
          </div>

          <div>
            <label className="label" htmlFor="academicYear">
              School academic year
            </label>
            <input
              id="academicYear"
              className="input"
              pattern="[0-9]{4}-[0-9]{4}"
              required
              value={form.academicYear}
              onChange={update('academicYear')}
              placeholder="2026-2027"
            />
            <p className="mt-1.5 text-xs text-slate-500 ">
              Two consecutive years, for example 2026-2027.
            </p>
          </div>

          <div className="sm:col-span-2">
            <label className="label" htmlFor="heroDescription">
              Hero description
            </label>
            <textarea
              id="heroDescription"
              className="input min-h-24"
              maxLength={2000}
              required
              value={form.heroDescription}
              onChange={update('heroDescription')}
            />
          </div>

          <div className="sm:col-span-2">
            <label className="label" htmlFor="primaryColor">
              Hero theme color
            </label>
            <div className="flex max-w-sm items-center gap-3">
              <input
                id="primaryColor"
                type="color"
                className="h-11 w-14 cursor-pointer rounded border border-slate-300 p-1"
                value={/^#[0-9A-Fa-f]{6}$/.test(form.primaryColor) ? form.primaryColor : '#1e3a8a'}
                onChange={update('primaryColor')}
                aria-label="Choose hero theme color"
              />
              <input
                type="text"
                className="input font-mono uppercase"
                pattern="#[0-9A-Fa-f]{6}"
                maxLength={7}
                required
                value={form.primaryColor}
                onChange={update('primaryColor')}
                aria-label="Hero theme color hex value"
              />
            </div>
          </div>

          {isSuperAdmin && (
            <>
              <div>
                <label className="label" htmlFor="contactEmail">
                  Public contact email
                </label>
                <input
                  id="contactEmail"
                  type="email"
                  className="input"
                  maxLength={256}
                  value={form.contactEmail}
                  onChange={update('contactEmail')}
                  placeholder="info@sms.edu"
                />
                <p className="mt-1.5 text-xs text-slate-500 ">Leave blank to remove it.</p>
              </div>

              <div>
                <label className="label" htmlFor="passMarkPercentage">
                  Pass mark (%)
                </label>
                <input
                  id="passMarkPercentage"
                  type="number"
                  min="0"
                  max="100"
                  step="0.5"
                  className="input"
                  required
                  value={form.passMarkPercentage}
                  onChange={update('passMarkPercentage')}
                />
                <p className="mt-1.5 text-xs text-slate-500 ">
                  Minimum weighted total, out of 100, needed to pass a subject.
                </p>
              </div>

              <div>
                <label className="label" htmlFor="maxUploadSizeMb">
                  Maximum upload size (MB)
                </label>
                <input
                  id="maxUploadSizeMb"
                  type="number"
                  min="1"
                  max={MAX_UPLOAD_CEILING_MB}
                  step="1"
                  className="input"
                  required
                  value={form.maxUploadSizeMb}
                  onChange={update('maxUploadSizeMb')}
                />
                <p className="mt-1.5 text-xs text-slate-500 ">
                  Capped at {MAX_UPLOAD_CEILING_MB} MB by the server's request body limit.
                </p>
              </div>
            </>
          )}
        </div>

        {isSuperAdmin && (
          <label className="flex items-start gap-3 rounded-lg border border-slate-200 p-4">
            <input
              type="checkbox"
              className="mt-0.5 size-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500 "
              checked={form.allowSelfRegistration}
              onChange={update('allowSelfRegistration')}
            />
            <span>
              <span className="block text-sm font-medium text-slate-800 ">
                Allow students to register themselves
              </span>
              <span className="block text-xs text-slate-500 ">
                When off, only an administrator can create accounts and the public registration
                endpoint returns 403.
              </span>
            </span>
          </label>
        )}

        <div className="flex items-center justify-between border-t border-slate-200 pt-5">
          <p className="text-xs text-slate-500 ">
            {lastUpdatedAt
              ? `Last changed ${new Date(lastUpdatedAt).toLocaleString()}`
              : 'Never changed'}
          </p>
          <button type="submit" className="btn-primary" disabled={saving}>
            {saving ? (
              <Spinner className="size-4" />
            ) : (
              <Save className="size-4" aria-hidden="true" />
            )}
            Save settings
          </button>
        </div>
      </form>
    </div>
  );
}
