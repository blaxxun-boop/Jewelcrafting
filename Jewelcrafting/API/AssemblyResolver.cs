using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
	public sealed class ModuleInitializerAttribute : Attribute
	{
	}
}

namespace Jewelcrafting
{
	public static class Initializer
	{
		[ModuleInitializer]
		public static void Init() => AppDomain.CurrentDomain.AssemblyResolve += (_, e) => e.Name.StartsWith("Jewelcrafting,") ? Assembly.Load(StreamToByteArray(Assembly.GetExecutingAssembly().GetManifestResourceStream("Jewelcrafting.Jewelcrafting.dll")!)) : null;

		private static byte[] StreamToByteArray(Stream input)
		{
			using MemoryStream stream = new();
			input.CopyTo(stream);
			return stream.ToArray();
		}
	}
}