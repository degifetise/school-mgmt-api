import { useCallback, useEffect, useState } from 'react';
import {
  AlertTriangle,
  Building2,
  Check,
  Copy,
  Edit3,
  LoaderCircle,
  KeyRound,
  Mail,
  MapPin,
  Phone,
  RefreshCw,
  ShieldCheck,
  UserRoundPlus,
  UserRoundX,
  X,
} from 'lucide-react';
import { superAdminSchoolApi } from '../../services/endpoints';
import { extractErrorMessage } from '../../services/api';
import { Alert } from '../../components/ui/Feedback';

const INITIAL_FORM = {
  schoolName: '',
  schoolCode: '',
  contactEmail: '',
  contactPhone: '',
  address: '',
  adminFullName: '',
  adminEmail: '',
  adminTemporaryPassword: '',
};

const FIELDS = [
  { name: 'schoolName', label: 'School name', placeholder: 'Example High School', required: true },
  {
    name: 'schoolCode',
    label: 'School code',
    placeholder: 'EXAMPLE-HS',
    required: true,
    hint: 'Letters, numbers, and hyphens only.',
    maxLength: 50,
  },
  {
    name: 'contactEmail',
    label: 'Contact email',
    type: 'email',
    placeholder: 'office@example.edu',
    required: true,
  },
  {
    name: 'contactPhone',
    label: 'Contact phone',
    type: 'tel',
    placeholder: '+251-92427-5434',
    required: true,
  },
  {
    name: 'address',
    label: 'School address',
    placeholder: 'Street, city, region',
    required: true,
    wide: true,
  },
  {
    name: 'adminFullName',
    label: 'Primary admin full name',
    placeholder: 'Jordan Lee',
    required: true,
    maxLength: 150,
  },
  {
    name: 'adminEmail',
    label: 'Primary admin email',
    type: 'email',
    placeholder: 'admin@example.edu',
    required: true,
  },
  {
    name: 'adminTemporaryPassword',
    label: 'Temporary password',
    type: 'password',
    placeholder: 'At least 8 characters',
    required: true,
    hint: 'Must include uppercase, lowercase, number, and symbol.',
    maxLength: 100,
  },
];

function instructionsFor(school, temporaryPassword) {
  return [
    'School tenant created successfully',
    `School: ${school.schoolName}`,
    `School code: ${school.schoolCode}`,
    `School ID: ${school.schoolId}`,
    `Student registration: ${window.location.origin}/register/${school.slug}`,
    `School contact: ${school.contactEmail} | ${school.contactPhone}`,
    `Primary administrator: ${school.adminFullName} <${school.adminEmail}>`,
    `Sign in at: ${window.location.origin}/login`,
    `Temporary password: ${temporaryPassword}`,
    'The administrator should change the temporary password after their first sign-in.',
  ].join('\n');
}

function Field({ field, value, onChange }) {
  const inputId = `tenant-${field.name}`;
  const common = {
    id: inputId,
    name: field.name,
    type: field.type ?? 'text',
    value,
    onChange,
    required: field.required,
    maxLength:
      field.maxLength ?? (field.name === 'address' ? 500 : field.name === 'schoolName' ? 200 : 256),
    autoComplete:
      field.name === 'adminTemporaryPassword'
        ? 'new-password'
        : field.name.toLowerCase().includes('email')
          ? 'email'
          : 'off',
    className:
      'mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-500 focus:ring-4 focus:ring-brand-100 ',
    placeholder: field.placeholder,
  };

  return (
    <div className={field.wide ? 'sm:col-span-2' : ''}>
      <label htmlFor={inputId} className="text-sm font-semibold text-slate-700 ">
        {field.label}
      </label>
      <input {...common} />
      {field.hint && <p className="mt-1 text-xs text-slate-500 ">{field.hint}</p>}
    </div>
  );
}

