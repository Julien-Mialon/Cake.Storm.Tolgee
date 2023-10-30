namespace Cake.Storm.Tolgee.Configurations;

public class OutputLanguageConfiguration
{
	public string LanguageCode { get; set; } = "";

	public string OutputFile { get; set; } = "";

	public string[] SourceFiles { get; init; } = Array.Empty<string>();

	public bool IsPartial { get; set; }
}