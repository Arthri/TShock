using Newtonsoft.Json;
using OTAPI.Patcher.Engine.Modification;
using System.Collections.Generic;
using System.Reflection;
using Terraria;

namespace Mintaka.Modifications.Json
{
	public class PatchJson : ModificationBase
	{
		public override IEnumerable<string> AssemblyTargets => new[]
		{
			typeof(WorldGen).Assembly.FullName
		};

		public Assembly NewAssembly { get; }

		public AssemblyName NewAssemblyName { get; }

		public override string Description { get; }

		public PatchJson()
		{
			var newAssembly = typeof(JsonConvert).Assembly;
			NewAssembly = newAssembly;
			NewAssemblyName = newAssembly.GetName();
			Description = $"Patching Newtonsoft.Json to assembly version {NewAssemblyName.Version}";
		}

		public override void Run()
		{
			foreach (var reference in SourceDefinition.MainModule.AssemblyReferences)
			{
				if (reference.Name == NewAssemblyName.Name)
				{
					reference.Version = NewAssemblyName.Version;
					reference.PublicKey = NewAssemblyName.GetPublicKey();
					reference.PublicKeyToken = NewAssemblyName.GetPublicKeyToken();
				}
			}
		}
	}
}
