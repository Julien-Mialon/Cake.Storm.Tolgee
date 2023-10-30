using Cake.Core.Diagnostics;

namespace Cake.Storm.Tolgee.Tasks;

internal class BaseTask
{
	protected ICakeLog Log { get; }

	public BaseTask(ICakeLog log)
	{
		Log = log;
	}
}