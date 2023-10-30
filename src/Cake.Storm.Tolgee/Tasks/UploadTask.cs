using System.Text;
using Cake.Core.Diagnostics;
using Newtonsoft.Json;

namespace Cake.Storm.Tolgee.Tasks;

internal class UploadTask : TolgeeTask
{
	public UploadTask(string host, string apiKey, List<string> languages, ICakeLog log) : base(host, apiKey, languages, log)
	{
	}

	public override async Task Run()
	{
		foreach (string language in Languages)
		{
			await UploadTranslationFile(language);
		}

		await base.Run();
	}

	private async Task UploadFile(Dictionary<string, string> translations, string lang)
	{
		Dictionary<string, string> remoteTranslations = await DownloadOneLang(lang);
		List<string> keys = translations.Keys.OrderBy(x => x).ToList();

		UploadRequest requestData = new()
		{
			Keys = keys.Select(key => new UploadRequest.UploadKeyRequest()
			{
				Name = key,
				Translations = new()
				{
					[lang] = new()
					{
						Text = translations[key],
						Resolution = remoteTranslations.ContainsKey(key) ? "OVERRIDE" : "NEW",
					},
				}
			}).ToList()
		};

		HttpClient client = new();
		HttpRequestMessage request = new(HttpMethod.Post, $"{Host}/v2/projects/keys/import-resolvable");
		request.Headers.TryAddWithoutValidation("X-API-Key", ApiKey);
		string jsonContent = JsonConvert.SerializeObject(requestData);
		request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

		HttpResponseMessage response = await client.SendAsync(request);
		if (response.IsSuccessStatusCode)
		{
			Log.Information("Uploaded with success");
		}
		else
		{
			Log.Information("Upload error");
			Log.Information(await response.Content.ReadAsStringAsync());
		}
	}

	private async Task<Dictionary<string, string>> DownloadOneLang(string lang)
	{
		Log.Information($"Download translation files");
		HttpClient client = new();
		HttpRequestMessage request = new(HttpMethod.Get, $"{Host}/v2/projects/translations/{lang}?ns=string");
		request.Headers.TryAddWithoutValidation("X-API-Key", ApiKey);

		HttpResponseMessage response = await client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Dictionary<string, Dictionary<string, string>>? remoteData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(content);
		return remoteData.TryGetValue(lang, out Dictionary<string, string> data) ? data : new();
	}

	private async Task UploadTranslationFile(string lang)
	{
		Dictionary<string, string> defaultTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(await System.IO.File.ReadAllTextAsync($"sources/{lang}.json"));
		Log.Information($"Upload {lang} translation file");
		await UploadFile(defaultTranslations, lang);
	}

	private class UploadRequest
	{
		[JsonProperty("keys")]
		public List<UploadKeyRequest> Keys { get; set; }

		public class UploadKeyRequest
		{
			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("namespace")]
			public string Namespace { get; set; } = "string";

			[JsonProperty("translations")]
			public Dictionary<string, UploadTranslationRequest> Translations { get; set; }
		}

		public class UploadTranslationRequest
		{
			[JsonProperty("text")]
			public string Text { get; set; }

			[JsonProperty("resolution")]
			public string Resolution { get; set; }
		}
	}
}