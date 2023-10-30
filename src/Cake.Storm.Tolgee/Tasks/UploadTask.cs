using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Configurations;
using Newtonsoft.Json;

namespace Cake.Storm.Tolgee.Tasks;

internal class UploadTask : BaseTask
{
	private readonly TolgeeClient _client;
	private readonly InputLanguageConfiguration[] _languages;

	public UploadTask(ICakeLog log, TolgeeClient client, InputLanguageConfiguration[] languages) : base(log)
	{
		_client = client;
		_languages = languages;
	}

	public async Task Run()
	{
		foreach (InputLanguageConfiguration language in _languages)
		{
			string content = await File.ReadAllTextAsync(language.SourceFile);
			Dictionary<string, string>? defaultTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
			if (defaultTranslations is null)
			{
				Log.Warning($"Skip upload of {language.LanguageCode} file, it's empty");
				continue;
			}

			Log.Information($"Upload {language.LanguageCode} translation file");
			if (await _client.UploadFile(defaultTranslations, language.LanguageCode))
			{
				Log.Information("\tUploaded with success");
			}
		}
	}
}