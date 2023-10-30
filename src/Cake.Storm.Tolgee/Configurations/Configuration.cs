namespace Cake.Storm.Tolgee.Configurations;

public class Configuration
{
	public string DefaultLanguage { get; set; } = "";

	public TolgeeConfiguration Tolgee { get; } = new();

	public InputConfiguration Input { get; } = new();

	public OutputConfiguration Output { get; } = new();
}