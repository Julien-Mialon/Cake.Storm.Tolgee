using Cake.Storm.Tolgee.Configurations;

namespace Cake.Storm.Tolgee;

internal static class Extensions
{
	public static string FileExtension(this OutputType type)
	{
		return type switch
		{
			OutputType.Typescript => "ts",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}