using Cake.Common;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Configurations;
using Cake.Storm.Tolgee.Tasks;

namespace Cake.Storm.Tolgee;

internal class TaskMaker
{
	private readonly FluentContext _context;
	private readonly Configuration _configuration;
	private readonly TolgeeClient _client;

	public TaskMaker(FluentContext context, Configuration configuration)
	{
		_context = context;
		_configuration = configuration;
		_client = new(_configuration.Tolgee);
	}

	public void Make()
	{
		List<string> uploadTasks = new();
		List<string> downloadTasks = new();

		if (_context.CakeContext.HasArgument("minify"))
		{
			_configuration.Minify = true;
		}

		uploadTasks.Add("upload");
		downloadTasks.Add("download");
		_context.Task("upload").IsDependentOn($"upload-{_configuration.DefaultLanguage}");
		_context.Task("download").IsDependentOn($"download-{_configuration.DefaultLanguage}");

		foreach (InputLanguageConfiguration languageConfiguration in _configuration.Input.Languages)
		{
			uploadTasks.Add($"upload-{languageConfiguration.LanguageCode}");
			downloadTasks.Add($"download-{languageConfiguration.LanguageCode}");
			AddUploadTask(languageConfiguration);
			AddDownloadTask(languageConfiguration);
		}

		_context.Task("upload-all")
			.Does(async () =>
			{
				await ExecuteUpload(_configuration.Input.Languages.ToArray());
				await ExecuteOutput();
				await ExecuteMerge();
			});

		_context.Task($"download-all")
			.Does(async () =>
			{
				await ExecuteDownload(_configuration.Input.Languages.ToArray());
				await ExecuteOutput();
				await ExecuteMerge();
			});

		uploadTasks.Add("upload-all");
		downloadTasks.Add("download-all");

		_context.Task("help")
			.Does(() =>
			{
				_context.CakeContext.Log.Information("");
				_context.CakeContext.Log.Information("List of targets");
				if (uploadTasks.Count > 0)
				{
					_context.CakeContext.Log.Information("-- upload --");
					foreach (string task in uploadTasks)
					{
						_context.CakeContext.Log.Information($"\t{task}");
					}

					_context.CakeContext.Log.Information("");
				}

				if (downloadTasks.Count > 0)
				{
					_context.CakeContext.Log.Information("-- download --");
					foreach (string task in downloadTasks)
					{
						_context.CakeContext.Log.Information($"\t{task}");
					}

					_context.CakeContext.Log.Information("");
				}

				_context.CakeContext.Log.Information("help");
			});
		_context.Task("default")
			.Does(async () =>
			{
				await ExecuteOutput();
				await ExecuteMerge();
			});
	}

	private async Task ExecuteUpload(params InputLanguageConfiguration[] languages)
	{
		await new UploadTask(_context.CakeContext.Log, _client, languages).Run();
	}

	private async Task ExecuteDownload(params InputLanguageConfiguration[] languages)
	{
		await new DownloadTask(_context.CakeContext.Log, _client, languages).Run();
	}

	private async Task ExecuteOutput()
	{
		if (_configuration.Output.Languages.Count == 0)
		{
			return;
		}

		if (_configuration.Output.Type is OutputType.Typescript)
		{
			await new TypescriptOutputTask(_context.CakeContext.Log, _configuration.Output.Languages, _configuration.DefaultLanguage, _configuration.Minify).Run();
			return;
		}

		if (_configuration.Output.Type is OutputType.Json)
		{
			await new JsonOutputTask(_context.CakeContext.Log, _configuration.Output.Languages, _configuration.DefaultLanguage, _configuration.Minify).Run();
			return;
		}

		throw new InvalidOperationException("Output type not supported");
	}

	private async Task ExecuteMerge()
	{
		if (_configuration.Merge.Operations.Count == 0)
		{
			return;
		}

		foreach (MergeFileOperation operation in _configuration.Merge.Operations)
		{
			await new MergeFilesTask(_context.CakeContext.Log, operation, _configuration.DefaultLanguage, _configuration.Minify).Run();
		}
	}

	private void AddUploadTask(InputLanguageConfiguration configuration)
	{
		_context.Task($"upload-{configuration.LanguageCode}")
			.Does(async () =>
			{
				await ExecuteUpload(configuration);
				await ExecuteOutput();
				await ExecuteMerge();
			});
	}

	private void AddDownloadTask(InputLanguageConfiguration configuration)
	{
		_context.Task($"download-{configuration.LanguageCode}")
			.Does(async () =>
			{
				await ExecuteDownload(configuration);
				await ExecuteOutput();
				await ExecuteMerge();
			});
	}
}