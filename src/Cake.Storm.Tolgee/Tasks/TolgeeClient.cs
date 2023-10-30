using System.Text;
using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Configurations;
using Newtonsoft.Json;

namespace Cake.Storm.Tolgee.Tasks;

internal class TolgeeClient
{
	private readonly TolgeeConfiguration _configuration;
	private readonly ICakeLog _log;

	public TolgeeClient(TolgeeConfiguration configuration, ICakeLog log)
	{
		_configuration = configuration;
		_log = log;
	}

	public async Task<List<(string lang, Dictionary<string, string> translations)>> DownloadTranslationFiles(params string[] languages)
	{
		HttpClient client = new();
		HttpRequestMessage request = new(HttpMethod.Get, $"{_configuration.Host}/v2/projects/translations/{string.Join(",", languages)}?ns=string");
		request.Headers.TryAddWithoutValidation("X-API-Key", _configuration.ApiKey);

		HttpResponseMessage response = await client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Dictionary<string, Dictionary<string, string>>? remoteData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(content);

		if (remoteData is null)
		{
			return languages.Select(x => (x, new Dictionary<string, string>())).ToList();
		}

		List<(string lang, Dictionary<string, string> data)> langsData = languages.Select(lang =>
				(lang, remoteData.TryGetValue(lang, out Dictionary<string, string>? dict) ? dict : new()))
			.ToList();

		return langsData;
	}

	public async Task<bool> UploadFile(Dictionary<string, string> translations, string lang)
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
		HttpRequestMessage request = new(HttpMethod.Post, $"{_configuration.Host}/v2/projects/keys/import-resolvable");
		request.Headers.TryAddWithoutValidation("X-API-Key", _configuration.ApiKey);
		string jsonContent = JsonConvert.SerializeObject(requestData);
		request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

		HttpResponseMessage response = await client.SendAsync(request);
		if (response.IsSuccessStatusCode)
		{
			return true;
		}

		_log.Warning("Upload error");
		_log.Warning(await response.Content.ReadAsStringAsync());
		return false;
	}

	private async Task<Dictionary<string, string>> DownloadOneLang(string lang)
	{
		HttpClient client = new();
		HttpRequestMessage request = new(HttpMethod.Get, $"{_configuration.Host}/v2/projects/translations/{lang}?ns=string");
		request.Headers.TryAddWithoutValidation("X-API-Key", _configuration.ApiKey);

		HttpResponseMessage response = await client.SendAsync(request);
		string content = await response.Content.ReadAsStringAsync();

		Dictionary<string, Dictionary<string, string>>? remoteData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(content);
		if (remoteData is null)
		{
			return new();
		}

		return remoteData.TryGetValue(lang, out Dictionary<string, string>? data) ? data : new();
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
			public string Namespace { get; set; } = "string";

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