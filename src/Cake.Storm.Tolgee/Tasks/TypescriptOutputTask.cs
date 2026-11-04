using System.Text.RegularExpressions;
using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Configurations;

namespace Cake.Storm.Tolgee.Tasks;

internal class TypescriptOutputTask : BaseOutputTask
{
	private readonly string _defaultLanguage;
	private readonly List<OutputLanguageConfiguration> _languages;

	public TypescriptOutputTask(ICakeLog log, List<OutputLanguageConfiguration> languages, string defaultLanguage, bool minify) : base(log, minify)
	{
		_languages = languages;
		_defaultLanguage = defaultLanguage;
	}

	public async Task Run()
	{
		Log.Information("Generating typescript files");
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

			string nl = Environment.NewLine;
			List<string> lines = new();
			if (languageConfiguration.LanguageCode == _defaultLanguage)
			{
				lines.Add($"const {languageConfiguration.LanguageCode}Strings = {{");
			}
			else
			{
				lines.Add("import { RawStrings } from \"./types\";");
				lines.Add("");
				lines.Add("");
				if (languageConfiguration.IsPartial)
				{
					lines.Add($"const {languageConfiguration.LanguageCode}Strings: Partial<RawStrings> = {{");
				}
				else
				{
					lines.Add($"const {languageConfiguration.LanguageCode}Strings: RawStrings = {{");
				}
			}

			foreach (string key in keys)
			{
				string referenceTranslation = defaultTranslations[key];
				string tr = "";
				if (translations.TryGetValue(key, out string? translation) && !string.IsNullOrEmpty(translation))
				{
					tr = translation;
				}
				else if (languageConfiguration.IsPartial)
				{
					continue;
				}
				else
				{
					tr = referenceTranslation;
				}

				if (AreTokenValid(key, languageConfiguration.LanguageCode, referenceTranslation, tr) is false)
				{
					tr = referenceTranslation;
				}

				tr = tr
					.Replace("\"", "\\\"")
					.Replace("\t", "\\t")
					.Replace("\n", "\\n");
				lines.Add($"\t{PropertyName(key)}: \"{tr}\",");
			}

			lines.Add("};");
			lines.Add("");
			lines.Add($"export default {languageConfiguration.LanguageCode}Strings;");
			lines.Add("");

			string result = string.Join(nl, lines);
			await File.WriteAllTextAsync(languageConfiguration.OutputFile, result);
		}
	}

	private static string PropertyName(string key)
	{
		return Regex.IsMatch(key, @"^[A-Za-z_$][A-Za-z0-9_$]*$") ? key : $"\"{key}\"";
	}
}