using Cake.Core;
using Cake.Core.Annotations;

[assembly: CakeNamespaceImport("Cake.Storm.Tolgee")]

namespace Cake.Storm.Tolgee;

public delegate CakeTaskBuilder TaskDelegate(string name);

public delegate void SetupDelegate(Action<ICakeContext> action);

public delegate void TeardownDelegate(Action<ICakeContext> action);

public delegate void TaskSetupDelegate(Action<ITaskSetupContext> action);

public delegate void TaskTeardownDelegate(Action<ITaskTeardownContext> action);

[CakeAliasCategory("Cake.Storm.Tolgee")]
public static class Aliases
{
	[CakeMethodAlias]
	public static ConfigurationBuilder TolgeeConfigurationBuilder(this ICakeContext context, TaskDelegate task, SetupDelegate setup, TeardownDelegate teardown, TaskSetupDelegate taskSetup, TaskTeardownDelegate taskTeardown)
	{
		IFluentContext fluentContext = new FluentContext(context, task, setup, teardown, taskSetup, taskTeardown);
		return new ConfigurationBuilder(fluentContext);
	}

	private class FluentContext : IFluentContext
	{
		public ICakeContext CakeContext { get; }

		public TaskDelegate Task { get; }

		public SetupDelegate Setup { get; }

		public TeardownDelegate Teardown { get; }

		public TaskSetupDelegate TaskSetup { get; }

		public TaskTeardownDelegate TaskTeardown { get; }

		public FluentContext(ICakeContext cakeContext, TaskDelegate task, SetupDelegate setup, TeardownDelegate teardown, TaskSetupDelegate taskSetup, TaskTeardownDelegate taskTeardown)
		{
			CakeContext = cakeContext;
			Task = task;
			Setup = setup;
			Teardown = teardown;
			TaskSetup = taskSetup;
			TaskTeardown = taskTeardown;
		}
	}
}