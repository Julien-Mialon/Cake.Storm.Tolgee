namespace Cake.Storm.Tolgee.Configurations;

public class TolgeeConfiguration
{
	public string Host { get; set; } = "";

	public string ApiKey { get; set; } = "";

	public long? ProjectId { get; set; }

	// null targets Tolgee's default namespace
	public string? Namespace { get; set; } = "string";
}