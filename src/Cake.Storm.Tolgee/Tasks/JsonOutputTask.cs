using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Configurations;

namespace Cake.Storm.Tolgee.Tasks;

internal class JsonOutputTask : BaseOutputTask
{
	private readonly List<OutputLanguageConfiguration> _languages;
	private readonly string _defaultLanguage;

	public JsonOutputTask(ICakeLog log, List<OutputLanguageConfiguration> languages, string defaultLanguage, bool minify) : base(log, minify)
	{
		_languages = languages;
		_defaultLanguage = defaultLanguage;
	}

	public async Task Run()
	{
		Log.Information("Generating json files");
		await UpdateTranslationFiles();
	}

	private async Task UpdateTranslationFiles()
	{
		Dictionary<string, Dictionary<string, string>> allTranslations = new();
		foreach (OutputLanguageConfiguration languageConfiguration in _languages)
		{
			allTranslations.Add(languageConfiguration.LanguageCode, await LoadTranslationFiles(languageConfiguration.SourceFiles));
		}

		Dictionary<string, string> defaultTranslations = allTranslations[_defaultLanguage];
		List<string> keys = allTranslations.SelectMany(x => x.Value.Keys).Distinct().OrderBy(x => x).ToList();

		foreach (OutputLanguageConfiguration languageConfiguration in _languages)
		{
			Dictionary<string, string> translations = allTranslations[languageConfiguration.LanguageCode];

			Dictionary<string, string> result = new();
			foreach (string key in keys)
			{
				string referenceTranslation = defaultTranslations[key];
				string tr = "";
				if (translations.TryGetValue(key, out string? translation) && !string.IsNullOrWhiteSpace(translation))
				{
					tr = translation;
				}
				else
				{
					tr = referenceTranslation;
				}

				if (AreTokenValid(key, languageConfiguration.LanguageCode, referenceTranslation, tr) is false)
				{
					tr = referenceTranslation;
				}

				result.Add(key, tr);
			}

			await MakeJsonFile(result, languageConfiguration.OutputFile);
		}
	}
}