using Cake.Core;

namespace Cake.Storm.Tolgee;

public interface IFluentContext
{
	ICakeContext CakeContext { get; }

	TaskDelegate Task { get; }

	SetupDelegate Setup { get; }

	TeardownDelegate Teardown { get; }

	TaskSetupDelegate TaskSetup { get; }

	TaskTeardownDelegate TaskTeardown { get; }
}