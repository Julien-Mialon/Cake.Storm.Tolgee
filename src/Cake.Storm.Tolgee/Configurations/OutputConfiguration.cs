namespace Cake.Storm.Tolgee.Configurations;

public class OutputConfiguration
{
	public OutputType Type { get; set; } = OutputType.Typescript;

	public List<OutputLanguageConfiguration> Languages { get; } = new();
}