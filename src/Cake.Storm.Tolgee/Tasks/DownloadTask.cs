using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Configurations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cake.Storm.Tolgee.Tasks;

internal class DownloadTask : BaseTask
{
	private readonly TolgeeClient _client;
	private readonly string[] _languages;
	private readonly Dictionary<string, InputLanguageConfiguration> _languageConfigurations;

	public DownloadTask(ICakeLog log, TolgeeClient client, InputLanguageConfiguration[] languages) : base(log)
	{
		_client = client;
		_languages = languages.Select(x => x.LanguageCode).ToArray();
		_languageConfigurations = languages.ToDictionary(x => x.LanguageCode, x => x);
	}

	public async Task Run()
	{
		await DownloadTranslationFiles();
	}

	private async Task DownloadTranslationFiles()
	{
		Log.Information("Download translation files");
		List<(string lang, Dictionary<string, string> data)> langsData = await _client.DownloadTranslationFiles(_languages);
		HashSet<string> keys = new(langsData.SelectMany(x => x.data.Keys));

		foreach (string key in keys)
		{
			foreach ((string _, Dictionary<string, string> data) in langsData)
			{
				data.TryAdd(key, string.Empty);
			}
		}

		foreach ((string lang, Dictionary<string, string> data) in langsData)
		{
			ShowCompletion(data, lang);
			await File.WriteAllTextAsync(_languageConfigurations[lang].SourceFile, SerializeDictionary(data));
		}
	}

	private void ShowCompletion(Dictionary<string, string> strings, string language)
	{
		int translatedCount = strings.Values.Count(x => !string.IsNullOrEmpty(x));
		int total = strings.Count;

		int percentage = (int)(100f * translatedCount / total);
		string remaining = "";
		if (translatedCount < total)
		{
			remaining = $" {total - translatedCount} remains";
		}

		Log.Information($"{language}: {translatedCount}/{strings.Count} ({percentage}%){remaining}");
	}

	private string SerializeDictionary(Dictionary<string, string> source)
	{
		JObject obj = new();

		List<string> sortedKeys = source.Keys.ToList();
		sortedKeys.Sort((a, b) =>
		{
			if (a == b)
			{
				return 0;
			}

			if (string.IsNullOrEmpty(a))
			{
				return 1;
			}

			if (string.IsNullOrEmpty(b))
			{
				return -1;
			}

			for (int i = 0 ; i < a.Length && i < b.Length ; ++i)
			{
				int ca = a[i];
				int cb = b[i];

				int r = ca.CompareTo(cb);
				if (r == 0)
				{
					continue;
				}

				return r;
			}

			return a.Length < b.Length ? -1 : 1;
		});

		foreach (string key in sortedKeys)
		{
			obj.Add(key, source[key]);
		}

		string result = JsonConvert.SerializeObject(obj, Formatting.Indented);
		return result.Replace("\n  \"", "\n    \"");
	}
}