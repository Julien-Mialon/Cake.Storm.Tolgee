# Cake.Storm.Tolgee
A set of aliases for Cake to interact with tolgee translation platform.

## Configuration

```csharp
ConfigureTolgee()
    .UseTolgee("https://app.tolgee.io", EnvironmentVariable("TOLGEE_API_KEY"))
    .UseProjectId(42)
    .UseNamespace("mobile")
    .UseDefaultLanguage("en")
    .WithInputs(inputs => inputs.AddLanguage("en").AddLanguage("fr"))
    .Build();
```

- `UseProjectId(long projectId)`: required with a personal access token (`tgpat_...`). Optional with a project API key, which already identifies its project.
- `UseNamespace(string name)`: namespace used by downloads and uploads. Defaults to `string`. Pass `""` to use the default Tolgee namespace.