export default function SuperAdminSchools() {
  const [form, setForm] = useState(INITIAL_FORM);
  const [schools, setSchools] = useState([]);
  const [schoolsLoading, setSchoolsLoading] = useState(true);
  const [schoolsError, setSchoolsError] = useState('');
  const [rowAction, setRowAction] = useState('');
  const [assignSchool, setAssignSchool] = useState(null);
  const [renameSchool, setRenameSchool] = useState(null);
  const [schoolName, setSchoolName] = useState('');
  const [renameError, setRenameError] = useState('');
  const [renaming, setRenaming] = useState(false);
  const [adminEmail, setAdminEmail] = useState('');
  const [assignError, setAssignError] = useState('');
  const [assigning, setAssigning] = useState(false);
  const [createdSchool, setCreatedSchool] = useState(null);
  const [createdAdminPassword, setCreatedAdminPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);
  const [copyError, setCopyError] = useState('');

  const loadSchools = useCallback(async () => {
    setSchoolsLoading(true);
    setSchoolsError('');
    try {
      const result = await superAdminSchoolApi.list();
      setSchools(Array.isArray(result) ? result : []);
    } catch (loadError) {
      setSchoolsError(loadError.friendlyMessage ?? extractErrorMessage(loadError));
    } finally {
      setSchoolsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSchools();
  }, [loadSchools]);

  function updateField(event) {
    const { name, value } = event.target;
    setForm((current) => ({
      ...current,
      [name]: name === 'schoolCode' ? value.toUpperCase() : value,
    }));
  }

  async function createSchool(event) {
    event.preventDefault();
    setSaving(true);
    setError('');
    try {
      const result = await superAdminSchoolApi.create({
        ...form,
        schoolName: form.schoolName.trim(),
        schoolCode: form.schoolCode.trim(),
        contactEmail: form.contactEmail.trim(),
        contactPhone: form.contactPhone.trim(),
        address: form.address.trim(),
        adminFullName: form.adminFullName.trim(),
        adminEmail: form.adminEmail.trim(),
      });
      setCreatedSchool(result);
      setCreatedAdminPassword(form.adminTemporaryPassword);
      setCopied(false);
      setCopyError('');
      setForm(INITIAL_FORM);
      await loadSchools();
    } catch (createError) {
      setError(createError.friendlyMessage ?? extractErrorMessage(createError));
    } finally {
      setSaving(false);
    }
  }

  async function copyInstructions() {
    setCopyError('');
    try {
      await navigator.clipboard.writeText(instructionsFor(createdSchool, createdAdminPassword));
      setCopied(true);
    } catch {
      setCopyError('Could not access the clipboard. Copy the details manually.');
    }
  }

  async function toggleSchoolStatus(school) {
    setRowAction(`${school.schoolId}:status`);
    setSchoolsError('');
    try {
      const result = await superAdminSchoolApi.toggleStatus(school.schoolId);
      setSchools((current) =>
        current.map((item) =>
          item.schoolId === school.schoolId ? { ...item, isActive: result.isActive } : item
        )
      );
    } catch (actionError) {
      setSchoolsError(actionError.friendlyMessage ?? extractErrorMessage(actionError));
    } finally {
      setRowAction('');
    }
  }

  async function submitAssignAdmin(event) {
    event.preventDefault();
    if (!assignSchool) return;
    setAssigning(true);
    setAssignError('');
    try {
      const admin = await superAdminSchoolApi.assignAdmin(assignSchool.schoolId, adminEmail.trim());
      setSchools((current) =>
        current.map((school) =>
          school.schoolId === assignSchool.schoolId ? { ...school, admin } : school
        )
      );
      setAssignSchool(null);
      setAdminEmail('');
    } catch (actionError) {
      setAssignError(actionError.friendlyMessage ?? extractErrorMessage(actionError));
    } finally {
      setAssigning(false);
    }
  }

  async function unassignAdmin(school) {
    if (!school.admin) return;
    const confirmed = window.confirm(
      `Unassign ${school.admin.fullName} (${school.admin.email}) from ${school.name}?`
    );
    if (!confirmed) return;

    setRowAction(`${school.schoolId}:admin`);
    setSchoolsError('');
    try {
      await superAdminSchoolApi.unassignAdmin(school.schoolId);
      setSchools((current) =>
        current.map((item) => (item.schoolId === school.schoolId ? { ...item, admin: null } : item))
      );
    } catch (actionError) {
      setSchoolsError(actionError.friendlyMessage ?? extractErrorMessage(actionError));
    } finally {
      setRowAction('');
    }
  }

  async function submitRenameSchool(event) {
    event.preventDefault();
    if (!renameSchool) return;
    setRenaming(true);
    setRenameError('');
    try {
      const updated = await superAdminSchoolApi.updateName(
        renameSchool.schoolId,
        schoolName.trim()
      );
      setSchools((current) =>
        current.map((item) =>
          item.schoolId === updated.schoolId ? { ...item, name: updated.name } : item
        )
      );
      setRenameSchool(null);
    } catch (actionError) {
      setRenameError(actionError.friendlyMessage ?? extractErrorMessage(actionError));
    } finally {
      setRenaming(false);
    }
  }

  return (
    <main className="mx-auto w-full max-w-5xl spacerounded-3xl bg-gradient-to-br from-blue-950 via-slate-900 to-brand-950 p-6-y-6">
      <header className="bg-blue-950 text-white p-6 rounded-xl shadow-lg border border-blue-800 text-white shadow-lg sm:p-8">
        <div className="flex items-start gap-4">
          <div className="grid size-12 shrink-0 place-items-center rounded-2xl bg-white/10 ring-1 ring-white/15">
            <Building2 className="size-6" aria-hidden="true" />
          </div>
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-brand-200 ">
              System Superadmin
            </p>
            <h1 className="mt-2 text-2xl font-bold tracking-tight sm:text-3xl">
              Tenant onboarding
            </h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-300 ">
              Create a school workspace and provision its first administrator in one secure step.
              Each tenant receives its own school identity and grade configuration.
            </p>
          </div>
        </div>
      </header>

      <section className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 p-5 sm:p-6">
          <div>
            <h2 className="text-lg font-bold text-slate-950 ">Registered schools</h2>
            <p className="mt-1 text-sm text-slate-500 ">
              Review tenant access and manage school administrators.
            </p>
          </div>
          <button
            type="button"
            onClick={loadSchools}
            disabled={schoolsLoading}
            className="inline-flex min-h-10 items-center gap-2 rounded-xl border border-slate-200 px-3.5 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
          >
            <RefreshCw
              className={`size-4 ${schoolsLoading ? 'animate-spin' : ''}`}
              aria-hidden="true"
            />
            Refresh
          </button>
        </div>

        {schoolsError && (
          <div className="p-4">
            <Alert
              variant="error"
              title="School management action failed"
              onDismiss={() => setSchoolsError('')}
            >
              {schoolsError}
            </Alert>
          </div>
        )}

        {schoolsLoading ? (
          <div className="flex min-h-44 items-center justify-center gap-3 text-sm text-slate-500 ">
            <LoaderCircle className="size-5 animate-spin" aria-hidden="true" />
            Loading schools…
          </div>
        ) : schools.length === 0 ? (
          <div className="grid min-h-44 place-items-center px-6 text-center">
            <div>
              <Building2 className="mx-auto size-8 text-slate-300 " aria-hidden="true" />
              <p className="mt-3 font-semibold text-slate-700 ">No school tenants found</p>
              <p className="mt-1 text-sm text-slate-500 ">
                Create a school using the onboarding form below.
              </p>
            </div>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead className="bg-slate-50 text-xs uppercase tracking-wider text-slate-500 ">
                <tr>
                  <th scope="col" className="px-5 py-3.5 font-semibold">
                    School
                  </th>
                  <th scope="col" className="px-5 py-3.5 font-semibold">
                    Date added
                  </th>
                  <th scope="col" className="px-5 py-3.5 font-semibold">
                    Administrator
                  </th>
                  <th scope="col" className="px-5 py-3.5 font-semibold">
                    System access
                  </th>
                  <th scope="col" className="px-5 py-3.5 font-semibold">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 ">
                {schools.map((school) => {
                  const busy = rowAction.startsWith(`${school.schoolId}:`);
                  return (
                    <tr key={school.schoolId} className="align-top hover:bg-slate-50/70">
                      <td className="px-5 py-4">
                        <p className="font-semibold text-slate-900 ">{school.name}</p>
                        <p
                          className="mt-1 max-w-48 truncate font-mono text-[11px] text-slate-400 "
                          title={school.schoolId}
                        >
                          {school.schoolId}
                        </p>
                      </td>
                      <td className="whitespace-nowrap px-5 py-4 text-slate-600 ">
                        {Number.isNaN(Date.parse(school.createdAt))
                          ? '—'
                          : new Date(school.createdAt).toLocaleDateString()}
                      </td>
                      <td className="px-5 py-4">
                        {school.admin ? (
                          <>
                            <p className="font-medium text-slate-800 ">{school.admin.fullName}</p>
                            <p className="mt-1 text-xs text-slate-500 ">{school.admin.email}</p>
                          </>
                        ) : (
                          <span className="inline-flex items-center gap-1.5 text-slate-500 ">
                            <AlertTriangle className="size-4 text-amber-500 " aria-hidden="true" />
                            No Admin Assigned
                          </span>
                        )}
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex flex-col items-start gap-2">
                          <span
                            className={`rounded-full px-2.5 py-1 text-xs font-bold ${
                              school.isActive
                                ? 'bg-emerald-50 text-emerald-700 '
                                : 'bg-slate-100 text-slate-600 '
                            }`}
                          >
                            {school.isActive ? 'Active' : 'Disabled'}
                          </span>
                          <button
                            type="button"
                            onClick={() => toggleSchoolStatus(school)}
                            disabled={busy}
                            aria-label={`${school.isActive ? 'Disable' : 'Enable'} access for ${school.name}`}
                            className={`min-h-9 rounded-lg px-3 py-1.5 text-xs font-semibold transition disabled:cursor-wait disabled:opacity-50 ${
                              school.isActive
                                ? 'border border-red-200 text-red-700 hover:bg-red-50 '
                                : 'border border-emerald-200 text-emerald-700 hover:bg-emerald-50 '
                            }`}
                          >
                            {busy && rowAction.endsWith(':status')
                              ? 'Updating…'
                              : school.isActive
                                ? 'Disable access'
                                : 'Enable access'}
                          </button>
                        </div>
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex flex-wrap gap-2">
                          <button
                            type="button"
                            onClick={() => {
                              setRenameSchool(school);
                              setSchoolName(school.name);
                              setRenameError('');
                            }}
                            disabled={busy}
                            className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-semibold text-slate-700 hover:bg-slate-100 disabled:opacity-50"
                          >
                            <Edit3 className="size-3.5" aria-hidden="true" />
                            Rename
                          </button>
                          <button
                            type="button"
                            onClick={() => {
                              setAssignSchool(school);
                              setAdminEmail('');
                              setAssignError('');
                            }}
                            className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-brand-200 px-2.5 py-1.5 text-xs font-semibold text-brand-800 hover:bg-brand-50 "
                          >
                            <UserRoundPlus className="size-3.5" aria-hidden="true" />
                            Assign Admin
                          </button>
                          {school.admin && (
                            <button
                              type="button"
                              onClick={() => unassignAdmin(school)}
                              disabled={busy}
                              className="inline-flex min-h-9 items-center gap-1.5 rounded-lg border border-slate-200 px-2.5 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-100 disabled:opacity-50"
                            >
                              <UserRoundX className="size-3.5" aria-hidden="true" />
                              Unassign
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {error && (
        <Alert variant="error" title="School could not be created" onDismiss={() => setError('')}>
          {error}
        </Alert>
      )}

      <form
        onSubmit={createSchool}
        className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm"
      >
        <section className="border-b border-slate-100 p-5 sm:p-7">
          <div className="mb-5 flex items-center gap-3">
            <span className="grid size-9 place-items-center rounded-xl bg-brand-50 text-brand-700 ">
              <Building2 className="size-4" aria-hidden="true" />
            </span>
            <div>
              <h2 className="font-bold text-slate-900 ">School information</h2>
              <p className="text-xs text-slate-500 ">
                Identity and official contact details for the new tenant.
              </p>
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            {FIELDS.slice(0, 5).map((field) => (
              <Field
                key={field.name}
                field={field}
                value={form[field.name]}
                onChange={updateField}
              />
            ))}
          </div>
        </section>

        <section className="p-5 sm:p-7">
          <div className="mb-5 flex items-center gap-3">
            <span className="grid size-9 place-items-center rounded-xl bg-emerald-50 text-emerald-700 ">
              <ShieldCheck className="size-4" aria-hidden="true" />
            </span>
            <div>
              <h2 className="font-bold text-slate-900 ">Primary administrator</h2>
              <p className="text-xs text-slate-500 ">
                The first school-level Admin account will be created with these details.
              </p>
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            {FIELDS.slice(5).map((field) => (
              <Field
                key={field.name}
                field={field}
                value={form[field.name]}
                onChange={updateField}
              />
            ))}
          </div>
          <div className="mt-6 flex flex-col-reverse gap-3 border-t border-slate-100 pt-5 sm:flex-row sm:items-center sm:justify-between">
            <p className="flex items-center gap-2 text-xs leading-5 text-slate-500 ">
              <KeyRound className="size-4 shrink-0" aria-hidden="true" />
              The temporary password is never returned by the API. Share it securely with the
              administrator.
            </p>
            <button
              type="submit"
              disabled={saving}
              className="inline-flex min-h-11 shrink-0 items-center justify-center gap-2 rounded-xl bg-brand-700 px-5 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-brand-800 disabled:cursor-wait disabled:opacity-60"
            >
              <Building2 className="size-4" aria-hidden="true" />
              {saving ? 'Creating school…' : 'Create school tenant'}
            </button>
          </div>
        </section>
      </form>

      {assignSchool && (
        <div className="fixed inset-0 z-40 grid place-items-center bg-slate-950/50 p-4 backdrop-blur-sm">
          <form
            onSubmit={submitAssignAdmin}
            className="w-full max-w-md rounded-3xl bg-white p-6 shadow-2xl sm:p-7"
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-bold uppercase tracking-wider text-brand-700 ">
                  School administrator
                </p>
                <h2 className="mt-1 text-xl font-bold text-slate-950 ">Assign an admin</h2>
                <p className="mt-1 text-sm text-slate-600 ">{assignSchool.name}</p>
              </div>
              <button
                type="button"
                onClick={() => setAssignSchool(null)}
                className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700 "
                aria-label="Close assign admin dialog"
              >
                <X className="size-5" aria-hidden="true" />
              </button>
            </div>
            <label
              htmlFor="school-admin-email"
              className="mt-5 block text-sm font-semibold text-slate-700 "
            >
              Existing user email
            </label>
            <input
              id="school-admin-email"
              type="email"
              autoComplete="email"
              required
              value={adminEmail}
              onChange={(event) => setAdminEmail(event.target.value)}
              placeholder="admin@example.edu"
              className="mt-1.5 w-full rounded-xl border border-slate-200 px-3.5 py-3 text-sm outline-none focus:border-brand-500 focus:ring-4 focus:ring-brand-100 "
            />
            <p className="mt-2 text-xs leading-5 text-slate-500 ">
              The account will be assigned to this school and receive school administrator access.
            </p>
            {assignError && (
              <p role="alert" className="mt-3 text-sm text-red-700 ">
                {assignError}
              </p>
            )}
            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setAssignSchool(null)}
                className="min-h-10 rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 "
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={assigning}
                className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-brand-700 px-4 py-2 text-sm font-bold text-white hover:bg-brand-800 disabled:opacity-60"
              >
                {assigning && <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />}
                {assigning ? 'Assigning…' : 'Assign administrator'}
              </button>
            </div>
          </form>
        </div>
      )}

      {renameSchool && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-slate-950/50 p-4 backdrop-blur-sm">
          <form
            onSubmit={submitRenameSchool}
            className="w-full max-w-md rounded-3xl bg-white p-6 shadow-2xl"
          >
            <h2 className="text-lg font-bold text-slate-950 ">Rename school</h2>
            <p className="mt-1 text-sm text-slate-500 ">
              This updates only {renameSchool.name} ({renameSchool.schoolId}).
            </p>
            <label
              htmlFor="tenant-school-name"
              className="mt-5 block text-sm font-semibold text-slate-700 "
            >
              School name
            </label>
            <input
              id="tenant-school-name"
              className="mt-1.5 w-full rounded-xl border border-slate-200 px-3.5 py-3 text-sm outline-none focus:border-brand-500 focus:ring-4 focus:ring-brand-100 "
              value={schoolName}
              onChange={(event) => setSchoolName(event.target.value)}
              maxLength={200}
              required
              autoFocus
            />
            {renameError && (
              <p role="alert" className="mt-3 text-sm text-red-700 ">
                {renameError}
              </p>
            )}
            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setRenameSchool(null)}
                disabled={renaming}
                className="min-h-10 rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 "
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={renaming || !schoolName.trim()}
                className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-brand-700 px-4 py-2 text-sm font-bold text-white hover:bg-brand-800 disabled:opacity-60"
              >
                {renaming && <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />}
                {renaming ? 'Saving…' : 'Save name'}
              </button>
            </div>
          </form>
        </div>
      )}

      {createdSchool && (
        <div className="fixed inset-0 z-50 grid place-items-center overflow-y-auto bg-slate-950/50 p-4 backdrop-blur-sm">
          <section
            role="dialog"
            aria-modal="true"
            aria-labelledby="tenant-created-title"
            className="my-auto w-full max-w-xl rounded-3xl bg-white p-6 shadow-2xl sm:p-8"
          >
            <div className="flex items-start justify-between gap-4">
              <div className="flex items-start gap-3">
                <span className="grid size-11 shrink-0 place-items-center rounded-2xl bg-emerald-50 text-emerald-700 ">
                  <Check className="size-5" aria-hidden="true" />
                </span>
                <div>
                  <p className="text-xs font-bold uppercase tracking-wider text-emerald-700 ">
                    Tenant ready
                  </p>
                  <h2 id="tenant-created-title" className="mt-1 text-xl font-bold text-slate-950 ">
                    {createdSchool.schoolName} created
                  </h2>
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  setCreatedSchool(null);
                  setCreatedAdminPassword('');
                }}
                className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700 "
                aria-label="Close success details"
              >
                <X className="size-5" aria-hidden="true" />
              </button>
            </div>

            <div className="mt-6 space-y-3 rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm">
              <Detail label="School ID" value={createdSchool.schoolId} mono />
              <Detail
                label="Registration URL"
                value={`${window.location.origin}/register/${createdSchool.slug}`}
              />
              <Detail label="School code" value={createdSchool.schoolCode} />
              <Detail
                label="Contact"
                value={createdSchool.contactEmail}
                icon={<Mail className="size-4" />}
              />
              <Detail
                label="Phone"
                value={createdSchool.contactPhone}
                icon={<Phone className="size-4" />}
              />
              <Detail
                label="Address"
                value={createdSchool.address}
                icon={<MapPin className="size-4" />}
              />
              <Detail
                label="Primary admin"
                value={`${createdSchool.adminFullName} · ${createdSchool.adminEmail}`}
              />
            </div>

            <div className="mt-5 rounded-2xl bg-brand-50 p-4">
              <h3 className="text-sm font-bold text-brand-950 ">Login instructions</h3>
              <p className="mt-1 text-sm leading-6 text-brand-900 ">
                Sign in at <span className="font-semibold">{window.location.origin}/login</span>{' '}
                with the primary administrator email and the temporary password you set above. That
                password is held only in this page's memory so the copy action can include setup
                instructions.
              </p>
            </div>

            <p className="mt-3 text-xs leading-5 text-amber-800 ">
              The copied instructions include the temporary password. Share them securely, and ask
              the administrator to change it after signing in.
            </p>

            {copyError && (
              <p role="alert" className="mt-3 text-sm text-red-700 ">
                {copyError}
              </p>
            )}
            <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => {
                  setCreatedSchool(null);
                  setCreatedAdminPassword('');
                }}
                className="min-h-11 rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 "
              >
                Done
              </button>
              <button
                type="button"
                onClick={copyInstructions}
                className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-brand-700 px-4 py-2 text-sm font-bold text-white hover:bg-brand-800 "
              >
                {copied ? (
                  <Check className="size-4" aria-hidden="true" />
                ) : (
                  <Copy className="size-4" aria-hidden="true" />
                )}
                {copied ? 'Copied tenant details' : 'Copy tenant details'}
              </button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}

function Detail({ label, value, icon, mono = false }) {
  return (
    <div className="grid grid-cols-[minmax(6rem,auto)_1fr] items-start gap-3">
      <span className="text-slate-500 ">{label}</span>
      <span
        className={`flex min-w-0 items-start gap-2 break-all font-medium text-slate-900 ${mono ? 'font-mono text-xs' : ''}`}
      >
        {icon && <span className="mt-0.5 shrink-0 text-slate-400 ">{icon}</span>}
        {value || '—'}
      </span>
    </div>
  );
}
