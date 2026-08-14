

This document explains the database design in plain terms: what each table represents, how tables connect, and the business rules the schema enforces. Pair it with `schema-erd.mermaid` for the visual diagram.

## Overview

The system has **3 roles** (Admin, Teacher, Student) sharing one `User` table, **2 lookup tables** (`SchoolClass`, `Subject`), **2 join tables** that model "who's connected to what" (`TeacherAssignment`, `StudentEnrollment`), and **2 core workflow tables** (`Assignment`, `Submission`) that hold the actual coursework data.

```
User ──┬── TeacherAssignment ──┬── SchoolClass
       │                       └── Subject
       ├── StudentEnrollment ──── SchoolClass
       ├── Assignment (as creator) ──┬── SchoolClass
       │                             └── Subject
       └── Submission (as submitter) ── Assignment
```

---

## Tables

### `User`

Every person in the system — Admin, Teacher, or Student — is one row here, distinguished by the `Role` column.

|Column|Type|Notes|
|---|---|---|
|`Id`|guid (PK)||
|`FullName`|string||
|`Email`|string|**Unique.** Used as the login identifier.|
|`PasswordHash`|string|BCrypt hash — the plain password is never stored.|
|`Role`|string|`Admin`, `Teacher`, or `Student`. Stored as text, not a number, so the database stays human-readable.|
|`IsActive`|bool|Lets Admin disable an account without deleting it.|
|`CreatedAt`|datetime||

**Why one table for all three roles**, instead of separate `Admin`/`Teacher`/ `Student` tables: they share every field (name, email, password, login behavior), and splitting them would mean either three near-identical tables or a more complex "table-per-hierarchy" EF mapping — real added complexity for no practical benefit at this size. `Role` is the only thing that differs, so it's just a column.

---

### `SchoolClass`

A class/course/cohort — e.g. "Grade 10 - A" or "BSc CSE - 3rd Year". Purely a name and an ID; everything interesting about a class (who teaches it, who's in it, what's been assigned) lives in the tables that reference it.

|Column|Type|Notes|
|---|---|---|
|`Id`|guid (PK)||
|`Name`|string||
|`CreatedAt`|datetime||

---

### `Subject`

A subject taught across classes — e.g. "Mathematics". Independent of `SchoolClass`: the same subject can be taught in multiple classes, by different teachers, which is why it's its own table rather than nested under `SchoolClass`.

|Column|Type|Notes|
|---|---|---|
|`Id`|guid (PK)||
|`Name`|string||
|`Code`|string?|Optional short code, e.g. `MATH101`.|
|`CreatedAt`|datetime||

---

### `TeacherAssignment`

**Answers: "which teacher teaches which subject, in which class?"**

This is a pure join table — it has no meaning on its own, only in relation to the `User`, `SchoolClass`, and `Subject` rows it connects. One row means "this Teacher is assigned to teach this Subject for this SchoolClass."

|Column|Type|Notes|
|---|---|---|
|`Id`|guid (PK)||
|`TeacherId`|guid (FK → User)|Must be a `User` with `Role = Teacher` — enforced in application code, not the database.|
|`SchoolClassId`|guid (FK → SchoolClass)||
|`SubjectId`|guid (FK → Subject)||
|`CreatedAt`|datetime||

**Constraint:** `(TeacherId, SchoolClassId, SubjectId)` is unique — the same teacher can't be assigned to the same subject+class combination twice. This is enforced by the database itself (a unique index), so it holds even under concurrent requests, not just when the application remembers to check first.

---

### `StudentEnrollment`

**Answers: "which student belongs to which class?"**

Also a pure join table. One row means "this Student is enrolled in this SchoolClass." This is what determines which assignments a student can see and submit to — a student only sees assignments for classes they appear here for.

|Column|Type|Notes|
|---|---|---|
|`Id`|guid (PK)||
|`StudentId`|guid (FK → User)|Must be a `User` with `Role = Student` — enforced in application code.|
|`SchoolClassId`|guid (FK → SchoolClass)||
|`CreatedAt`|datetime||

**Constraint:** `(StudentId, SchoolClassId)` is unique — a student can't be enrolled in the same class twice.

---

