# AI-assisted code review checklist

## Correctness and functionality

- Does the code compile and run?
- Are the prompt requirements actually implemented?
- Are inputs validated and edge cases handled?
- Are assumptions explicit?
- Does the code produce the expected output for sample cases?

## Readability and maintainability

- Is the code clear?
- Are names meaningful and consistent?
- Is there unnecessary complexity?
- Are comments accurate?
- Does the style match the project?
- Could another developer change it later?

## Test coverage

- Are there tests for the key behaviour?
- Do tests cover success and failure?
- Are tests deterministic?
- Is coverage adequate, above 80 percent where that is the team bar?
- Is test data realistic, and are secrets kept out of tests?

## Security

- Is user input validated?
- Does the code avoid insecure functions and libraries?
- Are secrets kept out of source and logs?
- Are authentication and authorization enforced?
- Are database, file, and API calls safe?
- Have dependencies been scanned?

## Performance

- Is the algorithm appropriate for the expected data volume?
- Are there needless nested loops or repeated calls?
- Are external calls batched when that is possible?
- Is memory released, and are connections closed?

## Reliability

- Are exceptions handled without hiding the cause?
- Is there a recovery path where one is needed?
- Are log messages useful, and do they omit sensitive data?

## Ethical and legal

- Does the code copy licensed material?
- Is AI-generated content attributable if the team requires that?
- Does the change comply with the organization's AI-use policy?

## Documentation

- Is there a short explanation of what the code does?
- Are non-obvious choices explained?
- Are setup and dependencies documented?
