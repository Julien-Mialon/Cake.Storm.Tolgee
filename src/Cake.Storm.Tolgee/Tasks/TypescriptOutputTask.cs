﻿using Cake.Core.Diagnostics;
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
		List<string> keys = defaultTranslations.Keys.OrderBy(x => x).ToList();

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
				lines.Add($"const {languageConfiguration.LanguageCode}Strings: RawStrings = {{");
			}

			foreach (string key in keys)
			{
				string referenceTranslation = defaultTranslations[key];
				string tr = translations.ContainsKey(key) && !string.IsNullOrEmpty(translations[key]) ? translations[key] : referenceTranslation;
				if (AreTokenValid(key, languageConfiguration.LanguageCode, referenceTranslation, tr) == false)
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
			await File.WriteAllTextAsync($"../{languageConfiguration.LanguageCode}.ts", result);
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