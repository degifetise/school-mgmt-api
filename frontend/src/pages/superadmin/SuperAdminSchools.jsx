import { useState } from 'react';
import {
  Building2,
  Check,
  Copy,
  KeyRound,
  Mail,
  MapPin,
  Phone,
  ShieldCheck,
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
  { name: 'schoolCode', label: 'School code', placeholder: 'EXAMPLE-HS', required: true, hint: 'Letters, numbers, and hyphens only.', maxLength: 50 },
  { name: 'contactEmail', label: 'Contact email', type: 'email', placeholder: 'office@example.edu', required: true },
  { name: 'contactPhone', label: 'Contact phone', type: 'tel', placeholder: '+1 555 0100', required: true },
  { name: 'address', label: 'School address', placeholder: 'Street, city, region', required: true, wide: true },
  { name: 'adminFullName', label: 'Primary admin full name', placeholder: 'Jordan Lee', required: true, maxLength: 150 },
  { name: 'adminEmail', label: 'Primary admin email', type: 'email', placeholder: 'admin@example.edu', required: true },
  { name: 'adminTemporaryPassword', label: 'Temporary password', type: 'password', placeholder: 'At least 8 characters', required: true, hint: 'Must include uppercase, lowercase, number, and symbol.', maxLength: 100 },
];

function instructionsFor(school, temporaryPassword) {
  return [
    'School tenant created successfully',
    `School: ${school.schoolName}`,
    `School code: ${school.schoolCode}`,
    `School ID: ${school.schoolId}`,
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
    maxLength: field.maxLength ?? (field.name === 'address' ? 500 : field.name === 'schoolName' ? 200 : 256),
    autoComplete: field.name === 'adminTemporaryPassword'
      ? 'new-password'
      : field.name.toLowerCase().includes('email')
        ? 'email'
        : 'off',
    className: 'mt-1.5 w-full rounded-xl border border-slate-200 bg-white px-3.5 py-3 text-sm text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-brand-500 focus:ring-4 focus:ring-brand-100',
    placeholder: field.placeholder,
  };

  return (
    <div className={field.wide ? 'sm:col-span-2' : ''}>
      <label htmlFor={inputId} className="text-sm font-semibold text-slate-700">{field.label}</label>
      <input {...common} />
      {field.hint && <p className="mt-1 text-xs text-slate-500">{field.hint}</p>}
    </div>
  );
}

