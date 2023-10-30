using Cake.Core;

namespace Cake.Storm.Tolgee;

public class FluentContext
{
	public ICakeContext CakeContext { get; }

	public TaskDelegate Task { get; }

	public FluentContext(ICakeContext cakeContext, TaskDelegate task)
	{
		CakeContext = cakeContext;
		Task = task;
	}
}