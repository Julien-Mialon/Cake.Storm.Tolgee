using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Configurations;
using Newtonsoft.Json;

namespace Cake.Storm.Tolgee.Tasks;

internal class TypescriptOutputTask : BaseTask
{
	private readonly string _defaultLanguage;
	private readonly List<OutputLanguageConfiguration> _languages;

	public TypescriptOutputTask(ICakeLog log, List<OutputLanguageConfiguration> languages, string defaultLanguage) : base(log)
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
				lines.Add($"\t{key}: \"{tr}\",");
			}

			lines.Add("};");
			lines.Add("");
			lines.Add($"export default {languageConfiguration.LanguageCode}Strings;");
			lines.Add("");

			string result = string.Join(nl, lines);
			await File.WriteAllTextAsync(languageConfiguration.OutputFile, result);
		}
	}

	private HashSet<string> ExtractTokens(string s)
	{
		int tokenStart = s.IndexOf("{", StringComparison.InvariantCulture);
		if (tokenStart < 0)
		{
			return new();
		}

		HashSet<string> tokens = new();
		while (tokenStart >= 0)
		{
			int tokenEnd = s.IndexOf("}", tokenStart + 1, StringComparison.InvariantCulture);

			string token = s.Substring(tokenStart + 1, tokenEnd - tokenStart - 1);
			tokens.Add(token);

			tokenStart = s.IndexOf("{", tokenStart + 1, StringComparison.InvariantCulture);
		}

		return tokens;
	}

	private bool AreTokenValid(string key, string language, string reference, string translation)
	{
		HashSet<string> referenceTokens = ExtractTokens(reference);
		if (referenceTokens.Count == 0)
		{
			return true;
		}

		HashSet<string> translationTokens = ExtractTokens(translation);
		List<string> referenceTokensList = new(referenceTokens);

		List<string> invalidTokens = new();
		foreach (string translationToken in translationTokens)
		{
			if (referenceTokens.Remove(translationToken) == false)
			{
				invalidTokens.Add(translationToken);
			}
		}

		if (invalidTokens.Count == 0 && referenceTokens.Count == 0)
		{
			return true;
		}

		invalidTokens.AddRange(referenceTokens);

		Log.Warning($"Invalid tokens {language}: {key} => {string.Join(", ", invalidTokens.Select(x => $"{{{x}}}"))}");
		Log.Information($"\tReference: {reference} (extractedTokens: #{string.Join("#, #", referenceTokensList)}#)");
		Log.Information($"\tTranslation: {translation} (extractedTokens: #{string.Join("#, #", translationTokens)}#)");

		if (translation.StartsWith(' ') && !reference.StartsWith(' '))
		{
			Log.Warning($"Missing leading space {language}: {key}");
			Log.Information($"\t#{reference}#");
			Log.Information($"\t#{translation}#");
		}
		else if (reference.StartsWith(' '))
		{
			Log.Warning($"Additional leading space {language}: {key}");
			Log.Information($"\t#{reference}#");
			Log.Information($"\t#{translation}#");
		}

		if (translation.EndsWith(' ') && !reference.EndsWith(' '))
		{
			Log.Warning($"Missing trailing space {language}: {key}");
			Log.Information($"\t#{reference}#");
			Log.Information($"\t#{translation}#");
		}
		else if (reference.EndsWith(' '))
		{
			Log.Warning($"Additional trailing space {language}: {key}");
			Log.Information($"\t#{reference}#");
			Log.Information($"\t#{translation}#");
		}

		return false;
	}

	private async Task<Dictionary<string, string>> LoadTranslationFiles(string[] files)
	{
		Dictionary<string, string> result = new();
		foreach (string file in files)
		{
			Dictionary<string, string>? data = JsonConvert.DeserializeObject<Dictionary<string, string>>(await File.ReadAllTextAsync(file));
			if (data is null)
			{
				continue;
			}

			foreach (KeyValuePair<string, string> kvp in data)
			{
				result.Add(kvp.Key, kvp.Value);
			}
		}

		return result;
	}
}