### `Assignment`

An assignment a teacher has created for a specific class and subject.

|Column|Type|Notes|
|---|---|---|
|`Id`|guid (PK)||
|`Title`|string||
|`Description`|string||
|`Deadline`|datetime||
|`MaxMarks`|int||
|`Status`|string|`Draft` (not visible to students) or `Published`.|
|`TeacherId`|guid (FK → User)|The teacher who created it — used to check "can this teacher edit/delete this assignment?"|
|`SchoolClassId`|guid (FK → SchoolClass)||
|`SubjectId`|guid (FK → Subject)||
|`CreatedAt` / `UpdatedAt`|datetime||

**Why `TeacherId` is stored here directly**, rather than derived by looking up `TeacherAssignment`: it answers a different question. `TeacherAssignment` says "who's _generally_ assigned to teach this subject/class" (which can change over a term). `Assignment.TeacherId` says "who _specifically_ created this piece of coursework" — the two can diverge (a substitute teacher creates one assignment; the regular teacher still owns the rest), and only the second question matters for edit/delete permissions.

---

### `Submission`

A student's answer to a specific assignment, plus its grading state.

|Column|Type|Notes|
|---|---|---|
|`Id`|guid (PK)||
|`AssignmentId`|guid (FK → Assignment)||
|`StudentId`|guid (FK → User)||
|`AnswerText`|string|The student's answer.|
|`SubmittedAt`|datetime||
|`Status`|string|`Submitted`, `Late` (set automatically if `SubmittedAt > Assignment.Deadline`), or `Graded`.|
|`Marks`|int?|Null until graded.|
|`Feedback`|string?|Null until graded.|
|`GradedByTeacherId`|guid?|Which teacher graded it. Nullable, no formal foreign-key relationship configured — see note below.|
|`GradedAt`|datetime?|Null until graded.|

**Constraint:** `(AssignmentId, StudentId)` is unique — one submission per student per assignment. Whether a student can _update_ their existing submission before the deadline (vs. being blocked entirely) is an application-level rule, not a database one.

**Why `GradedByTeacherId` has no formal relationship**: `Submission` already has one foreign key pointing at `User` (`StudentId`). Adding a second, properly-configured foreign key to the same table needs explicit disambiguation in the ORM — real but avoidable complexity until the project actually needs to query "show me everything Teacher X has graded." For now it's stored as a plain ID.

---

## How the tables answer real questions

|Question|How to answer it|
|---|---|
|"What classes is this teacher teaching, and what subjects?"|Query `TeacherAssignment` filtered by `TeacherId`.|
|"What students are in this class?"|Query `StudentEnrollment` filtered by `SchoolClassId`.|
|"What assignments can this student see?"|Find their classes via `StudentEnrollment`, then `Assignment` rows for those classes where `Status = Published`.|
|"Can this teacher create an assignment for Class X, Subject Y?"|Check `TeacherAssignment` for a matching `(TeacherId, SchoolClassId, SubjectId)` row.|
|"Has this student already submitted?"|Check `Submission` for a matching `(AssignmentId, StudentId)` row — the unique constraint guarantees at most one exists.|
|"What's this student's grade history?"|Query `Submission` filtered by `StudentId`, joined to `Assignment` for context.|

## What the database enforces vs. what the application enforces

The schema is deliberately not the only line of defense — some rules live in the database (so they hold even under bugs or concurrent requests), others live in application code (because the database can't express them).

**Enforced by the database (unique constraints, foreign keys):**

- No duplicate teacher-subject-class assignments.
- No duplicate student-class enrollments.
- No duplicate submissions per student per assignment.
- Every `TeacherId`/`StudentId`/`SchoolClassId`/`SubjectId`/`AssignmentId` reference points at a row that actually exists.

**Enforced by application code (role checks, workflow rules):**

- That a `TeacherId` foreign key actually points at a `User` with `Role = Teacher` (the database doesn't know the difference between a Teacher-shaped and Student-shaped `User` row).
- That a student can only submit to assignments for classes they're enrolled in.
- That only the assigned teacher can create/edit an assignment for a given class+subject.
- Deadline-based logic (marking a submission `Late`, blocking submissions after the deadline).