export default function SuperAdminSchools() {
  const [form, setForm] = useState(INITIAL_FORM);
  const [createdSchool, setCreatedSchool] = useState(null);
  const [createdAdminPassword, setCreatedAdminPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [copied, setCopied] = useState(false);
  const [copyError, setCopyError] = useState('');

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

  return (
    <main className="mx-auto w-full max-w-5xl space-y-6">
      <header className="rounded-3xl bg-gradient-to-br from-slate-950 via-slate-900 to-brand-950 p-6 text-white shadow-lg sm:p-8">
        <div className="flex items-start gap-4">
          <div className="grid size-12 shrink-0 place-items-center rounded-2xl bg-white/10 ring-1 ring-white/15">
            <Building2 className="size-6" aria-hidden="true" />
          </div>
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-brand-200">System / Superadmin</p>
            <h1 className="mt-2 text-2xl font-bold tracking-tight sm:text-3xl">Tenant onboarding</h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-300">
              Create a school workspace and provision its first administrator in one secure step.
              Each tenant receives its own school identity and grade configuration.
            </p>
          </div>
        </div>
      </header>

      {error && <Alert variant="error" title="School could not be created" onDismiss={() => setError('')}>{error}</Alert>}

      <form onSubmit={createSchool} className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <section className="border-b border-slate-100 p-5 sm:p-7">
          <div className="mb-5 flex items-center gap-3">
            <span className="grid size-9 place-items-center rounded-xl bg-brand-50 text-brand-700">
              <Building2 className="size-4" aria-hidden="true" />
            </span>
            <div>
              <h2 className="font-bold text-slate-900">School information</h2>
              <p className="text-xs text-slate-500">Identity and official contact details for the new tenant.</p>
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            {FIELDS.slice(0, 5).map((field) => (
              <Field key={field.name} field={field} value={form[field.name]} onChange={updateField} />
            ))}
          </div>
        </section>

        <section className="p-5 sm:p-7">
          <div className="mb-5 flex items-center gap-3">
            <span className="grid size-9 place-items-center rounded-xl bg-emerald-50 text-emerald-700">
              <ShieldCheck className="size-4" aria-hidden="true" />
            </span>
            <div>
              <h2 className="font-bold text-slate-900">Primary administrator</h2>
              <p className="text-xs text-slate-500">The first school-level Admin account will be created with these details.</p>
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            {FIELDS.slice(5).map((field) => (
              <Field key={field.name} field={field} value={form[field.name]} onChange={updateField} />
            ))}
          </div>
          <div className="mt-6 flex flex-col-reverse gap-3 border-t border-slate-100 pt-5 sm:flex-row sm:items-center sm:justify-between">
            <p className="flex items-center gap-2 text-xs leading-5 text-slate-500">
              <KeyRound className="size-4 shrink-0" aria-hidden="true" />
              The temporary password is never returned by the API. Share it securely with the administrator.
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
                <span className="grid size-11 shrink-0 place-items-center rounded-2xl bg-emerald-50 text-emerald-700">
                  <Check className="size-5" aria-hidden="true" />
                </span>
                <div>
                  <p className="text-xs font-bold uppercase tracking-wider text-emerald-700">Tenant ready</p>
                  <h2 id="tenant-created-title" className="mt-1 text-xl font-bold text-slate-950">
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
                className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                aria-label="Close success details"
              >
                <X className="size-5" aria-hidden="true" />
              </button>
            </div>

            <div className="mt-6 space-y-3 rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm">
              <Detail label="School ID" value={createdSchool.schoolId} mono />
              <Detail label="School code" value={createdSchool.schoolCode} />
              <Detail label="Contact" value={createdSchool.contactEmail} icon={<Mail className="size-4" />} />
              <Detail label="Phone" value={createdSchool.contactPhone} icon={<Phone className="size-4" />} />
              <Detail label="Address" value={createdSchool.address} icon={<MapPin className="size-4" />} />
              <Detail label="Primary admin" value={`${createdSchool.adminFullName} · ${createdSchool.adminEmail}`} />
            </div>

            <div className="mt-5 rounded-2xl bg-brand-50 p-4">
              <h3 className="text-sm font-bold text-brand-950">Login instructions</h3>
              <p className="mt-1 text-sm leading-6 text-brand-900">
                Sign in at <span className="font-semibold">{window.location.origin}/login</span> with
                the primary administrator email and the temporary password you set above. That password
                is held only in this page's memory so the copy action can include setup instructions.
              </p>
            </div>

            <p className="mt-3 text-xs leading-5 text-amber-800">
              The copied instructions include the temporary password. Share them securely, and ask the
              administrator to change it after signing in.
            </p>

            {copyError && <p role="alert" className="mt-3 text-sm text-red-700">{copyError}</p>}
            <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                type="button"
                onClick={() => {
                  setCreatedSchool(null);
                  setCreatedAdminPassword('');
                }}
                className="min-h-11 rounded-xl border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50"
              >
                Done
              </button>
              <button
                type="button"
                onClick={copyInstructions}
                className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-brand-700 px-4 py-2 text-sm font-bold text-white hover:bg-brand-800"
              >
                {copied ? <Check className="size-4" aria-hidden="true" /> : <Copy className="size-4" aria-hidden="true" />}
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
      <span className="text-slate-500">{label}</span>
      <span className={`flex min-w-0 items-start gap-2 break-all font-medium text-slate-900 ${mono ? 'font-mono text-xs' : ''}`}>
        {icon && <span className="mt-0.5 shrink-0 text-slate-400">{icon}</span>}
        {value || '—'}
      </span>
    </div>
  );
}
