# Configurare departamente și invitații pentru clase

Status: ready-for-agent
Labels: ready-for-agent

## Problem Statement

Administrators need a shared place to define the departments and invitation count used when a school class is created. Each class needs its own responsible person for each department and must retain the configuration it originally received. Changing global defaults must not change existing classes.

The application currently manages school classes but has no department templates, class departments, invitation count, or shared configuration page for these concepts.

## Solution

Add a page reached through a new “Configurare” menu item. Administrators can create, edit, and delete department templates containing a name and responsibilities, and explicitly save a nonnegative integer invitation count. The department count is derived from the template list.

New classes automatically copy the saved templates and invitation count. Their page displays the copied values and lets users complete each department's responsible person as free text. Only the responsible person is editable within the class. Existing classes can explicitly copy the current saved configuration once. Later global changes affect subsequent copies only.

## User Stories

1. As an administrator, I want a “Configurare” menu item, so that I can find the shared defaults in one place.
2. As an administrator, I want to see the saved department templates, so that I know what new classes will receive.
3. As an administrator, I want to add as many department templates as needed, so that the configuration matches our organization.
4. As an administrator, I want to name each department template using free text, so that our terminology is preserved.
5. As an administrator, I want to describe each template's responsibilities using free text, so that each department's purpose is clear.
6. As an administrator, I want to edit department template names and responsibilities, so that future classes use revised defaults.
7. As an administrator, I want to delete a department template, so that subsequent classes no longer receive it.
8. As an administrator, I want the department count to follow the template list, so that I do not maintain a separate inconsistent count.
9. As an administrator, I want empty department names and responsibilities rejected, so that templates are meaningful.
10. As an administrator, I want duplicate template names rejected, so that departments can be distinguished.
11. As an administrator, I want to save a fixed integer invitation count, so that subsequent classes receive that count.
12. As an administrator, I want to save zero invitations, so that classes that need none are supported.
13. As an administrator, I want negative and noninteger invitation counts rejected, so that the configuration remains valid.
14. As an administrator, I want to explicitly save an empty department list with zero invitations, so that an intentional empty configuration is usable.
15. As an administrator, I want saved configuration to survive reopening the page, so that my changes remain available.
16. As a user creating a class, I want a clear message when shared configuration has never been saved, so that I know what must happen before creation.
17. As a user creating a class, I want saved department templates copied automatically, so that I do not recreate their names and responsibilities.
18. As a user creating a class, I want the saved invitation count copied automatically, so that the class starts with the agreed default.
19. As a class user, I want to see copied department names and responsibilities, so that I understand the class's organization.
20. As a class user, I want responsible people initially left blank, so that the class can be created before assignments are known.
21. As a class user, I want to record and edit a responsible person as free text, so that the person need not have a user account.
22. As a class user, I want to leave a responsible person blank, so that an unassigned department is supported.
23. As a class user, I want responsible people saved separately for each class, so that assignments in one class do not change another.
24. As a class user, I want assignments to survive reopening the class, so that I can rely on the recorded information.
25. As a class user, I want the copied department names, responsibilities, and list protected from class-level changes, so that the original configuration is preserved.
26. As a class user, I want to see the copied invitation count without being able to edit it, so that the class retains its initial value.
27. As a class user, I want my departments preserved after a global template is edited or deleted, so that ongoing class organization is stable.
28. As a class user, I want my invitation count preserved after a global change, so that the initial allocation is stable.
29. As a user creating a later class, I want the latest saved defaults, so that subsequent classes receive current configuration.
30. As a user managing an existing class, I want an explicit action to copy the saved configuration, so that older classes can adopt this feature deliberately.
31. As a user managing an existing class, I want that action to copy both departments and invitation count, so that the class receives a complete configuration.
32. As a user managing an existing class, I want copying limited to once, so that initial values and recorded responsible people cannot be overwritten by another copy.
33. As a user managing an existing class, I want copying an empty list and zero invitations to count as completed, so that empty values do not accidentally enable another copy.
34. As a nonadministrator, I want global changes restricted to administrators, so that shared defaults remain under the agreed control.
35. As a mobile user, I want navigation, department forms, and class assignments to fit a narrow screen, so that I can use the feature without horizontal page scrolling.
36. As a tablet or desktop user, I want the available space used with a clear visual hierarchy, so that I can scan departments and edit efficiently.
37. As a user, I want visible field labels, nearby validation messages, and clear save feedback, so that I understand what to enter and whether my changes were saved.
38. As a keyboard user, I want logical focus order and visible focus indicators, so that I can navigate and complete the forms.
39. As a user reading long names or responsibilities, I want readable wrapping and accessible full content, so that important information is not lost at any screen width.

