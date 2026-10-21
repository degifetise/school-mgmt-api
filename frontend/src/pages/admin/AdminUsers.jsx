import { useCallback, useEffect, useMemo, useState } from 'react';
import { Check, RefreshCw, Search, ShieldCheck, UserCog, UserRoundX, Users } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { adminUserApi } from '../../services/endpoints';
import { extractErrorMessage } from '../../services/api';
import { Alert, Badge, EmptyState, ErrorState, LoadingPanel, Spinner } from '../../components/ui/Feedback';

function roleLabel(role) {
  return role === 'Guardian' ? 'Parent / Guardian' : role;
}

function roleTone(role) {
  if (role === 'Admin') return 'brand';
  if (role === 'Teacher') return 'green';
  if (role === 'Student') return 'amber';
  return 'slate';
}

export default function AdminUsers() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState([]);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const loadUsers = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      setUsers(await adminUserApi.list());
    } catch (loadError) {
      setError(loadError.friendlyMessage ?? extractErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const filteredUsers = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    return users.filter((user) => {
      const matchesSearch = !normalizedSearch
        || user.fullName?.toLowerCase().includes(normalizedSearch)
        || user.email?.toLowerCase().includes(normalizedSearch)
        || user.roles.some((role) => role.toLowerCase().includes(normalizedSearch));
      const matchesRole = !roleFilter || user.roles.includes(roleFilter);
      const matchesStatus = !statusFilter
        || (statusFilter === 'active' ? user.isActive : !user.isActive);
      return matchesSearch && matchesRole && matchesStatus;
    });
  }, [users, search, roleFilter, statusFilter]);

  async function promote(user) {
    if (!window.confirm(`Promote ${user.fullName} to Admin? This grants full administrative access.`)) return;

    setBusyId(user.id);
    setError('');
    setNotice('');
    try {
      const updatedUser = await adminUserApi.promoteToAdmin(user.id);
      setUsers((current) => current.map((item) => item.id === user.id ? updatedUser : item));
      setNotice(`${user.fullName} now has the Admin role.`);
    } catch (actionError) {
      setError(actionError.friendlyMessage ?? extractErrorMessage(actionError));
    } finally {
      setBusyId('');
    }
  }

  async function setActive(user) {
    const nextStatus = !user.isActive;
    const action = nextStatus ? 'activate' : 'deactivate';
    if (!window.confirm(`${action[0].toUpperCase()}${action.slice(1)} ${user.fullName}'s account?`)) return;

    setBusyId(user.id);
    setError('');
    setNotice('');
    try {
      const updatedUser = await adminUserApi.setStatus(user.id, nextStatus);
      setUsers((current) => current.map((item) => item.id === user.id ? updatedUser : item));
      setNotice(`${user.fullName}'s account is now ${nextStatus ? 'active' : 'deactivated'}.`);
    } catch (actionError) {
      setError(actionError.friendlyMessage ?? extractErrorMessage(actionError));
    } finally {
      setBusyId('');
    }
  }

  const activeCount = users.filter((user) => user.isActive).length;
  const adminCount = users.filter((user) => user.roles.includes('Admin')).length;
  const inactiveCount = users.length - activeCount;

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900">User management</h1>
          <p className="mt-1 text-sm text-slate-600">
            Review portal accounts, manage administrator access, and deactivate accounts when staff leave.
          </p>
        </div>
        <button type="button" className="btn-secondary" onClick={loadUsers} disabled={loading}>
          {loading ? <Spinner className="size-4" /> : <RefreshCw className="size-4" aria-hidden="true" />}
          Refresh
        </button>
      </header>

      {error && <Alert variant="error" title="User management action failed" onDismiss={() => setError('')}>{error}</Alert>}
      {notice && <Alert variant="success" onDismiss={() => setNotice('')}>{notice}</Alert>}

      <section className="grid gap-3 sm:grid-cols-3" aria-label="User account summary">
        <div className="card flex items-center gap-3 p-4">
          <span className="rounded-lg bg-brand-50 p-2 text-brand-700"><Users className="size-5" aria-hidden="true" /></span>
          <div><p className="text-xs font-medium text-slate-500">Total users</p><p className="text-xl font-bold text-slate-900">{users.length}</p></div>
        </div>
        <div className="card flex items-center gap-3 p-4">
          <span className="rounded-lg bg-emerald-50 p-2 text-emerald-700"><Check className="size-5" aria-hidden="true" /></span>
          <div><p className="text-xs font-medium text-slate-500">Active accounts</p><p className="text-xl font-bold text-slate-900">{activeCount}</p></div>
        </div>
        <div className="card flex items-center gap-3 p-4">
          <span className="rounded-lg bg-amber-50 p-2 text-amber-700"><ShieldCheck className="size-5" aria-hidden="true" /></span>
          <div><p className="text-xs font-medium text-slate-500">Administrators</p><p className="text-xl font-bold text-slate-900">{adminCount}</p></div>
          <span className="ml-auto text-xs text-slate-500">{inactiveCount} inactive</span>
        </div>
      </section>

      <section className="card overflow-hidden">
        <div className="grid gap-3 border-b border-slate-200 p-4 md:grid-cols-[minmax(14rem,1fr)_auto_auto] md:items-end">
          <label className="text-sm font-medium text-slate-700">
            Search users
            <span className="relative mt-1 block">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" aria-hidden="true" />
              <input
                className="input pl-9"
                type="search"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Name, email, or role"
              />
            </span>
          </label>
          <label className="text-sm font-medium text-slate-700">
            Role
            <select className="input mt-1" value={roleFilter} onChange={(event) => setRoleFilter(event.target.value)}>
              <option value="">All roles</option>
              <option value="Admin">Admin</option>
              <option value="Teacher">Teacher</option>
              <option value="Student">Student</option>
              <option value="Guardian">Parent / Guardian</option>
              <option value="Staff">Staff</option>
            </select>
          </label>
          <label className="text-sm font-medium text-slate-700">
            Status
            <select className="input mt-1" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              <option value="">All statuses</option>
              <option value="active">Active</option>
              <option value="inactive">Deactivated</option>
            </select>
          </label>
        </div>

        {loading ? <LoadingPanel label="Loading users…" /> : error && users.length === 0 ? (
          <ErrorState title="Could not load users" message={error} onRetry={loadUsers} retrying={loading} />
        ) : filteredUsers.length === 0 ? (
          <EmptyState
            icon={UserRoundX}
            title="No matching users"
            description={users.length ? 'Try adjusting the search or filters.' : 'No portal accounts have been created yet.'}
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-4 py-3">User</th>
                  <th className="px-4 py-3">Roles</th>
                  <th className="px-4 py-3">Account status</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredUsers.map((user) => {
                  const isBusy = busyId === user.id;
                  const isAdmin = user.roles.includes('Admin');
                  const isCurrentUser = currentUser?.id === user.id;
                  return (
                    <tr key={user.id} className="align-middle hover:bg-slate-50">
                      <td className="px-4 py-3">
                        <p className="font-semibold text-slate-900">{user.fullName || 'Unnamed user'}</p>
                        <p className="mt-0.5 break-all text-xs text-slate-500">{user.email || 'No email address'}</p>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1.5">
                          {user.roles.length
                            ? user.roles.map((role) => <Badge key={role} tone={roleTone(role)}>{roleLabel(role)}</Badge>)
                            : <Badge>No role</Badge>}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1.5">
                          <Badge tone={user.isActive ? 'green' : 'red'}>{user.isActive ? 'Active' : 'Deactivated'}</Badge>
                          {user.isLockedOut && <Badge tone="amber">Locked out</Badge>}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex justify-end gap-2">
                          {!isAdmin && (
                            <button
                              type="button"
                              className="btn-secondary !px-2.5 !py-1.5 text-xs"
                              onClick={() => promote(user)}
                              disabled={isBusy}
                            >
                              {isBusy ? <Spinner className="size-4" /> : <UserCog className="size-4" aria-hidden="true" />}
                              Promote to Admin
                            </button>
                          )}
                          <button
                            type="button"
                            className={`btn-secondary !px-2.5 !py-1.5 text-xs ${user.isActive ? 'text-rose-700' : 'text-emerald-700'}`}
                            onClick={() => setActive(user)}
                            disabled={isBusy || (isCurrentUser && user.isActive)}
                            title={isCurrentUser && user.isActive ? 'You cannot deactivate your own account.' : undefined}
                          >
                            {isBusy ? <Spinner className="size-4" /> : null}
                            {user.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            <p className="border-t border-slate-100 px-4 py-3 text-xs text-slate-500">
              Showing {filteredUsers.length} of {users.length} accounts. A final active administrator cannot be deactivated.
            </p>
          </div>
        )}
      </section>
    </div>
  );
}
