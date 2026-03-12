namespace Cake.Storm.Tolgee.Configurations;

public class MergeFilesConfiguration
{
	public List<MergeFileOperation> Operations { get; set; } = new();
}

public class MergeFileOperation
{
	public List<MergeFileDetail> Files { get; set; } = new();
}

public class MergeFileDetail
{
	public string LanguageCode { get; set; } = "";

	public string[] SourceFiles { get; set; } = Array.Empty<string>();

	public string OutputFile { get; set; } = "";
}