## Implementation Decisions

- Use the glossary concepts Department template (Model de departament), Class department (Departament al clasei), and Invitation count (Număr de invitații).
- Follow ADR 0002: classes own copied values, and global template edits or deletions do not propagate to existing copies.
- Extend the shared configuration capability with persisted department templates, a persisted invitation count, and an explicit indication that configuration has been saved. An unsaved configuration is distinct from a saved empty configuration.
- Extend the school-class model with persisted class departments, the copied invitation count, and an explicit indication that configuration has been copied. Zero invitations and an empty department list must not imply an uninitialized class.
- Class departments persist copied names and responsibilities plus an optional responsible person as free text. They must survive deletion of their source template.
- Invitation counts use `int` and accept zero; reject negative values, fractions, and values outside the supported integer range.
- Department template names and responsibilities are required text. Names are unique within shared configuration. The number of departments is derived from the list, with no preset business limit.
- Global configuration mutation requires the Administrator role, enforced by the server as well as the client. Preserve existing authorization rules for class access and responsible-person editing.
- Add Angular navigation and the configuration page, and extend the class page with copied departments, responsible-person editing, invitation-count display, and the one-time action for existing uninitialized classes.
- Follow the existing Angular/ng-zorro design system, typography, spacing, semantic colors, and navigation patterns. Present clear sections for department templates and invitation count, with an obvious primary save action and distinct destructive actions.
- Design from narrow screens upward using the project's existing breakpoint scale. Keep fields, department actions, and long responsibilities readable, with no clipped controls, overlapping content, or horizontal page overflow. Use the existing drawer below the navigation threshold and sidebar at or above it.
- Provide visible input labels, required-field indications, inline validation, clear empty/loading/error/success states, and save feedback. Explain that global changes apply to future copies and that existing-class initialization happens once. Use accessible contrast, keyboard operation, visible focus, logical heading order, and touch-friendly actions.
- Extend the HTTP/Application boundary with operations to read and save global configuration, read class copies, initialize an existing class once, and update a class department's responsible person. Exact route names and DTO member names are implementation details to align with existing conventions.
- New-class creation requires saved global configuration and copies the current saved values as part of creation. Existing-class initialization requires saved configuration and copies the current values once.
- Class-level mutation contracts permit responsible-person changes only. The server must reject attempts to change copied department names, responsibilities, the department list, or invitation count.
- Persist the complete class copy atomically. Concurrent requests to initialize the same existing class must not create duplicate departments or replace its first copy.
- Existing classes remain uninitialized until the explicit action is used. Schema evolution must preserve their current data and allow them to remain usable before initialization.
- Keep Domain definitions separate from Infrastructure persistence and route business operations through Application handlers using the existing database abstraction, validation, result mapping, and cache invalidation conventions. Regenerate EF migration artifacts through repository tooling.
- Read operations must reflect saved configuration and responsible-person changes after successful mutations, including when existing caches have been populated.

## Testing Decisions

The user confirmed the testing approach with one change: no Playwright test suite for this feature for now. UI verification must use screenshots in a real browser at every project breakpoint and include an explicit UI/UX review.

