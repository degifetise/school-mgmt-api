# Database tenant-schema audit

## Tenant-scoped tables

Every tenant-owned operational row should have a non-null `Guid SchoolId`, a
foreign key to `Schools.Id`, and an index beginning with `SchoolId`. Keep
tenant IDs on dependent records as well as their parent IDs so constraints and
queries can enforce that both records belong to the same school.

### Already directly tenant-linked

- [x] `Schools` (`School`): tenant registry; `Id` is the tenant primary key.
- [x] `AspNetUsers` (`ApplicationUser`): required `SchoolId` foreign key and
  index.
- [x] `SchoolGradeSections` (`SchoolGradeSection`): required `SchoolId` in the
  key and a school relationship.
- [x] `StudentRegistrationRequests`
  (`StudentRegistrationRequest`): required `SchoolId` foreign key and
  school-prefixed indexes.
- [x] `Assessments` (`Assessment`): required school foreign key and index;
  assessable marks are constrained to the same school through a composite key.
- [x] `Marks` (`Mark`): required school foreign key and index.
- [x] `Events` (`Event`): required school foreign key and index; creation
  assigns the authenticated school's ID, and the migration backfills ownership
  from the event organizer account.

### Missing direct tenant ownership

For each item below, add a required `SchoolId` property and `School` navigation
to the named model, configure its foreign key and `SchoolId`-leading index in
`ApplicationDbContext`, and add same-school composite foreign keys where the
row references another tenant-owned entity.

- [ ] `Students` — `Student`
- [ ] `Teachers` — `Teacher`
- [ ] `Guardians` — `Guardian`
- [ ] `StudentGuardians` — `StudentGuardian`
- [ ] `TeacherSubjects` — `TeacherSubject`
- [ ] `TeacherSectionAssignments` — `TeacherSectionAssignment`
- [ ] `Lessons` — `Lesson`
- [ ] `Attendance` — `Attendance`
- [ ] `Announcements` — `Announcement`
- [ ] `EventRegistrations` — `EventRegistration`
- [ ] `EventComments` — `EventComment`
- [ ] `Notifications` — `Notification`
- [ ] `StudentFeedbacks` — `StudentFeedback`
- [ ] `FeeInvoices` — `FeeInvoice`
- [ ] `FeePayments` — `FeePayment`
- [ ] `SmartCards` — `SmartCard`
- [ ] `SmartIDScanLogs` — `SmartIDScanLog`
- [ ] `RefreshTokens` — `RefreshToken`
- [ ] `PasswordResetRequests` — `PasswordResetRequest`
- [ ] `PasswordChangeLogs` — `PasswordChangeLog`

`StudentSubjectPerformance` is a keyless view, not an independently stored
table. Its view definition must project `SchoolId` from its tenant-owned source
records after those entities are migrated.

Event registrations and comments are currently scoped through their parent
event in application queries; they still need direct tenant ownership and
same-school database constraints to satisfy the direct-ownership rule above.

## Global and shared system tables

These are currently shared catalogs or Identity/system infrastructure. Keep
them global only while their values are intentionally identical for all
schools; any school-editable settings should instead be stored against the
school.

- [x] `GradeLevels` (`GradeLevel`) — shared grade catalog; activation is
  represented by `SchoolGradeSections`.
- [x] `Sections` (`Section`) — shared section catalog; activation is
  represented by `SchoolGradeSections`.
- [x] `GradeLevelSections` (`GradeLevelSection`) — shared grade/section lookup.
- [x] `Subjects` (`Subject`) — currently a shared lookup; make tenant-scoped if
  schools manage their own subject catalog.
- [x] `AssessmentTypes` (`AssessmentTypeWeight`) — currently shared default
  assessment weights; make tenant-scoped if schools can customize weights.
- [x] `SystemSettings` (`SystemSetting`) — currently global; do not store
  school-specific settings here.
- [x] ASP.NET Identity role, role-claim, login, user-claim, and token tables —
  shared Identity infrastructure. User-owned rows are reached through their
  Identity user relationship.
- [x] `StudentSubjectPerformance` — a database view, not a stored table.

## Migration and data backfill requirements

- [ ] Add required school ownership to the remaining tenant models and update
  the EF model snapshot before generating their migration.
- [ ] Backfill each existing row using a verified ownership path. Students and
  teachers may be associated through `ApplicationUser.SchoolId` only when
  `UserId` is present; orphaned rows need an explicit mapping decision.
- [ ] Backfill dependent records from their parent only after confirming the
  parent row's tenant. Reject or manually reconcile records with conflicting
  parent ownership rather than assigning a default tenant.
- [ ] Add the `Schools.Id` foreign keys and `SchoolId`-leading indexes, plus
  composite same-tenant constraints for cross-entity references.
- [ ] Scope the performance view and all migration-created indexes/constraints
  to the resulting tenant-aware schema.
- [ ] Apply and verify the migration against an available local PostgreSQL
  instance before considering it ready for deployment.

`TenantScopeAssessmentsAndMarks` backfills ownership from teacher accounts and
student accounts. It intentionally aborts rather than assigning a default
school when legacy records have missing or conflicting ownership. Apply it only
after resolving any such records in the target database.

The existing `AddSchoolRegistrationSlugs` migration adds the school slug and
tenant ownership to registration requests. Together with
`TenantScopeAssessmentsAndMarks`, it does not yet provide complete tenant
isolation for every operational table.
