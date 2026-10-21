import { useCallback, useEffect, useState } from 'react';
import { ArrowRightLeft, BriefcaseBusiness, Plus, Save, UserRoundCog, X } from 'lucide-react';
import TeacherAssignmentModal from '../../components/teacher/TeacherAssignmentModal';
import BulkTeacherAssignmentForm from '../../components/teacher/BulkTeacherAssignmentForm';
import { gradeLevelApi, sectionApi, teacherApi } from '../../services/endpoints';
import { extractErrorMessage } from '../../services/api';
import { Alert, EmptyState, ErrorState, LoadingPanel, Spinner } from '../../components/ui/Feedback';

export default function TeacherAssignments() {
  const [teachers, setTeachers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [selectedTeacher, setSelectedTeacher] = useState(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [reassigning, setReassigning] = useState(null);
  const [reassignTeacherId, setReassignTeacherId] = useState('');
  const [savingReassignment, setSavingReassignment] = useState(false);
  const [grades, setGrades] = useState([]);
  const [gradeSections, setGradeSections] = useState([]);
  const [sectionAssignments, setSectionAssignments] = useState({});
  const [sectionTeacherId, setSectionTeacherId] = useState(null);
  const [sectionGradeId, setSectionGradeId] = useState('');
  const [sectionId, setSectionId] = useState('');
  const [loadingSectionAssignments, setLoadingSectionAssignments] = useState(false);
  const [savingSectionAssignment, setSavingSectionAssignment] = useState(false);

  const loadTeachers = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await teacherApi.list({ page: 1, pageSize: 100, includeInactive: true });
      setTeachers(data.items ?? []);
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadTeachers();
  }, [loadTeachers]);

  useEffect(() => {
    gradeLevelApi.list()
      .then((gradeData) => {
        setGrades(gradeData);
      })
      .catch((err) => setError(err.friendlyMessage ?? extractErrorMessage(err)));
  }, []);

  useEffect(() => {
    if (!sectionGradeId) {
      setGradeSections([]);
      return;
    }
    sectionApi.list({ gradeLevelId: Number(sectionGradeId) })
      .then(setGradeSections)
      .catch((err) => setError(err.friendlyMessage ?? extractErrorMessage(err)));
  }, [sectionGradeId]);

  async function toggleSectionAssignments(teacherId) {
    if (sectionTeacherId === teacherId) {
      setSectionTeacherId(null);
      return;
    }

    setSectionTeacherId(teacherId);
    setLoadingSectionAssignments(true);
    setError('');
    try {
      const assignments = await teacherApi.getSectionAssignments(teacherId);
      setSectionAssignments((current) => ({ ...current, [teacherId]: assignments }));
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setLoadingSectionAssignments(false);
    }
  }

  async function addSectionAssignment(teacherId, event) {
    event.preventDefault();
    if (!sectionGradeId || !sectionId) return;
    setSavingSectionAssignment(true);
    setError('');
    try {
      const added = await teacherApi.assignSections(teacherId, [
        { gradeLevelId: Number(sectionGradeId), sectionId: Number(sectionId) },
      ]);
      setSectionAssignments((value) => ({ ...value, [teacherId]: added }));
      setNotice('Section assigned to teacher.');
      setSectionId('');
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setSavingSectionAssignment(false);
    }
  }

  async function removeSectionAssignment(teacherId, assignedSectionId) {
    setError('');
    try {
      await teacherApi.removeSectionAssignment(teacherId, assignedSectionId);
      setSectionAssignments((current) => ({
        ...current,
        [teacherId]: (current[teacherId] ?? []).filter((assignment) => assignment.sectionId !== assignedSectionId),
      }));
      setNotice('Section assignment removed.');
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    }
  }

  async function removeAssignment(teacherId, assignmentId) {
    try {
      await teacherApi.removeAssignment(teacherId, assignmentId);
      setNotice('Assignment removed.');
      await loadTeachers();
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    }
  }

  async function saveReassignment(event) {
    event.preventDefault();
    if (!reassigning || !reassignTeacherId) return;

    setSavingReassignment(true);
    setError('');
    try {
      await teacherApi.reassignAssignment(reassigning.assignmentId, Number(reassignTeacherId));
      setNotice('Teaching assignment reassigned.');
      setReassigning(null);
      setReassignTeacherId('');
      await loadTeachers();
    } catch (err) {
      setError(err.friendlyMessage ?? extractErrorMessage(err));
    } finally {
      setSavingReassignment(false);
    }
  }

  if (loading) return <LoadingPanel label="Loading teachers…" />;
  if (error) return <ErrorState title="Could not load teachers" message={error} onRetry={loadTeachers} />;

  return (
    <div className="space-y-6">
      {notice && <Alert variant="success" onDismiss={() => setNotice(null)}>{notice}</Alert>}
      {error && <Alert variant="error" title="Something went wrong" onDismiss={() => setError(null)}>{error}</Alert>}

      <section className="card p-5">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Staff map</p>
            <h1 className="text-2xl font-bold text-slate-900">Teacher assignments</h1>
          </div>
          <div className="rounded-lg bg-brand-100 px-3 py-2 text-sm font-medium text-brand-700">
            {teachers.length} teachers
          </div>
        </div>
      </section>

      {teachers.length === 0 ? (
        <EmptyState icon={BriefcaseBusiness} title="No teachers found" description="Create teacher profiles first to assign classes." />
      ) : (
        <>
        <BulkTeacherAssignmentForm teachers={teachers} onSaved={loadTeachers} />
        <section className="space-y-4">
          {teachers.map((teacher) => (
            <div key={teacher.id} className="card p-5">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 pb-4">
                <div>
                  <h2 className="text-lg font-bold text-slate-900">{teacher.fullName}</h2>
                  <p className="text-sm text-slate-500">{teacher.specialization || 'No specialization'} · {teacher.employeeId}</p>
                </div>
                <button className="btn-primary" onClick={() => { setSelectedTeacher(teacher); setModalOpen(true); }}>
                  <Plus className="size-4" aria-hidden="true" />
                  Assign class
                </button>
              </div>

              {teacher.assignments?.length ? (
                <div className="mt-4 space-y-3">
                  {teacher.assignments.map((assignment) => (
                    <div key={assignment.id} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
                      <div>
                        <p className="font-semibold text-slate-900">{assignment.subjectName}</p>
                        <p className="text-sm text-slate-500">{assignment.gradeLevelName} · {assignment.sectionName} · {assignment.academicYear}</p>
                      </div>
                      <button type="button" className="btn-secondary" onClick={() => removeAssignment(teacher.id, assignment.id)}>
                        Remove
                      </button>
                      <button
                        type="button"
                        className="btn-secondary"
                        onClick={() => {
                          setReassigning({
                            assignmentId: assignment.id,
                            teacherId: teacher.id,
                            subjectName: assignment.subjectName,
                            sectionName: `${assignment.gradeLevelName} · ${assignment.sectionName}`,
                          });
                          setReassignTeacherId('');
                        }}
                      >
                        <ArrowRightLeft className="size-4" aria-hidden="true" />
                        Change teacher
                      </button>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="mt-4 text-sm text-slate-500">No active assignments yet.</p>
              )}

              <div className="mt-5 border-t border-slate-200 pt-4">
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={() => toggleSectionAssignments(teacher.id)}
                  aria-expanded={sectionTeacherId === teacher.id}
                >
                  <UserRoundCog className="size-4" aria-hidden="true" />
                  {sectionTeacherId === teacher.id ? 'Hide section access' : 'Manage section access'}
                </button>
                {sectionTeacherId === teacher.id && (
                  <div className="mt-4 space-y-4">
                    {loadingSectionAssignments ? (
                      <Spinner />
                    ) : (
                      <>
                        <form className="grid gap-3 sm:grid-cols-[1fr_1fr_auto]" onSubmit={(event) => addSectionAssignment(teacher.id, event)}>
                          <label className="space-y-1 text-sm font-medium text-slate-700">
                            Grade
                            <select
                              className="input"
                              required
                              value={sectionGradeId}
                              onChange={(event) => {
                                setSectionGradeId(event.target.value);
                                setSectionId('');
                              }}
                            >
                              <option value="">Select grade</option>
                              {grades.map((grade) => <option key={grade.id} value={grade.id}>{grade.name}</option>)}
                            </select>
                          </label>
                          <label className="space-y-1 text-sm font-medium text-slate-700">
                            Section
                            <select
                              className="input"
                              required
                              disabled={!sectionGradeId}
                              value={sectionId}
                              onChange={(event) => setSectionId(event.target.value)}
                            >
                              <option value="">Select section</option>
                              {gradeSections.filter((section) => section.isActive).map((section) => (
                                <option key={section.id} value={section.id}>{section.name}</option>
                              ))}
                            </select>
                          </label>
                          <button className="btn-primary self-end" type="submit" disabled={savingSectionAssignment || !sectionGradeId || !sectionId}>
                            {savingSectionAssignment ? 'Assigning…' : 'Assign section'}
                          </button>
                        </form>
                        <div className="space-y-2">
                          {(sectionAssignments[teacher.id] ?? []).length ? (
                            sectionAssignments[teacher.id].map((assignment) => (
                              <div key={`${assignment.gradeLevelId}-${assignment.sectionId}`} className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
                                <span className="text-sm font-medium text-slate-800">
                                  {assignment.gradeLevelName} · {assignment.sectionName}
                                </span>
                                <button
                                  type="button"
                                  className="btn-secondary"
                                  onClick={() => removeSectionAssignment(teacher.id, assignment.sectionId)}
                                >
                                  <X className="size-4" aria-hidden="true" />
                                  Unassign
                                </button>
                              </div>
                            ))
                          ) : (
                            <p className="text-sm text-slate-500">No sections assigned.</p>
                          )}
                        </div>
                      </>
                    )}
                  </div>
                )}
              </div>
            </div>
          ))}
        </section>
        </>
      )}

      <TeacherAssignmentModal
        teacher={selectedTeacher}
        open={modalOpen}
        onClose={() => {
          setModalOpen(false);
          setSelectedTeacher(null);
        }}
        onSaved={async () => {
          setModalOpen(false);
          setSelectedTeacher(null);
          await loadTeachers();
        }}
      />

      {reassigning && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-slate-950/50 p-4" role="presentation">
          <section role="dialog" aria-modal="true" aria-labelledby="reassign-title" className="card w-full max-w-lg p-6 shadow-xl">
            <h2 id="reassign-title" className="text-xl font-bold text-slate-900">Change assigned teacher</h2>
            <p className="mt-1 text-sm text-slate-600">
              {reassigning.subjectName} · {reassigning.sectionName}
            </p>
            <form onSubmit={saveReassignment} className="mt-5 space-y-4">
              <label className="block space-y-1 text-sm font-medium text-slate-700">
                New teacher
                <select
                  className="input"
                  required
                  value={reassignTeacherId}
                  onChange={(event) => setReassignTeacherId(event.target.value)}
                >
                  <option value="">Select an active teacher…</option>
                  {teachers
                    .filter((teacher) => teacher.isActive && teacher.id !== reassigning.teacherId)
                    .map((teacher) => (
                      <option key={teacher.id} value={teacher.id}>{teacher.fullName} · {teacher.employeeId}</option>
                    ))}
                </select>
              </label>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  className="btn"
                  onClick={() => setReassigning(null)}
                  disabled={savingReassignment}
                >
                  <X className="size-4" aria-hidden="true" />
                  Cancel
                </button>
                <button type="submit" className="btn-primary" disabled={savingReassignment || !reassignTeacherId}>
                  {savingReassignment ? <Spinner className="size-4" /> : <Save className="size-4" aria-hidden="true" />}
                  Reassign
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </div>
  );
}
