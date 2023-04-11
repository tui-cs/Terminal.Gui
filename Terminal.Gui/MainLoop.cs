//
// MainLoop.cs: IMainLoopDriver and MainLoop for Terminal.Gui
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Terminal.Gui {
	/// <summary>
	/// Public interface to create your own platform specific main loop driver.
	/// </summary>
	public interface IMainLoopDriver {
		/// <summary>
		/// Initializes the main loop driver, gets the calling main loop for the initialization.
		/// </summary>
		/// <param name="mainLoop">Main loop.</param>
		void Setup (MainLoop mainLoop);

		/// <summary>
		/// Wakes up the mainloop that might be waiting on input, must be thread safe.
		/// </summary>
		void Wakeup ();

		/// <summary>
		/// Must report whether there are any events pending, or even block waiting for events.
		/// </summary>
		/// <returns><c>true</c>, if there were pending events, <c>false</c> otherwise.</returns>
		/// <param name="wait">If set to <c>true</c> wait until an event is available, otherwise return immediately.</param>
		bool EventsPending (bool wait);

		/// <summary>
		/// The iteration function.
		/// </summary>
		void MainIteration ();
	}

	/// <summary>
	///   Simple main loop implementation that can be used to monitor
	///   file descriptor, run timers and idle handlers.
	/// </summary>
	/// <remarks>
	///   Monitoring of file descriptors is only available on Unix, there
	///   does not seem to be a way of supporting this on Windows.
	/// </remarks>
	public class MainLoop {
		/// <summary>
		/// Provides data for timers running manipulation.
		/// </summary>
		public sealed class Timeout {
			/// <summary>
			/// Time to wait before invoke the callback.
			/// </summary>
			public TimeSpan Span;
			/// <summary>
			/// The function that will be invoked.
			/// </summary>
			public Func<MainLoop, bool> Callback;
		}

		internal SortedList<long, Timeout> timeouts = new SortedList<long, Timeout> ();
		object timeoutsLockToken = new object ();

		/// <summary>
		/// The idle handlers and lock that must be held while manipulating them
		/// </summary>
		object idleHandlersLock = new object ();
		internal List<Func<bool>> idleHandlers = new List<Func<bool>> ();

		/// <summary>
		/// Gets the list of all timeouts sorted by the <see cref="TimeSpan"/> time ticks./>.
		/// A shorter limit time can be added at the end, but it will be called before an
		///  earlier addition that has a longer limit time.
		/// </summary>
		public SortedList<long, Timeout> Timeouts => timeouts;

		/// <summary>
		/// Gets a copy of the list of all idle handlers.
		/// </summary>
		public ReadOnlyCollection<Func<bool>> IdleHandlers {
			get {
				lock (idleHandlersLock) {
					return new List<Func<bool>> (idleHandlers).AsReadOnly ();
				}
			}
		}

		/// <summary>
		/// The current IMainLoopDriver in use.
		/// </summary>
		/// <value>The driver.</value>
		public IMainLoopDriver Driver { get; }

		/// <summary>
		/// Invoked when a new timeout is added. To be used in the case
		/// when <see cref="Application.ExitRunLoopAfterFirstIteration"/> is <see langword="true"/>.
		/// </summary>
		public event EventHandler<TimeoutEventArgs> TimeoutAdded;

		/// <summary>
		///  Creates a new Mainloop. 
		/// </summary>
		/// <param name="driver">Should match the <see cref="ConsoleDriver"/> 
		/// (one of the implementations FakeMainLoop, UnixMainLoop, NetMainLoop or WindowsMainLoop).</param>
		public MainLoop (IMainLoopDriver driver)
		{
			Driver = driver;
			driver.Setup (this);
		}

		/// <summary>
		///   Runs <c>action</c> on the thread that is processing events
		/// </summary>
		/// <param name="action">the action to be invoked on the main processing thread.</param>
		public void Invoke (Action action)
		{
			AddIdle (() => {
				action ();
				return false;
			});
		}

		/// <summary>
		///   Adds specified idle handler function to mainloop processing. The handler function will be called once per iteration of the main loop after other events have been handled.
		/// </summary>
		/// <remarks>
		/// <para>
		///   Remove an idle hander by calling <see cref="RemoveIdle(Func{bool})"/> with the token this method returns.
		/// </para>
		/// <para>
		///   If the <c>idleHandler</c> returns <c>false</c> it will be removed and not called subsequently.
		/// </para>
		/// </remarks>
		/// <param name="idleHandler">Token that can be used to remove the idle handler with <see cref="RemoveIdle(Func{bool})"/> .</param>
		public Func<bool> AddIdle (Func<bool> idleHandler)
		{
			lock (idleHandlersLock) {
				idleHandlers.Add (idleHandler);
			}

			Driver.Wakeup ();
			return idleHandler;
		}

		/// <summary>
		///   Removes an idle handler added with <see cref="AddIdle(Func{bool})"/> from processing.
		/// </summary>
		/// <param name="token">A token returned by <see cref="AddIdle(Func{bool})"/></param>
		/// Returns <c>true</c>if the idle handler is successfully removed; otherwise, <c>false</c>.
		///  This method also returns <c>false</c> if the idle handler is not found.
		public bool RemoveIdle (Func<bool> token)
		{
			lock (idleHandlersLock)
				return idleHandlers.Remove (token);
		}

		void AddTimeout (TimeSpan time, Timeout timeout)
		{
			lock (timeoutsLockToken) {
				var k = (DateTime.UtcNow + time).Ticks;
				timeouts.Add (NudgeToUniqueKey (k), timeout);
				TimeoutAdded?.Invoke (this, new TimeoutEventArgs(timeout, k));
			}
		}

		/// <summary>
		///   Adds a timeout to the mainloop.
		/// </summary>
		/// <remarks>
		///   When time specified passes, the callback will be invoked.
		///   If the callback returns true, the timeout will be reset, repeating
		///   the invocation. If it returns false, the timeout will stop and be removed.
		///
		///   The returned value is a token that can be used to stop the timeout
		///   by calling <see cref="RemoveTimeout(object)"/>.
		/// </remarks>
		public object AddTimeout (TimeSpan time, Func<MainLoop, bool> callback)
		{
			if (callback == null)
				throw new ArgumentNullException (nameof (callback));
			var timeout = new Timeout () {
				Span = time,
				Callback = callback
			};
			AddTimeout (time, timeout);
			return timeout;
		}

		/// <summary>
		///   Removes a previously scheduled timeout
		/// </summary>
		/// <remarks>
		///   The token parameter is the value returned by AddTimeout.
		/// </remarks>
		/// Returns <c>true</c>if the timeout is successfully removed; otherwise, <c>false</c>.
		/// This method also returns <c>false</c> if the timeout is not found.
		public bool RemoveTimeout (object token)
		{
			lock (timeoutsLockToken) {
				var idx = timeouts.IndexOfValue (token as Timeout);
				if (idx == -1)
					return false;
				timeouts.RemoveAt (idx);
			}
			return true;
		}

		void RunTimers ()
		{
			long now = DateTime.UtcNow.Ticks;
			SortedList<long, Timeout> copy;

			// lock prevents new timeouts being added
			// after we have taken the copy but before
			// we have allocated a new list (which would
			// result in lost timeouts or errors during enumeration)
			lock (timeoutsLockToken) {
				copy = timeouts;
				timeouts = new SortedList<long, Timeout> ();
			}

			foreach (var t in copy) {
				var k = t.Key;
				var timeout = t.Value;
				if (k < now) {
					if (timeout.Callback (this))
						AddTimeout (timeout.Span, timeout);
				} else {
					lock (timeoutsLockToken) {
						timeouts.Add (NudgeToUniqueKey (k), timeout);
					}
				}
			}
		}

		/// <summary>
		/// Finds the closest number to <paramref name="k"/> that is not
		/// present in <see cref="timeouts"/> (incrementally).
		/// </summary>
		/// <param name="k"></param>
		/// <returns></returns>
		private long NudgeToUniqueKey (long k)
		{
			lock (timeoutsLockToken) {
				while (timeouts.ContainsKey (k)) {
					k++;
				}
			}

			return k;
		}

		void RunIdle ()
		{
			List<Func<bool>> iterate;
			lock (idleHandlersLock) {
				iterate = idleHandlers;
				idleHandlers = new List<Func<bool>> ();
			}

			foreach (var idle in iterate) {
				if (idle ())
					lock (idleHandlersLock)
						idleHandlers.Add (idle);
			}
		}

		bool running;

		/// <summary>
		///   Stops the mainloop.
		/// </summary>
		public void Stop ()
		{
			running = false;
			Driver.Wakeup ();
		}

		/// <summary>
		///   Determines whether there are pending events to be processed.
		/// </summary>
		/// <remarks>
		///   You can use this method if you want to probe if events are pending.
		///   Typically used if you need to flush the input queue while still
		///   running some of your own code in your main thread.
		/// </remarks>
		public bool EventsPending (bool wait = false)
		{
			return Driver.EventsPending (wait);
		}

		/// <summary>
		///   Runs one iteration of timers and file watches
		/// </summary>
		/// <remarks>
		///   You use this to process all pending events (timers, idle handlers and file watches).
		///
		///   You can use it like this:
		///     while (main.EvensPending ()) MainIteration ();
		/// </remarks>
		public void MainIteration ()
		{
			if (timeouts.Count > 0)
				RunTimers ();

			Driver.MainIteration ();

			bool runIdle = false;
			lock (idleHandlersLock) {
				runIdle = idleHandlers.Count > 0;
			}
			if (runIdle) {
				RunIdle ();
			}
		}

		/// <summary>
		///   Runs the mainloop.
		/// </summary>
		public void Run ()
		{
			bool prev = running;
			running = true;
			while (running) {
				EventsPending (true);
				MainIteration ();
			}
			running = prev;
		}
	}
}
