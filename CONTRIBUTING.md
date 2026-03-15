# Contributing

Thanks for considering a contribution to SonyDev Cleaner.

## Before You Start

- The project is Windows-only.
- Use .NET 8 SDK.
- Keep changes focused and reviewable.
- Prefer safe cleanup behavior over aggressive optimization ideas.

## Local Setup

```powershell
dotnet restore
dotnet build SonyDevCleaner.sln
```

To run the app:

```powershell
dotnet run --project .\SonyDevCleaner.App\SonyDevCleaner.App.csproj
```

## Pull Request Guidelines

- Open one PR per logical change.
- Include a short summary of what changed and why.
- Mention any Windows version assumptions.
- If UI is changed, include screenshots.
- If behavior changed, describe how you validated it.

## Code Style

- Keep the existing project structure unless there is a clear reason to refactor.
- Avoid adding unsafe cleanup targets without a strong justification.
- Do not add registry cleaning or "tweak" features.
- Keep UI changes consistent with the current visual language.

## Testing Expectations

At minimum, before opening a PR:

```powershell
dotnet build SonyDevCleaner.sln
```

If your change touches cleanup behavior, also validate:

- scan still works
- cleanup still works
- restricted/elevation states still display correctly

## Discussions and Issues

- Use GitHub Issues for bugs, feature requests, and questions.
- Use private reporting for security issues. See [SECURITY.md](./SECURITY.md).
