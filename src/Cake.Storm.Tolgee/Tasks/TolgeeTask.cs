using Cake.Core.Diagnostics;
using Newtonsoft.Json;

namespace Cake.Storm.Tolgee.Tasks;

public abstract class TolgeeTask
{
	protected string Host { get; }
	protected string ApiKey { get; }
	protected List<string> Languages { get; }
	protected ICakeLog Log { get; }

	protected TolgeeTask(string host, string apiKey, List<string> languages, ICakeLog log)
	{
		Host = host;
		ApiKey = apiKey;
		Languages = languages;
		Log = log;
	}

	public virtual async Task Run()
	{
		UpdateTranslationFiles();
	}

	private void UpdateTranslationFiles()
	{
		Dictionary<string, string[]> files = new();
		foreach (string lang in Languages)
		{
			files.Add(lang, new[] { $"sources/{lang}.json" });
		}

		Dictionary<string, string> defaultTranslations = LoadTranslationFiles(files["en"]);
		List<string> keys = defaultTranslations.Keys.OrderBy(x => x).ToList();

		foreach (KeyValuePair<string, string[]> fileInfo in files)
		{
			string lang = fileInfo.Key;
			string[] langFiles = fileInfo.Value;
			Dictionary<string, string> translations = LoadTranslationFiles(langFiles);

			string nl = System.Environment.NewLine;
			List<string> lines = new();
			if (lang == "en")
			{
				lines.Add($"const {lang}Strings = {{");
			}
			else
			{
				lines.Add("import { RawStrings } from \"./types\";");
				lines.Add("");
				lines.Add("");
				lines.Add($"const {lang}Strings: RawStrings = {{");
			}

			foreach (string key in keys)
			{
				string referenceTranslation = defaultTranslations[key];
				string tr = translations.ContainsKey(key) && !string.IsNullOrEmpty(translations[key]) ? translations[key] : referenceTranslation;
				if (AreTokenValid(key, lang, referenceTranslation, tr) == false)
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
			lines.Add($"export default {lang}Strings;");
			lines.Add("");

			string result = string.Join(nl, lines);
			File.WriteAllText($"../{lang}.ts", result);
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

	private Dictionary<string, string> LoadTranslationFiles(string[] files)
	{
		Dictionary<string, string> result = new();
		foreach (string file in files)
		{
			Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(file));
			foreach (KeyValuePair<string, string> kvp in data)
			{
				result.Add(kvp.Key, kvp.Value);
			}
		}

		return result;
	}
}