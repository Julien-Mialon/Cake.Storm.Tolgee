using Cake.Core;
using Cake.Core.Annotations;

[assembly: CakeNamespaceImport("Cake.Storm.Tolgee")]

namespace Cake.Storm.Tolgee;

public delegate CakeTaskBuilder TaskDelegate(string name);

[CakeAliasCategory("Cake.Storm.Tolgee")]
public static class Aliases
{
	[CakeMethodAlias]
	public static ConfigurationBuilder TolgeeConfigurationBuilder(this ICakeContext context, TaskDelegate task)
	{
		FluentContext fluentContext = new(context, task);
		return new(fluentContext);
	}
}