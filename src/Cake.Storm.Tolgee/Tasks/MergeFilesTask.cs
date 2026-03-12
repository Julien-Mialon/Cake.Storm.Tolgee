using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Configurations;

namespace Cake.Storm.Tolgee.Tasks;

internal class MergeFilesTask : BaseOutputTask
{
	private readonly MergeFileOperation _operation;
	private readonly string _defaultLanguage;

	public MergeFilesTask(ICakeLog log, MergeFileOperation operation, string defaultLanguage, bool minify) : base(log, minify)
	{
		_operation = operation;
		_defaultLanguage = defaultLanguage;
	}

	public async Task Run()
	{
		Log.Information("Merging files");
		Dictionary<string, Dictionary<string, string>> allTranslations = new();
		foreach (MergeFileDetail file in _operation.Files)
		{
			allTranslations.Add(file.LanguageCode, await LoadTranslationFiles(file.SourceFiles));
		}

		Dictionary<string, string> defaultTranslations = allTranslations[_defaultLanguage];
		List<string> keys = allTranslations.SelectMany(x => x.Value.Keys).Distinct().OrderBy(x => x).ToList();

		foreach (MergeFileDetail file in _operation.Files)
		{
			Dictionary<string, string> translations = allTranslations[file.LanguageCode];

			Dictionary<string, string> result = new();
			foreach (string key in keys)
			{
				string referenceTranslation = defaultTranslations[key];
				string tr = "";
				if (translations.TryGetValue(key, out string? translation) && !string.IsNullOrWhiteSpace(translation))
				{
					tr = translation;
				}
				else
				{
					tr = referenceTranslation;
				}

				if (AreTokenValid(key, file.LanguageCode, referenceTranslation, tr) is false)
				{
					tr = referenceTranslation;
				}

				result.Add(key, tr);
			}

			await MakeJsonFile(result, file.OutputFile);
		}
	}
}