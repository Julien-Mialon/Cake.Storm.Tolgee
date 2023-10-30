using Cake.Core.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cake.Storm.Tolgee.Tasks;

internal class DownloadTask : TolgeeTask
{
	public DownloadTask(string host, string apiKey, List<string> languages, ICakeLog log) : base(host, apiKey, languages, log)
	{
	}

	public override async Task Run()
	{
		await DownloadTranslationFiles();

		await base.Run();
	}

	private async Task DownloadTranslationFiles()
	{
		Log.Information($"Download translation files");
		HttpClient client = new();
		HttpRequestMessage request = new(HttpMethod.Get, $"{Host}/v2/projects/translations/{string.Join(",", Languages)}?ns=string");
		request.Headers.TryAddWithoutValidation("X-API-Key", ApiKey);

		HttpResponseMessage response = await client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Dictionary<string, Dictionary<string, string>>? remoteData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(content);

		List<(string lang, Dictionary<string, string> data)> langsData = Languages.ConvertAll(lang => (lang, remoteData.TryGetValue(lang, out Dictionary<string, string> dict) ? dict : new()));
		HashSet<string> keys = new(langsData.SelectMany(x => x.data.Keys));

		foreach (string key in keys)
		{
			foreach ((string _, Dictionary<string, string> data) in langsData)
			{
				if (!data.ContainsKey(key))
				{
					data.Add(key, string.Empty);
				}
			}
		}

		foreach ((string lang, Dictionary<string, string> data) in langsData)
		{
			ShowCompletion(data, lang);
			await File.WriteAllTextAsync($"sources/{lang}.json", SerializeDictionary(data));
		}
	}

	private void ShowCompletion(Dictionary<string, string> strings, string language)
	{
		int translatedCount = strings.Values.Count(x => !string.IsNullOrEmpty(x));
		int total = strings.Count;

		int percentage = (int)(100f * translatedCount / (float)total);
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

		List<string> keys = source.Keys.ToList();
		List<string> orderedKeys = source.Keys.OrderBy(x => x).ToList();
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

			return (a.Length < b.Length) ? -1 : 1;
		});

		foreach (string key in sortedKeys)
		{
			obj.Add(key, source[key]);
		}

		string result = JsonConvert.SerializeObject(obj, Formatting.Indented);
		return result.Replace("\n  \"", "\n    \"");
	}
}