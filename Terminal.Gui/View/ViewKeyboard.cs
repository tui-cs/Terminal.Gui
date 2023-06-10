using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Terminal.Gui {
	public partial class View  {
		ShortcutHelper _shortcutHelper;

		/// <summary>
		/// Event invoked when the <see cref="HotKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

		Key _hotKey = Key.Null;

		/// <summary>
		/// Gets or sets the HotKey defined for this view. A user pressing HotKey on the keyboard while this view has focus will cause the Clicked event to fire.
		/// </summary>
		public virtual Key HotKey {
			get => _hotKey;
			set {
				if (_hotKey != value) {
					var v = value == Key.Unknown ? Key.Null : value;
					if (_hotKey != Key.Null && ContainsKeyBinding (Key.Space | _hotKey)) {
						if (v == Key.Null) {
							ClearKeyBinding (Key.Space | _hotKey);
						} else {
							ReplaceKeyBinding (Key.Space | _hotKey, Key.Space | v);
						}
					} else if (v != Key.Null) {
						AddKeyBinding (Key.Space | v, Command.Accept);
					}
					_hotKey = TextFormatter.HotKey = v;
				}
			}
		}

		/// <summary>
		/// Gets or sets the specifier character for the hotkey (e.g. '_'). Set to '\xffff' to disable hotkey support for this View instance. The default is '\xffff'. 
		/// </summary>
		public virtual Rune HotKeySpecifier {
			get {
				if (TextFormatter != null) {
					return TextFormatter.HotKeySpecifier;
				} else {
					return new Rune ('\xFFFF');
				}
			}
			set {
				TextFormatter.HotKeySpecifier = value;
				SetHotKey ();
			}
		}

		/// <summary>
		/// This is the global setting that can be used as a global shortcut to invoke an action if provided.
		/// </summary>
		public Key Shortcut {
			get => _shortcutHelper.Shortcut;
			set {
				if (_shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == Key.Null)) {
					_shortcutHelper.Shortcut = value;
				}
			}
		}

		/// <summary>
		/// The keystroke combination used in the <see cref="Shortcut"/> as string.
		/// </summary>
		public string ShortcutTag => ShortcutHelper.GetShortcutTag (_shortcutHelper.Shortcut);

		/// <summary>
		/// The action to run if the <see cref="Shortcut"/> is defined.
		/// </summary>
		public virtual Action ShortcutAction { get; set; }

		// This is null, and allocated on demand.
		List<View> _tabIndexes;

		/// <summary>
		/// Configurable keybindings supported by the control
		/// </summary>
		private Dictionary<Key, Command []> KeyBindings { get; set; } = new Dictionary<Key, Command []> ();
		private Dictionary<Command, Func<bool?>> CommandImplementations { get; set; } = new Dictionary<Command, Func<bool?>> ();

		/// <summary>
		/// This returns a tab index list of the subviews contained by this view.
		/// </summary>
		/// <value>The tabIndexes.</value>
		public IList<View> TabIndexes => _tabIndexes?.AsReadOnly () ?? _empty;

		int _tabIndex = -1;

		/// <summary>
		/// Indicates the index of the current <see cref="View"/> from the <see cref="TabIndexes"/> list.
		/// </summary>
		public int TabIndex {
			get { return _tabIndex; }
			set {
				if (!CanFocus) {
					_tabIndex = -1;
					return;
				} else if (SuperView?._tabIndexes == null || SuperView?._tabIndexes.Count == 1) {
					_tabIndex = 0;
					return;
				} else if (_tabIndex == value) {
					return;
				}
				_tabIndex = value > SuperView._tabIndexes.Count - 1 ? SuperView._tabIndexes.Count - 1 : value < 0 ? 0 : value;
				_tabIndex = GetTabIndex (_tabIndex);
				if (SuperView._tabIndexes.IndexOf (this) != _tabIndex) {
					SuperView._tabIndexes.Remove (this);
					SuperView._tabIndexes.Insert (_tabIndex, this);
					SetTabIndex ();
				}
			}
		}

		int GetTabIndex (int idx)
		{
			var i = 0;
			foreach (var v in SuperView._tabIndexes) {
				if (v._tabIndex == -1 || v == this) {
					continue;
				}
				i++;
			}
			return Math.Min (i, idx);
		}

		void SetTabIndex ()
		{
			var i = 0;
			foreach (var v in SuperView._tabIndexes) {
				if (v._tabIndex == -1) {
					continue;
				}
				v._tabIndex = i;
				i++;
			}
		}

		bool _tabStop = true;

		/// <summary>
		/// This only be <see langword="true"/> if the <see cref="CanFocus"/> is also <see langword="true"/> 
		/// and the focus can be avoided by setting this to <see langword="false"/>
		/// </summary>
		public bool TabStop {
			get => _tabStop;
			set {
				if (_tabStop == value) {
					return;
				}
				_tabStop = CanFocus && value;
			}
		}

		int _oldTabIndex;
		
		/// <summary>
		/// Invoked when a character key is pressed and occurs after the key up event.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyPress;

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (Focused?.Enabled == true) {
				Focused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}

			return Focused?.Enabled == true && Focused?.ProcessKey (keyEvent) == true;
		}

		/// <summary>
		/// Invokes any binding that is registered on this <see cref="View"/>
		/// and matches the <paramref name="keyEvent"/>
		/// </summary>
		/// <param name="keyEvent">The key event passed.</param>
		protected bool? InvokeKeybindings (KeyEvent keyEvent)
		{
			bool? toReturn = null;

			if (KeyBindings.ContainsKey (keyEvent.Key)) {

				foreach (var command in KeyBindings [keyEvent.Key]) {

					if (!CommandImplementations.ContainsKey (command)) {
						throw new NotSupportedException ($"A KeyBinding was set up for the command {command} ({keyEvent.Key}) but that command is not supported by this View ({GetType ().Name})");
					}

					// each command has its own return value
					var thisReturn = CommandImplementations [command] ();

					// if we haven't got anything yet, the current command result should be used
					if (toReturn == null) {
						toReturn = thisReturn;
					}

					// if ever see a true then that's what we will return
					if (thisReturn ?? false) {
						toReturn = true;
					}
				}
			}

			return toReturn;
		}

		/// <summary>
		/// <para>Adds a new key combination that will trigger the given <paramref name="command"/>
		/// (if supported by the View - see <see cref="GetSupportedCommands"/>)
		/// </para>
		/// <para>If the key is already bound to a different <see cref="Command"/> it will be
		/// rebound to this one</para>
		/// <remarks>Commands are only ever applied to the current <see cref="View"/>(i.e. this feature
		/// cannot be used to switch focus to another view and perform multiple commands there) </remarks>
		/// </summary>
		/// <param name="key"></param>
		/// <param name="command">The command(s) to run on the <see cref="View"/> when <paramref name="key"/> is pressed.
		/// When specifying multiple commands, all commands will be applied in sequence. The bound <paramref name="key"/> strike
		/// will be consumed if any took effect.</param>
		public void AddKeyBinding (Key key, params Command [] command)
		{
			if (command.Length == 0) {
				throw new ArgumentException ("At least one command must be specified", nameof (command));
			}

			if (KeyBindings.ContainsKey (key)) {
				KeyBindings [key] = command;
			} else {
				KeyBindings.Add (key, command);
			}
		}

		/// <summary>
		/// Replaces a key combination already bound to <see cref="Command"/>.
		/// </summary>
		/// <param name="fromKey">The key to be replaced.</param>
		/// <param name="toKey">The new key to be used.</param>
		protected void ReplaceKeyBinding (Key fromKey, Key toKey)
		{
			if (KeyBindings.ContainsKey (fromKey)) {
				var value = KeyBindings [fromKey];
				KeyBindings.Remove (fromKey);
				KeyBindings [toKey] = value;
			}
		}

		/// <summary>
		/// Checks if the key binding already exists.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns><see langword="true"/> If the key already exist, <see langword="false"/> otherwise.</returns>
		public bool ContainsKeyBinding (Key key)
		{
			return KeyBindings.ContainsKey (key);
		}

		/// <summary>
		/// Removes all bound keys from the View and resets the default bindings.
		/// </summary>
		public void ClearKeyBindings ()
		{
			KeyBindings.Clear ();
		}

		/// <summary>
		/// Clears the existing keybinding (if any) for the given <paramref name="key"/>.
		/// </summary>
		/// <param name="key"></param>
		public void ClearKeyBinding (Key key)
		{
			KeyBindings.Remove (key);
		}

		/// <summary>
		/// Removes all key bindings that trigger the given command. Views can have multiple different
		/// keys bound to the same command and this method will clear all of them.
		/// </summary>
		/// <param name="command"></param>
		public void ClearKeyBinding (params Command [] command)
		{
			foreach (var kvp in KeyBindings.Where (kvp => kvp.Value.SequenceEqual (command)).ToArray ()) {
				KeyBindings.Remove (kvp.Key);
			}
		}

		/// <summary>
		/// <para>States that the given <see cref="View"/> supports a given <paramref name="command"/>
		/// and what <paramref name="f"/> to perform to make that command happen
		/// </para>
		/// <para>If the <paramref name="command"/> already has an implementation the <paramref name="f"/>
		/// will replace the old one</para>
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="f">The function.</param>
		protected void AddCommand (Command command, Func<bool?> f)
		{
			// if there is already an implementation of this command
			if (CommandImplementations.ContainsKey (command)) {
				// replace that implementation
				CommandImplementations [command] = f;
			} else {
				// else record how to perform the action (this should be the normal case)
				CommandImplementations.Add (command, f);
			}
		}

		/// <summary>
		/// Returns all commands that are supported by this <see cref="View"/>.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Command> GetSupportedCommands ()
		{
			return CommandImplementations.Keys;
		}

		/// <summary>
		/// Gets the key used by a command.
		/// </summary>
		/// <param name="command">The command to search.</param>
		/// <returns>The <see cref="Key"/> used by a <see cref="Command"/></returns>
		public Key GetKeyFromCommand (params Command [] command)
		{
			return KeyBindings.First (kb => kb.Value.SequenceEqual (command)).Key;
		}

		/// <inheritdoc/>
		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			if (MostFocused?.Enabled == true) {
				MostFocused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}
			if (MostFocused?.Enabled == true && MostFocused?.ProcessKey (keyEvent) == true)
				return true;
			if (_subviews == null || _subviews.Count == 0)
				return false;

			foreach (var view in _subviews)
				if (view.Enabled && view.ProcessHotKey (keyEvent))
					return true;
			return false;
		}

		/// <inheritdoc/>
		public override bool ProcessColdKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (MostFocused?.Enabled == true) {
				MostFocused?.KeyPress?.Invoke (this, args);
				if (args.Handled)
					return true;
			}
			if (MostFocused?.Enabled == true && MostFocused?.ProcessKey (keyEvent) == true)
				return true;
			if (_subviews == null || _subviews.Count == 0)
				return false;

			foreach (var view in _subviews)
				if (view.Enabled && view.ProcessColdKey (keyEvent))
					return true;
			return false;
		}

		/// <summary>
		/// Invoked when a key is pressed.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyDown;

		/// <inheritdoc/>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyDown?.Invoke (this, args);
			if (args.Handled) {
				return true;
			}
			if (Focused?.Enabled == true) {
				Focused.KeyDown?.Invoke (this, args);
				if (args.Handled) {
					return true;
				}
				if (Focused?.OnKeyDown (keyEvent) == true) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Invoked when a key is released.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyUp;

		/// <inheritdoc/>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			var args = new KeyEventEventArgs (keyEvent);
			KeyUp?.Invoke (this, args);
			if (args.Handled) {
				return true;
			}
			if (Focused?.Enabled == true) {
				Focused.KeyUp?.Invoke (this, args);
				if (args.Handled) {
					return true;
				}
				if (Focused?.OnKeyUp (keyEvent) == true) {
					return true;
				}
			}

			return false;
		}
		
		void SetHotKey ()
		{
			if (TextFormatter == null) {
				return; // throw new InvalidOperationException ("Can't set HotKey unless a TextFormatter has been created");
			}
			TextFormatter.FindHotKey (_text, HotKeySpecifier, true, out _, out var hk);
			if (_hotKey != hk) {
				HotKey = hk;
			}
		}
	}
}
