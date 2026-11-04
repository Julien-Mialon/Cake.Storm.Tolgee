#addin nuget:?package=Cake.Storm.Tolgee&version=$version$

public ConfigurationBuilder ConfigureTolgee()
{
    return TolgeeConfigurationBuilder(Task);
}