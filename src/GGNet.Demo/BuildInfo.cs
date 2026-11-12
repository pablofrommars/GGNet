using System.Reflection;

namespace GGNet.Demo;

public static class BuildInfo
{
	public static string Version { get; } = typeof(BuildInfo).Assembly
		.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
}
