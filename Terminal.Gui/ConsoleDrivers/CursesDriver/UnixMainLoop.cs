//
// mainloop.cs: Simple managed mainloop implementation.
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Terminal.Gui {
	/// <summary>
	/// Unix main loop, suitable for using on Posix systems
	/// </summary>
	/// <remarks>
	/// In addition to the general functions of the mainloop, the Unix version
	/// can watch file descriptors using the AddWatch methods.
	/// </remarks>
	internal class UnixMainLoop : IMainLoopDriver {
		public UnixMainLoop (ConsoleDriver consoleDriver = null)
		{
			// UnixDriver doesn't use the consoleDriver parameter, but the WindowsDriver does.
		}

		public const int KEY_RESIZE = unchecked((int)0xffffffffffffffff);

		[StructLayout (LayoutKind.Sequential)]
		struct Pollfd {
			public int fd;
			public short events, revents;
		}

		/// <summary>
		///	Condition on which to wake up from file descriptor activity.  These match the Linux/BSD poll definitions.
		/// </summary>
		[Flags]
		public enum Condition : short {
			/// <summary>
			/// There is data to read
			/// </summary>
			PollIn = 1,
			/// <summary>
			/// Writing to the specified descriptor will not block
			/// </summary>
			PollOut = 4,
			/// <summary>
			/// There is urgent data to read
			/// </summary>
			PollPri = 2,
			/// <summary>
			///  Error condition on output
			/// </summary>
			PollErr = 8,
			/// <summary>
			/// Hang-up on output
			/// </summary>
			PollHup = 16,
			/// <summary>
			/// File descriptor is not open.
			/// </summary>
			PollNval = 32
		}

		class Watch {
			public int File;
			public Condition Condition;
			public Func<MainLoop, bool> Callback;
		}

		Dictionary<int, Watch> descriptorWatchers = new Dictionary<int, Watch> ();

		[DllImport ("libc")]
		extern static int poll ([In, Out] Pollfd [] ufds, uint nfds, int timeout);

		[DllImport ("libc")]
		extern static int pipe ([In, Out] int [] pipes);

		[DllImport ("libc")]
		extern static int read (int fd, IntPtr buf, IntPtr n);

		[DllImport ("libc")]
		extern static int write (int fd, IntPtr buf, IntPtr n);

		Pollfd [] pollmap;
		bool poll_dirty = true;
		int [] wakeupPipes = new int [2];
		static IntPtr ignore = Marshal.AllocHGlobal (1);
		static IntPtr readHandle = Marshal.AllocHGlobal (1);
		MainLoop mainLoop;
		bool winChanged;

		public Action WinChanged;

		void IMainLoopDriver.Wakeup ()
		{
			write (wakeupPipes [1], ignore, (IntPtr)1);
		}

		void IMainLoopDriver.Setup (MainLoop mainLoop)
		{
			this.mainLoop = mainLoop;
			pipe (wakeupPipes);
			AddWatch (wakeupPipes [0], Condition.PollIn, ml => {
				read (wakeupPipes [0], ignore, readHandle);
				return true;
			});
		}

		/// <summary>
		///	Removes an active watch from the mainloop.
		/// </summary>
		/// <remarks>
		///	The token parameter is the value returned from AddWatch
		/// </remarks>
		public void RemoveWatch (object token)
		{
			var watch = token as Watch;
			if (watch == null)
				return;
			descriptorWatchers.Remove (watch.File);
		}

		/// <summary>
		///  Watches a file descriptor for activity.
		/// </summary>
		/// <remarks>
		///  When the condition is met, the provided callback
		///  is invoked.  If the callback returns false, the
		///  watch is automatically removed.
		///
		///  The return value is a token that represents this watch, you can
		///  use this token to remove the watch by calling RemoveWatch.
		/// </remarks>
		public object AddWatch (int fileDescriptor, Condition condition, Func<MainLoop, bool> callback)
		{
			if (callback == null)
				throw new ArgumentNullException (nameof (callback));

			var watch = new Watch () { Condition = condition, Callback = callback, File = fileDescriptor };
			descriptorWatchers [fileDescriptor] = watch;
			poll_dirty = true;
			return watch;
		}

		void UpdatePollMap ()
		{
			if (!poll_dirty)
				return;
			poll_dirty = false;

			pollmap = new Pollfd [descriptorWatchers.Count];
			int i = 0;
			foreach (var fd in descriptorWatchers.Keys) {
				pollmap [i].fd = fd;
				pollmap [i].events = (short)descriptorWatchers [fd].Condition;
				i++;
			}
		}

		bool IMainLoopDriver.EventsPending (bool wait)
		{
			UpdatePollMap ();

			bool checkTimersResult = CheckTimers (wait, out var pollTimeout);

			var n = poll (pollmap, (uint)pollmap.Length, pollTimeout);

			if (n == KEY_RESIZE) {
				winChanged = true;
			}

			return checkTimersResult || n >= KEY_RESIZE;
		}

		bool CheckTimers (bool wait, out int pollTimeout)
		{
			long now = DateTime.UtcNow.Ticks;

			if (mainLoop.timeouts.Count > 0) {
				pollTimeout = (int)((mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
				if (pollTimeout < 0) {
					return true;
				}
			} else
				pollTimeout = -1;

			if (!wait)
				pollTimeout = 0;

			int ic;
			lock (mainLoop.idleHandlers) {
				ic = mainLoop.idleHandlers.Count;
			}

			return ic > 0;
		}

		void IMainLoopDriver.Iteration ()
		{
			if (winChanged) {
				winChanged = false;
				WinChanged?.Invoke ();
			}
			if (pollmap != null) {
				foreach (var p in pollmap) {
					Watch watch;

					if (p.revents == 0)
						continue;

					if (!descriptorWatchers.TryGetValue (p.fd, out watch))
						continue;
					if (!watch.Callback (this.mainLoop))
						descriptorWatchers.Remove (p.fd);
				}
			}
		}
	}
}
