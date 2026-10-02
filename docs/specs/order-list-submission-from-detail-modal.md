# Spec: Trimiterea unei ciorne din modalul de detaliu

## Problem Statement

Utilizatorii pot aproba o comandă ciornă din tabelul de comenzi, dar nu și din modalul în care o
revizuiesc sau o editează. Pentru a o aproba, trebuie să închidă modalul și să revină la listă.

## Solution

Adaugă acțiunea „Aprobă” în modalul unei liste de comandă aflate în starea Ciornă. Acțiunea
reutilizează trimiterea existentă a listei, care o mută din Ciornă în starea Submitted, afișată în interfață ca Finalizată. Înainte de orice
salvare sau trimitere, modalul cere confirmarea utilizatorului. La confirmare, salvează modificările
nesalvate, dacă există, apoi trimite lista. Dacă lista nu are produse, acțiunea este dezactivată cu
o explicație. După succes, modalul rămâne deschis și afișează lista în mod read-only.

## User Stories

1. As a user who can submit order lists, I want to approve a draft from its detail modal, so that I
   can complete the workflow without returning to the order list.
2. As a user reviewing a draft, I want the confirmation to explain that submitting finalizes the
   list for the next purchasing step, so that I understand the consequence before proceeding.
3. As a user with unsaved edits, I want approval confirmation before any save occurs, so that
   cancelling leaves the persisted draft untouched.
4. As a user who cancels the confirmation, I want the modal to remain open with my current edits,
   so that I can continue reviewing or editing the draft.
5. As a user who confirms approval with unsaved edits, I want those edits saved before submission,
   so that the submitted list contains the version I reviewed.
6. As a user whose save fails, I want to see the save error and keep the list as an editable draft,
   so that I can correct the problem without an unintended submission.
7. As a user whose submission fails after a successful save, I want the saved list to remain an
   editable draft and the submission error to be shown, so that I can retry or correct the issue.
8. As a user reviewing an empty draft, I want the approval action disabled with a clear explanation,
   so that I know at least one product must be added before submission.
9. As a user who confirms approval for a valid draft, I want the modal to remain open and show the
   submitted list read-only, so that I can verify the final state immediately.
10. As a user waiting for an approval request to finish, I want repeated clicks prevented, so that
    the list is not saved or submitted more than once.
11. As an authorized user, I want modal approval to follow the same access rules as submission from
    the order list, so that the two entry points behave consistently.
12. As a user viewing a submitted or cancelled list, I want no approval action, so that the modal
    continues to respect the existing lifecycle rules.

## Implementation Decisions

- Treat “Aprobă” as the existing order list submission action. The lifecycle remains Ciornă → Trimisă;
  approval is not a separate business state.
- Use the existing submission operation and authorization behavior. Do not add an endpoint, command,
  role restriction, database field, or migration for this feature.
- Show confirmation before saving or submitting. Cancelling performs no persistence and leaves the
  current modal state intact.
- On confirmation, save the current form only when it has unsaved changes. Submit only after that
  save succeeds. If the form is already clean, submit without a no-op save.
- If saving fails, do not submit. If submission fails after the save succeeds, keep the persisted
  changes as a draft and allow another attempt.
- Disable approval when the draft has no product lines and explain the existing minimum-product
  requirement.
- After a successful submission, keep the modal open, show the existing Finalizată label for Submitted, and switch to the
  existing read-only presentation.
- Preserve the existing behavior and placement of lifecycle actions in the order list.

## Testing Decisions

- Use one Playwright browser-level seam for the user-visible modal flow, with API responses
  intercepted so the test can verify both displayed behavior and request order.
- A good test asserts external behavior: confirmation and cancellation, absence of persistence on
  cancellation, save-before-submit ordering for dirty drafts, no submission after a save failure,
  disabled approval for an empty draft, and the read-only modal after successful submission. It
  should not assert private component methods or internal state implementation.
- Cover the order detail modal in the Client E2E suite. Reuse the established Playwright route
  interception pattern used by existing browser tests.
- Do not add backend tests for this UI change: the submission endpoint and its draft/line validation
  are unchanged and already covered by existing application tests.

## Out of Scope

- A new approval status or a separate approval workflow.
- Changes to the submission endpoint, domain lifecycle, authorization policy, or database schema.
- Changes to submit, cancel, or reopen actions outside the detail modal.
- Approval of drafts with no product lines.

## Further Notes

The order list is a plan, not a purchase or goods receipt. Submission finalizes that plan for the
next purchasing step; it does not mean the products have been purchased or received. The project
glossary records this distinction in `CONTEXT.md`.
