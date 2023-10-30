using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Storm.Tolgee.Tasks;

namespace Cake.Storm.Tolgee;

public class ConfigurationBuilder
{
	private readonly IFluentContext _context;
	private readonly List<string> _languages = new();
	private string? _defaultLanguage;
	private string? _host;
	private string? _apiKey;

	public ConfigurationBuilder(IFluentContext context)
	{
		_context = context;
	}

	public ConfigurationBuilder UseHost(string host)
	{
		_host = host;
		return this;
	}

	public ConfigurationBuilder UseApiKey(string apiKey)
	{
		_apiKey = apiKey;
		return this;
	}

	public ConfigurationBuilder WithDefaultLanguage(string language)
	{
		_defaultLanguage = language;
		return WithLanguage(language);
	}

	public ConfigurationBuilder WithLanguage(string language)
	{
		_languages.Add(language);
		return this;
	}

	public void Build()
	{
		if (_host is null || _apiKey is null)
		{
			throw new CakeException("Host and API key must be defined");
		}

		List<string> uploadTasks = new();
		List<string> downloadTasks = new();

		if (_defaultLanguage is not null)
		{
			uploadTasks.Add("upload");
			downloadTasks.Add("download");
			_context.Task("upload").IsDependentOn($"upload-{_defaultLanguage}");
			_context.Task("download").IsDependentOn($"download-{_defaultLanguage}");
		}

		foreach (string language in _languages)
		{
			uploadTasks.Add($"upload-{language}");
			downloadTasks.Add($"download-{language}");
			Upload(language);
			Download(language);
		}

		if (_languages.Count > 0)
		{
			_context.Task($"upload-all").Does(async () =>
			{
				await new UploadTask(_host!, _apiKey!, _languages, _context.CakeContext.Log).Run();
			});

			_context.Task($"download-all").Does(async () =>
			{
				await new DownloadTask(_host!, _apiKey!, _languages, _context.CakeContext.Log).Run();
			});

			uploadTasks.Add("upload-all");
			downloadTasks.Add("download-all");
		}

		_context.Task("help").Does(() =>
		{
			_context.CakeContext.Log.Information("");
			_context.CakeContext.Log.Information("List of targets");
			_context.CakeContext.Log.Information("");
			if (uploadTasks.Count > 0)
			{
				_context.CakeContext.Log.Information("-- upload --");
				_context.CakeContext.Log.Information("");
				foreach (string task in uploadTasks)
				{
					_context.CakeContext.Log.Information($"\t{task}");
				}
				_context.CakeContext.Log.Information("");
			}

			if (downloadTasks.Count > 0)
			{
				_context.CakeContext.Log.Information("-- download --");
				_context.CakeContext.Log.Information("");
				foreach (string task in downloadTasks)
				{
					_context.CakeContext.Log.Information($"\t{task}");
				}
				_context.CakeContext.Log.Information("");
			}
			_context.CakeContext.Log.Information("help");
		});
		_context.Task("default").IsDependentOn("help").Does(() => { });
	}

	private void Upload(string language)
	{
		_context.Task($"upload-{language}").Does(async () =>
		{
			await new UploadTask(_host!, _apiKey!, new() { language }, _context.CakeContext.Log).Run();
		});
	}
	private void Download(string language)
	{
		_context.Task($"download-{language}").Does(async () =>
		{
			await new DownloadTask(_host!, _apiKey!, new() { language }, _context.CakeContext.Log).Run();
		});
	}
}