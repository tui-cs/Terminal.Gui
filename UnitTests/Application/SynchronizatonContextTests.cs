using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using Xunit.Sdk;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ApplicationTests {
	public class SyncrhonizationContextTests {

		[Fact, AutoInitShutdown]
		public void SynchronizationContext_Post ()
		{
			var context = SynchronizationContext.Current;

			var success = false;
			Task.Run (() => {
				Thread.Sleep (1_000);

				// non blocking
				context.Post (
					delegate (object o) {
						success = true;

						// then tell the application to quit
						Application.MainLoop.Invoke (() => Application.RequestStop ());
					}, null);
				Assert.False (success);
			});

			// blocks here until the RequestStop is processed at the end of the test
			Application.Run ();
			Assert.True (success);
		}

		[Fact, AutoInitShutdown]
		public void SynchronizationContext_Send ()
		{
			var context = SynchronizationContext.Current;

			var success = false;
			Task.Run (() => {
				Thread.Sleep (1_000);

				// blocking
				context.Send (
					delegate (object o) {
						success = true;

						// then tell the application to quit
						Application.MainLoop.Invoke (() => Application.RequestStop ());
					}, null);
				Assert.True (success);
			});

			// blocks here until the RequestStop is processed at the end of the test
			Application.Run ();
			Assert.True (success);

		}

		[Fact, AutoInitShutdown]
		public void SynchronizationContext_CreateCopy ()
		{
			var context = SynchronizationContext.Current;
			Assert.NotNull (context);

			var contextCopy = context.CreateCopy ();
			Assert.NotNull (contextCopy);

			Assert.NotEqual (context, contextCopy);
		}

	}
}
