namespace Cake.Storm.Tolgee;

public interface IOutputConfigurationBuilder
{
	IOutputConfigurationBuilder AddOutput(string language);
	IOutputConfigurationBuilder AddOutput(string language, string outputFile, params string[] sourceFiles);
}