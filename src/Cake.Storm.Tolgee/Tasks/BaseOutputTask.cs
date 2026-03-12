using System.Text;
using Cake.Core.Diagnostics;
using Newtonsoft.Json;

namespace Cake.Storm.Tolgee.Tasks;

internal abstract class BaseOutputTask : BaseTask
{
	private readonly bool _minify;

	protected BaseOutputTask(ICakeLog log, bool minify) : base(log)
	{
		_minify = minify;
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

	protected bool AreTokenValid(string key, string language, string reference, string translation)
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

	protected async Task<Dictionary<string, string>> LoadTranslationFiles(string[] files)
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
				if (result.ContainsKey(kvp.Key))
				{
					if (string.IsNullOrEmpty(kvp.Value))
					{
						continue;
					}
				}

				result[kvp.Key] = kvp.Value;
			}
		}

		return result;
	}

	protected async Task MakeJsonFile(Dictionary<string, string> translations, string outputFile)
	{
		if (_minify)
		{
			await MakeMinifiedJsonFile(translations, outputFile);
			return;
		}

		string nl = Environment.NewLine;
		List<string> lines = ["{"];

		foreach ((string key, string value) in translations)
		{
			string tr = value
				.Replace("\"", "\\\"")
				.Replace("\t", "\\t")
				.Replace("\n", "\\n");
			lines.Add($"\t\"{key}\": \"{tr}\",");
		}

		lines[^1] = lines[^1].TrimEnd(',');
		lines.Add("}");

		string result = string.Join(nl, lines);
		await File.WriteAllTextAsync(outputFile, result);
	}

	private async Task MakeMinifiedJsonFile(Dictionary<string, string> translations, string outputFile)
	{
		StringBuilder sb = new();

		sb.Append('{');
		foreach ((string key, string value) in translations)
		{
			string tr = value
				.Replace("\"", "\\\"")
				.Replace("\t", "\\t")
				.Replace("\n", "\\n");
			sb.Append($"\"{key}\":\"{tr}\",");
		}

		if (sb.Length > 1)
		{
			sb.Remove(sb.Length - 1, 1);
		}

		sb.Append('}');
		string result = sb.ToString();
		await File.WriteAllTextAsync(outputFile, result);
	}
}