namespace Cake.Storm.Tolgee;

public interface IMergeConfigurationBuilder
{
	IMergeConfigurationBuilder AddOperation(Action<IMergeOperationBuilder> builder);
}

public interface IMergeOperationBuilder
{
	IMergeOperationBuilder AddFile(string language, string outputFile, params string[] sourceFiles);
}