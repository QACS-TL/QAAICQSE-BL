# Lab 1 - Evaluating AI generated code quality

**Goal:** learn to critically review AI output.

**Challenge project:** Task management API with complex business logic.

**Scenario:** You are building a project management system where tasks have dependencies, priority levels, and resource allocation constraints.

## AI prompting task

Use AI to generate a `TaskManager` that can:

1. Add tasks with dependencies (prerequisite tasks)
2. Detect circular dependencies
3. Calculate earliest start dates
4. Assign resources without conflicts
5. Generate a project timeline with a critical path

## What to review

`Lab 1/Exemplar` is a typical AI-generated starting point. Review it with the checklist in `AI-Checklist.md` and prioritise the issues you find. `Lab 1/Improved` is a reference solution: input validation, depth-first cycle detection, memoized earliest-start calculation, and a critical path.

## Expected discoveries

- Missing input validation
- Incorrect cycle detection (a shared dependency is treated as a cycle)
- No memoization, so earliest-start calculation is exponential
- Requirements such as resource assignment are named but not implemented
