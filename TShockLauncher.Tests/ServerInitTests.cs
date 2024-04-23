using NUnit.Framework;
using OTAPI;
using System;
using System.Diagnostics;
using System.Threading;

namespace TShockLauncher.Tests;

public class ServerInitTests
{
	/// <summary>
	/// This test will ensure that the TSAPI binary boots up as expected
	/// </summary>
	[TestCase]
	public void EnsureBoots()
	{
		var are = new AutoResetEvent(false);
		PreHookHandler<Hooks.Main.DedicatedServerEventArgs> cb = (Hooks.Main.DedicatedServerEventArgs args) =>
		{
			are.Set();
			Debug.WriteLine("Server init process successful");
			return HookResult.Cancel;
		};
		Hooks.Main.PreDedicatedServer += cb;

		new Thread(() => TerrariaApi.Server.Program.Main(new string[] { })).Start();

		var hit = are.WaitOne(TimeSpan.FromSeconds(10));

		Hooks.Main.PreDedicatedServer -= cb;

		Assert.IsTrue(hit);
	}
}

