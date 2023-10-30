namespace Cake.Storm.Tolgee;

public interface IInputConfigurationBuilder
{
	IInputConfigurationBuilder AddLanguage(string language);
	IInputConfigurationBuilder AddLanguage(string language, string sourceFile);
}