# QAECSAISE - C# edition

C# edition of the one-day course *Ensuring code quality and security in AI assisted software development*. The labs match the Python edition: same exercises, same intentional weaknesses in the exemplars, and the same reference solutions.

These folders are ready to become a git repository (`git init` in this directory). Build output is ignored.

## Prerequisites

- .NET 10.0 SDK
- Git, when you want to publish the repo

## Build and test

```powershell
dotnet build QAECSAISE.sln
dotnet test QAECSAISE.sln
dotnet run --project copilotDemo
```

`dotnet test` reports one failure in Lab 2 Improved: `ApplyDiscounts_FixedDiscountDoesNotMakeTotalNegative`. The Python suite fails the same test. `NEWUSER` requires a minimum order of 100, and the test uses an order of 10, so the discount is not applied and the total stays 10.

## Labs

| Lab | Exemplar | Improved |
| --- | --- | --- |
| 1. Review AI-generated task scheduling | `Lab 1/Exemplar` | `Lab 1/Improved` |
| 2. Test and refactor a pricing engine | `Lab 2/Exemplar` | `Lab 2/Improved` |
| 3. Find and fix authentication weaknesses | `Lab 3/OWASP` and `Lab 3/Exemplar` | `Lab 3/Improved` |
| 4. Write an AI governance policy | `docs/Lab4.md` |  |

`copilotDemo` is the email-validation walkthrough.

Student sheets and the review checklist are in `docs/`.

Exemplar projects are flawed on purpose. Lab 3 contains insecure patterns (string-built SQL, MD5, a hard-coded signing secret, missing authorization) so the class can find and replace them. Use `Lab 3/Improved` as the reference, and supply its signing key from configuration.