- Use the highest existing backend boundary suitable for each behavior: HTTP integration tests through the existing Web host for authorization and public request contracts, and existing Application functional tests through the Mediator pipeline with the real database for class/configuration lifecycle behavior. Reuse the existing host, identity helpers, and database reset mechanisms; introduce no new production testing interfaces.
- A good test checks external behavior: values returned through supported reads, persisted values after reopening, meaningful validation failures, and rejection of unauthorized or prohibited requests. Do not assert private handlers, EF tracking internals, DOM structure, or implementation call counts.
- Prior art includes existing class creation/query functional tests, HTTP tests for authentication restrictions, administrator/default-user helpers, and database reset support.
- Cover the configuration, school-class creation and initialization, class-department assignment, and authorization capabilities. Exercise the complete scenario: save departments with `30` invitations, create class A, record responsible people, revise/delete global templates and change invitations to `40`, reopen A and verify it is unchanged, then create B and verify the new defaults.
- Verify independent class assignments, optional empty responsible people, required template text, duplicate names, invalid invitation values, and explicit saving of an empty list with zero invitations.
- Verify blocked class creation/initialization before the first configuration save, successful use of explicitly saved empty defaults, and one-time initialization even for empty copies and repeated/concurrent requests.
- Verify nonadministrator global writes and prohibited class-level changes are rejected by the server through direct requests. Verify mutations remain visible after cached reads.
- Isolate test data and identities; serialize shared-configuration mutations or reset between scenarios so tests do not race or leave defaults changed for other tests.
- Verify the implemented configuration page and affected class page in a real browser. Capture and visually inspect screenshots; writing automated Playwright tests is deferred.
- Cover the project's actual CSS viewport breakpoints: `xs` 480 px, `sm` 576 px, `md` 768 px, `lg` 992 px, `xl` 1200 px, `xxl` 1600 px, and `xxxl` 1920 px. Capture at each threshold and immediately below and above it (threshold minus 1, threshold, threshold plus 1), plus representative narrow mobile widths of 320 and 375 px. Use viewport widths in CSS pixels and record the dimensions of each capture.
- Check the navigation transition at 992 px, menu accessibility, section hierarchy, form alignment, department actions, text wrapping, touch targets, and visibility of the primary save action. Inspect long realistic names/responsibilities and both empty and populated department lists; scroll through the whole page rather than judging only the initial viewport.
- Capture applicable empty, validation-error, loading, saved, and uninitialized/already-initialized class states at representative narrow and wide widths. Check the normal populated state at every breakpoint boundary.
- Supplement screenshots with live keyboard/focus and interaction checks, save/reopen behavior, and inspection for horizontal overflow and browser console errors. Screenshots alone cannot establish interaction correctness or accessibility.
- Apply a UI/UX finish review: consistent existing design language, clear grouping and primary action, readable hierarchy and contrast, helpful labels/messages, preserved user input on errors, and understandable copied-versus-global values. Fix issues and recapture affected widths/states before completion.
- Retain screenshots and a concise visual-verification report with viewport, page/state, findings, and any remaining limitations as implementation evidence. This specification defines future verification; screenshots have not been captured during specification publication.

## Out of Scope

- Automated Playwright tests for this feature at this stage.
- Generating, sending, tracking, or allocating individual invitations.
- Linking responsible people to user accounts or introducing a people directory.
- Editing copied department names, responsibilities, the department list, or invitation count within a class.
- Automatically propagating global changes into existing classes, resynchronization, or repeated copying.
- Automatically initializing all existing classes during migration.
- Per-student invitation calculations or allocation rules beyond storing the agreed class count.
- Reworking unrelated inventory, ordering, worker, authentication, or navigation capabilities.

## Further Notes

Functional requirements were agreed in the `grill-with-docs` session on 2026-10-02 and recorded in the domain glossary, ADR 0002, and the existing class-configuration requirements document.

The project issue tracker is Local Markdown. This specification is published under the class-configuration feature directory with the `ready-for-agent` label. Product requirements and the revised verification approach are confirmed; no further product interview or triage is needed.

The explicit saved/initialized states are essential: valid empty configurations must not be confused with configuration that has never been saved or copied.
