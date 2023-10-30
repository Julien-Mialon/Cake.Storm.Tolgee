using Cake.Core;
using Cake.Storm.Tolgee.Configurations;

namespace Cake.Storm.Tolgee;

public class ConfigurationBuilder : IInputConfigurationBuilder, IOutputConfigurationBuilder
{
	private readonly FluentContext _context;
	private readonly Configuration _configuration = new();

	public ConfigurationBuilder(FluentContext context)
	{
		_context = context;
	}

	public ConfigurationBuilder UseTolgee(string host, string apiKey)
	{
		_configuration.Tolgee.Host = host;
		_configuration.Tolgee.ApiKey = apiKey;
		return this;
	}

	public ConfigurationBuilder UseDefaultLanguage(string language)
	{
		_configuration.DefaultLanguage = language;
		return this;
	}

	public ConfigurationBuilder WithInputs(Action<IInputConfigurationBuilder> builder)
	{
		builder(this);
		return this;
	}

	public ConfigurationBuilder WithTypescriptOutputs(Action<IOutputConfigurationBuilder> builder)
	{
		_configuration.Output.Type = OutputType.Typescript;
		builder(this);
		return this;
	}

	IInputConfigurationBuilder IInputConfigurationBuilder.AddLanguage(string language)
	{
		_configuration.Input.Languages.Add(new()
		{
			LanguageCode = language,
			SourceFile = $"sources/{language}.json"
		});
		return this;
	}

	IInputConfigurationBuilder IInputConfigurationBuilder.AddLanguage(string language, string sourceFile)
	{
		_configuration.Input.Languages.Add(new()
		{
			LanguageCode = language,
			SourceFile = sourceFile
		});
		return this;
	}

	IOutputConfigurationBuilder IOutputConfigurationBuilder.AddOutput(string language)
	{
		_configuration.Output.Languages.Add(new()
		{
			LanguageCode = language,
			OutputFile = $"{language}.{_configuration.Output.Type.FileExtension()}",
			SourceFiles = new[] { $"sources/{language}.json" }
		});
		return this;
	}


	IOutputConfigurationBuilder IOutputConfigurationBuilder.AddOutput(string language, string outputFile, params string[] sourceFiles)
	{
		if (sourceFiles.Length == 0)
		{
			throw new CakeException($"No source files for output {language} / {outputFile}");
		}

		_configuration.Output.Languages.Add(new()
		{
			LanguageCode = language,
			OutputFile = outputFile,
			SourceFiles = sourceFiles
		});
		return this;
	}

	IOutputConfigurationBuilder IOutputConfigurationBuilder.AddPartialOutput(string language)
	{
		_configuration.Output.Languages.Add(new()
		{
			LanguageCode = language,
			OutputFile = $"{language}.{_configuration.Output.Type.FileExtension()}",
			SourceFiles = new[] { $"sources/{language}.json" },
			IsPartial = true
		});
		return this;
	}

	IOutputConfigurationBuilder IOutputConfigurationBuilder.AddPartialOutput(string language, string outputFile, params string[] sourceFiles)
	{
		if (sourceFiles.Length == 0)
		{
			throw new CakeException($"No source files for partial output {language} / {outputFile}");
		}

		_configuration.Output.Languages.Add(new()
		{
			LanguageCode = language,
			OutputFile = outputFile,
			SourceFiles = sourceFiles,
			IsPartial = true
		});
		return this;
	}

	public void Build()
	{
		if (_configuration.Tolgee.Host is "" || _configuration.Tolgee.ApiKey is "")
		{
			throw new CakeException("Host and API key must be defined");
		}

		if (_configuration.Input.Languages.Count == 0)
		{
			throw new CakeException("At least one language must be defined");
		}

		if (_configuration.DefaultLanguage is "")
		{
			throw new CakeException("Default language must be defined");
		}

		new TaskMaker(_context, _configuration).Make();
	}
}