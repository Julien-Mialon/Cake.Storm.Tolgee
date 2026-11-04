using System.Text;
using Cake.Core;
using Cake.Storm.Tolgee.Configurations;
using Newtonsoft.Json;

namespace Cake.Storm.Tolgee.Tasks;

internal class TolgeeClient
{
	private readonly TolgeeConfiguration _configuration;

	public TolgeeClient(TolgeeConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task<List<(string lang, Dictionary<string, string?> translations)>> DownloadTranslationFiles(params string[] languages)
	{
		Dictionary<string, Dictionary<string, string?>> remoteData = await FetchTranslations(languages);

		List<(string lang, Dictionary<string, string?> data)> langsData = languages.Select(lang =>
				(lang, remoteData.TryGetValue(lang, out Dictionary<string, string?>? dict) ? dict : new()))
			.ToList();

		return langsData;
	}

	public async Task UploadFile(Dictionary<string, string> translations, string lang)
	{
		Dictionary<string, string?> remoteTranslations = await DownloadOneLang(lang);
		List<string> keys = translations.Keys.OrderBy(x => x).ToList();

		UploadRequest requestData = new()
		{
			Keys = keys.Select(key => new UploadRequest.UploadKeyRequest()
			{
				Name = key,
				Namespace = _configuration.Namespace,
				Translations = new()
				{
					[lang] = new()
					{
						Text = translations[key],
						// Tolgee rejects OVERRIDE unless the existing translation is non-empty
						Resolution = string.IsNullOrEmpty(remoteTranslations.GetValueOrDefault(key)) ? "NEW" : "OVERRIDE",
					},
				}
			}).ToList()
		};

		HttpClient client = new();
		HttpRequestMessage request = new(HttpMethod.Post, $"{ProjectUrl}/keys/import-resolvable");
		request.Headers.TryAddWithoutValidation("X-API-Key", _configuration.ApiKey);
		string jsonContent = JsonConvert.SerializeObject(requestData);
		request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

		HttpResponseMessage response = await client.SendAsync(request);
		if (!response.IsSuccessStatusCode)
		{
			throw new CakeException($"Upload of {lang} failed with status {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
		}
	}

	private async Task<Dictionary<string, string?>> DownloadOneLang(string lang)
	{
		Dictionary<string, Dictionary<string, string?>> remoteData = await FetchTranslations(lang);

		return remoteData.TryGetValue(lang, out Dictionary<string, string?>? data) ? data : new();
	}

	private async Task<Dictionary<string, Dictionary<string, string?>>> FetchTranslations(params string[] languages)
	{
		HttpClient client = new();
		HttpRequestMessage request = new(HttpMethod.Get, TranslationsUrl(languages));
		request.Headers.TryAddWithoutValidation("X-API-Key", _configuration.ApiKey);

		HttpResponseMessage response = await client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			throw new CakeException($"Download of {string.Join(",", languages)} failed with status {(int)response.StatusCode}: {content}");
		}

		return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string?>>>(content)
			?? throw new CakeException($"Download of {string.Join(",", languages)} returned an empty response");
	}

	private string ProjectUrl => _configuration.ProjectId is { } projectId
		? $"{_configuration.Host}/v2/projects/{projectId}"
		: $"{_configuration.Host}/v2/projects";

	private string TranslationsUrl(params string[] languages)
	{
		// An empty structureDelimiter stops Tolgee from nesting keys on dots
		return $"{ProjectUrl}/translations/{string.Join(",", languages)}?ns={Uri.EscapeDataString(_configuration.Namespace ?? "")}&structureDelimiter=";
	}

	private class UploadRequest
	{
		[JsonProperty("keys")]
		public List<UploadKeyRequest> Keys { get; set; } = new();

		public class UploadKeyRequest
		{
			[JsonProperty("name")]
			public string Name { get; set; } = "";

			[JsonProperty("namespace")]
			public string? Namespace { get; set; }

			[JsonProperty("translations")]
			public Dictionary<string, UploadTranslationRequest> Translations { get; set; } = new();
		}

		public class UploadTranslationRequest
		{
			[JsonProperty("text")]
			public string Text { get; set; } = "";

			[JsonProperty("resolution")]
			public string Resolution { get; set; } = "";
		}
	}
}