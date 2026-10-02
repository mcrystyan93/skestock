# ADR 0002: Preserve copied configuration in school classes

- **Status:** Accepted
- **Date:** 2026-10-02

## Context

Shared configuration defines department templates and an invitation count for new school classes.

## Decision

Each new class copies the department names, responsibilities, and invitation count and retains these values when the
shared configuration is edited or a department template is deleted; responsible people are recorded separately as free
text on each class department.

## Consequences

This preserves the configuration under which a class was created while allowing later classes to use revised defaults.
Referencing live global values was considered, but would change existing classes whenever an administrator updated the
shared configuration.
