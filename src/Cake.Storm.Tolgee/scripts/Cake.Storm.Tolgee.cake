#addin Cake.Storm.Tolgee

public ConfigurationBuilder ConfigureTolgee()
{
    return TolgeeConfigurationBuilder(Task, Setup, Teardown, TaskSetup, TaskTeardown);
}