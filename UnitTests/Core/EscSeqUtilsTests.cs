using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace Terminal.Gui.CoreTests {
	public class EscSeqUtilsTests {
		[Fact]
		public void Defaults_Values ()
		{
			Assert.Equal ('\x1b', EscSeqUtils.KeyEsc);
			Assert.Equal ("\x1b[", EscSeqUtils.KeyCSI);
			Assert.Equal ("\x1b[?1003h", EscSeqUtils.CSI_EnableAnyEventMouse);
			Assert.Equal ("\x1b[?1006h", EscSeqUtils.CSI_EnableSgrExtModeMouse);
			Assert.Equal ("\x1b[?1015h", EscSeqUtils.CSI_EnableUrxvtExtModeMouse);
			Assert.Equal ("\x1b[?1003l", EscSeqUtils.CSI_DisableAnyEventMouse);
			Assert.Equal ("\x1b[?1006l", EscSeqUtils.CSI_DisableSgrExtModeMouse);
			Assert.Equal ("\x1b[?1015l", EscSeqUtils.CSI_DisableUrxvtExtModeMouse);
			Assert.Equal ("\x1b[?1003h\x1b[?1015h\u001b[?1006h", EscSeqUtils.EnableMouseEvents);
			Assert.Equal ("\x1b[?1003l\x1b[?1015l\u001b[?1006l", EscSeqUtils.DisableMouseEvents);
		}

		[Fact]
		public void GetConsoleInputKey_ConsoleKeyInfo ()
		{
			var cki = new ConsoleKeyInfo ('r', 0, false, false, false);
			var expectedCki = new ConsoleKeyInfo ('r', 0, false, false, false);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('r', 0, true, false, false);
			expectedCki = new ConsoleKeyInfo ('r', 0, true, false, false);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('r', 0, false, true, false);
			expectedCki = new ConsoleKeyInfo ('r', 0, false, true, false);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('r', 0, false, false, true);
			expectedCki = new ConsoleKeyInfo ('r', 0, false, false, true);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('r', 0, true, true, false);
			expectedCki = new ConsoleKeyInfo ('r', 0, true, true, false);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('r', 0, false, true, true);
			expectedCki = new ConsoleKeyInfo ('r', 0, false, true, true);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('r', 0, true, true, true);
			expectedCki = new ConsoleKeyInfo ('r', 0, true, true, true);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('\u0012', 0, false, false, false);
			expectedCki = new ConsoleKeyInfo ('R', ConsoleKey.R, false, false, true);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('\0', (ConsoleKey)64, false, false, true);
			expectedCki = new ConsoleKeyInfo (' ', ConsoleKey.Spacebar, false, false, true);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('\r', 0, false, false, false);
			expectedCki = new ConsoleKeyInfo ('\r', ConsoleKey.Enter, false, false, false);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('\u007f', 0, false, false, false);
			expectedCki = new ConsoleKeyInfo ('\u007f', ConsoleKey.Backspace, false, false, false);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));

			cki = new ConsoleKeyInfo ('R', 0, false, false, false);
			expectedCki = new ConsoleKeyInfo ('R', 0, false, false, false);
			Assert.Equal (expectedCki, EscSeqUtils.GetConsoleInputKey (cki));
		}

		[Fact]
		public void ResizeArray_ConsoleKeyInfo ()
		{
			ConsoleKeyInfo [] expectedCkInfos = null;
			var cki = new ConsoleKeyInfo ('\u001b', ConsoleKey.Escape, false, false, false);
			expectedCkInfos = EscSeqUtils.ResizeArray (cki, expectedCkInfos);
			Assert.Single (expectedCkInfos);
			Assert.Equal (cki, expectedCkInfos [0]);
		}

		private EscSeqReqProc escSeqReqProc;
		private ConsoleKeyInfo newConsoleKeyInfo;
		private ConsoleKey key;
		private ConsoleKeyInfo [] cki;
		private ConsoleModifiers mod;
		private string c1Control, code, terminating;
		private string [] values;
		private bool isKeyMouse;
		private bool isReq;
		private List<MouseFlags> mouseFlags;
		Point pos;
		private MouseFlags arg1;
		private Point arg2;
		private bool actionStarted;

		[Fact, AutoInitShutdown]
		public void DecodeEscSeq_Tests ()
		{
			// ESC
			cki = new ConsoleKeyInfo [] { new ConsoleKeyInfo ('\u001b', 0, false, false, false) };
			var expectedCki = new ConsoleKeyInfo ('\u001b', ConsoleKey.Escape, false, false, false);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.Escape, key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("ESC", c1Control);
			Assert.Null (code);
			Assert.Null (values);
			Assert.Null (terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('\u0012', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\u0012', ConsoleKey.R, false, true, true);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.R, key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("ESC", c1Control);
			Assert.Null (code);
			Assert.Null (values);
			Assert.Equal ("\u0012", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('r', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('R', ConsoleKey.R, false, true, false);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.R, key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("ESC", c1Control);
			Assert.Null (code);
			Assert.Null (values);
			Assert.Equal ("r", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			// SS3
			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('O', 0, false, false, false),
				new ConsoleKeyInfo ('R', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\0', ConsoleKey.F3, false, false, false);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.F3, key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("SS3", c1Control);
			Assert.Null (code);
			Assert.Single (values);
			Assert.Null (values [0]);
			Assert.Equal ("R", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			// CSI
			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('1', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo ('R', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\0', ConsoleKey.F3, true, false, false);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.F3, key);
			Assert.Equal (ConsoleModifiers.Shift, mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (2, values.Length);
			Assert.Equal ("1", values [0]);
			Assert.Equal ("2", values [^1]);
			Assert.Equal ("R", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('1', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('R', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\0', ConsoleKey.F3, false, true, false);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.F3, key);
			Assert.Equal (ConsoleModifiers.Alt, mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (2, values.Length);
			Assert.Equal ("1", values [0]);
			Assert.Equal ("3", values [^1]);
			Assert.Equal ("R", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('1', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('4', 0, false, false, false),
				new ConsoleKeyInfo ('R', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\0', ConsoleKey.F3, true, true, false);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.F3, key);
			Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Alt, mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (2, values.Length);
			Assert.Equal ("1", values [0]);
			Assert.Equal ("4", values [^1]);
			Assert.Equal ("R", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('1', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('5', 0, false, false, false),
				new ConsoleKeyInfo ('R', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\0', ConsoleKey.F3, false, false, true);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.F3, key);
			Assert.Equal (ConsoleModifiers.Control, mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (2, values.Length);
			Assert.Equal ("1", values [0]);
			Assert.Equal ("5", values [^1]);
			Assert.Equal ("R", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('1', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('6', 0, false, false, false),
				new ConsoleKeyInfo ('R', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\0', ConsoleKey.F3, true, false, true);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.F3, key);
			Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Control, mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (2, values.Length);
			Assert.Equal ("1", values [0]);
			Assert.Equal ("6", values [^1]);
			Assert.Equal ("R", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('1', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('7', 0, false, false, false),
				new ConsoleKeyInfo ('R', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\0', ConsoleKey.F3, false, true, true);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.F3, key);
			Assert.Equal (ConsoleModifiers.Alt | ConsoleModifiers.Control, mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (2, values.Length);
			Assert.Equal ("1", values [0]);
			Assert.Equal ("7", values [^1]);
			Assert.Equal ("R", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('1', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('8', 0, false, false, false),
				new ConsoleKeyInfo ('R', 0, false, false, false)
			};
			expectedCki = new ConsoleKeyInfo ('\0', ConsoleKey.F3, true, true, true);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (ConsoleKey.F3, key);
			Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control, mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (2, values.Length);
			Assert.Equal ("1", values [0]);
			Assert.Equal ("8", values [^1]);
			Assert.Equal ("R", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('M', 0, false, false, false)
			};
			expectedCki = default;
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (0, (int)key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Equal ("<", code);
			Assert.Equal (3, values.Length);
			Assert.Equal ("0", values [0]);
			Assert.Equal ("2", values [1]);
			Assert.Equal ("3", values [^1]);
			Assert.Equal ("M", terminating);
			Assert.True (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1Pressed }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('m', 0, false, false, false)
			};
			expectedCki = default;
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (0, (int)key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Equal ("<", code);
			Assert.Equal (3, values.Length);
			Assert.Equal ("0", values [0]);
			Assert.Equal ("2", values [1]);
			Assert.Equal ("3", values [^1]);
			Assert.Equal ("m", terminating);
			Assert.True (isKeyMouse);
			Assert.Equal (2, mouseFlags.Count);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1Released, MouseFlags.Button1Clicked }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('M', 0, false, false, false)
			};
			expectedCki = default;
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (0, (int)key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Equal ("<", code);
			Assert.Equal (3, values.Length);
			Assert.Equal ("0", values [0]);
			Assert.Equal ("2", values [1]);
			Assert.Equal ("3", values [^1]);
			Assert.Equal ("M", terminating);
			Assert.True (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1DoubleClicked }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);
			Assert.False (isReq);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('M', 0, false, false, false)
			};
			expectedCki = default;
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (0, (int)key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Equal ("<", code);
			Assert.Equal (3, values.Length);
			Assert.Equal ("0", values [0]);
			Assert.Equal ("2", values [1]);
			Assert.Equal ("3", values [^1]);
			Assert.Equal ("M", terminating);
			Assert.True (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1TripleClicked }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);
			Assert.False (isReq);

			var view = new View () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				WantContinuousButtonPressed = true
			};
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 0,
					Y = 0,
					Flags = 0
				});

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('M', 0, false, false, false)
			};
			expectedCki = default;
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (0, (int)key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Equal ("<", code);
			Assert.Equal (3, values.Length);
			Assert.Equal ("0", values [0]);
			Assert.Equal ("2", values [1]);
			Assert.Equal ("3", values [^1]);
			Assert.Equal ("M", terminating);
			Assert.True (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1Pressed }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);
			Assert.False (isReq);

			Application.Iteration += () => {
				if (actionStarted) {
					// set Application.WantContinuousButtonPressedView to null
					view.WantContinuousButtonPressed = false;
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 0,
							Y = 0,
							Flags = 0
						});

					Application.RequestStop ();
				}
			};

			Application.Run ();

			Assert.Null (Application.WantContinuousButtonPressedView);

			Assert.Equal (MouseFlags.Button1Pressed, arg1);
			Assert.Equal (new Point (1, 2), arg2);

			ClearAll ();
			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('m', 0, false, false, false)
			};
			expectedCki = default;
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Null (escSeqReqProc);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (0, (int)key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Equal ("<", code);
			Assert.Equal (3, values.Length);
			Assert.Equal ("0", values [0]);
			Assert.Equal ("2", values [1]);
			Assert.Equal ("3", values [^1]);
			Assert.Equal ("m", terminating);
			Assert.True (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1Released }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);
			Assert.False (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);

			ClearAll ();

			Assert.Null (escSeqReqProc);
			escSeqReqProc = new EscSeqReqProc ();
			escSeqReqProc.Add ("t");

			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('8', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('1', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo ('t', 0, false, false, false)
			};
			expectedCki = default;
			Assert.Single (escSeqReqProc.EscSeqReqStats);
			Assert.Equal ("t", escSeqReqProc.EscSeqReqStats [^1].Terminating);
			EscSeqUtils.DecodeEscSeq (escSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);
			Assert.Empty (escSeqReqProc.EscSeqReqStats);
			Assert.Equal (expectedCki, newConsoleKeyInfo);
			Assert.Equal (0, (int)key);
			Assert.Equal (0, (int)mod);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (3, values.Length);
			Assert.Equal ("8", values [0]);
			Assert.Equal ("10", values [1]);
			Assert.Equal ("20", values [^1]);
			Assert.Equal ("t", terminating);
			Assert.False (isKeyMouse);
			Assert.Equal (new List<MouseFlags> () { 0 }, mouseFlags);
			Assert.Equal (Point.Empty, pos);
			Assert.True (isReq);
			Assert.Equal (0, (int)arg1);
			Assert.Equal (Point.Empty, arg2);
		}

		private void ClearAll ()
		{
			escSeqReqProc = default;
			newConsoleKeyInfo = default;
			key = default;
			cki = default;
			mod = default;
			c1Control = default;
			code = default;
			terminating = default;
			values = default;
			isKeyMouse = default;
			isReq = default;
			mouseFlags = default;
			pos = default;
			arg1 = default;
			arg2 = default;
		}

		private void ProcessContinuousButtonPressed (MouseFlags arg1, Point arg2)
		{
			this.arg1 = arg1;
			this.arg2 = arg2;
			actionStarted = true;
		}

		[Fact]
		public void GetEscapeResult_Tests ()
		{
			char [] kChars = new char [] { '\u001b', '[', '5', ';', '1', '0', 'r' };
			(c1Control, code, values, terminating) = EscSeqUtils.GetEscapeResult (kChars);
			Assert.Equal ("CSI", c1Control);
			Assert.Null (code);
			Assert.Equal (2, values.Length);
			Assert.Equal ("5", values [0]);
			Assert.Equal ("10", values [^1]);
			Assert.Equal ("r", terminating);
		}

		[Fact]
		public void GetC1ControlChar_Tests ()
		{
			Assert.Equal ("IND", EscSeqUtils.GetC1ControlChar ('D'));
			Assert.Equal ("NEL", EscSeqUtils.GetC1ControlChar ('E'));
			Assert.Equal ("HTS", EscSeqUtils.GetC1ControlChar ('H'));
			Assert.Equal ("RI", EscSeqUtils.GetC1ControlChar ('M'));
			Assert.Equal ("SS2", EscSeqUtils.GetC1ControlChar ('N'));
			Assert.Equal ("SS3", EscSeqUtils.GetC1ControlChar ('O'));
			Assert.Equal ("DCS", EscSeqUtils.GetC1ControlChar ('P'));
			Assert.Equal ("SPA", EscSeqUtils.GetC1ControlChar ('V'));
			Assert.Equal ("EPA", EscSeqUtils.GetC1ControlChar ('W'));
			Assert.Equal ("SOS", EscSeqUtils.GetC1ControlChar ('X'));
			Assert.Equal ("DECID", EscSeqUtils.GetC1ControlChar ('Z'));
			Assert.Equal ("CSI", EscSeqUtils.GetC1ControlChar ('['));
			Assert.Equal ("ST", EscSeqUtils.GetC1ControlChar ('\\'));
			Assert.Equal ("OSC", EscSeqUtils.GetC1ControlChar (']'));
			Assert.Equal ("PM", EscSeqUtils.GetC1ControlChar ('^'));
			Assert.Equal ("APC", EscSeqUtils.GetC1ControlChar ('_'));
			Assert.Equal ("", EscSeqUtils.GetC1ControlChar ('\0'));
		}

		[Fact]
		public void GetConsoleModifiers_Tests ()
		{
			Assert.Equal (ConsoleModifiers.Shift, EscSeqUtils.GetConsoleModifiers ("2"));
			Assert.Equal (ConsoleModifiers.Alt, EscSeqUtils.GetConsoleModifiers ("3"));
			Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Alt, EscSeqUtils.GetConsoleModifiers ("4"));
			Assert.Equal (ConsoleModifiers.Control, EscSeqUtils.GetConsoleModifiers ("5"));
			Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Control, EscSeqUtils.GetConsoleModifiers ("6"));
			Assert.Equal (ConsoleModifiers.Alt | ConsoleModifiers.Control, EscSeqUtils.GetConsoleModifiers ("7"));
			Assert.Equal (ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control, EscSeqUtils.GetConsoleModifiers ("8"));
			Assert.Equal (0, (int)EscSeqUtils.GetConsoleModifiers (""));
		}

		[Fact]
		public void GetConsoleKey_Tests ()
		{
			ConsoleModifiers mod = 0;
			Assert.Equal (ConsoleKey.UpArrow, EscSeqUtils.GetConsoleKey ('A', "", ref mod));
			Assert.Equal (ConsoleKey.DownArrow, EscSeqUtils.GetConsoleKey ('B', "", ref mod));
			Assert.Equal (key = ConsoleKey.RightArrow, EscSeqUtils.GetConsoleKey ('C', "", ref mod));
			Assert.Equal (ConsoleKey.LeftArrow, EscSeqUtils.GetConsoleKey ('D', "", ref mod));
			Assert.Equal (ConsoleKey.End, EscSeqUtils.GetConsoleKey ('F', "", ref mod));
			Assert.Equal (ConsoleKey.Home, EscSeqUtils.GetConsoleKey ('H', "", ref mod));
			Assert.Equal (ConsoleKey.F1, EscSeqUtils.GetConsoleKey ('P', "", ref mod));
			Assert.Equal (ConsoleKey.F2, EscSeqUtils.GetConsoleKey ('Q', "", ref mod));
			Assert.Equal (ConsoleKey.F3, EscSeqUtils.GetConsoleKey ('R', "", ref mod));
			Assert.Equal (ConsoleKey.F4, EscSeqUtils.GetConsoleKey ('S', "", ref mod));
			Assert.Equal (ConsoleKey.Tab, EscSeqUtils.GetConsoleKey ('Z', "", ref mod));
			Assert.Equal (ConsoleModifiers.Shift, mod);
			Assert.Equal (0, (int)EscSeqUtils.GetConsoleKey ('\0', "", ref mod));
			Assert.Equal (ConsoleKey.Insert, EscSeqUtils.GetConsoleKey ('~', "2", ref mod));
			Assert.Equal (ConsoleKey.Delete, EscSeqUtils.GetConsoleKey ('~', "3", ref mod));
			Assert.Equal (ConsoleKey.PageUp, EscSeqUtils.GetConsoleKey ('~', "5", ref mod));
			Assert.Equal (ConsoleKey.PageDown, EscSeqUtils.GetConsoleKey ('~', "6", ref mod));
			Assert.Equal (ConsoleKey.F5, EscSeqUtils.GetConsoleKey ('~', "15", ref mod));
			Assert.Equal (ConsoleKey.F6, EscSeqUtils.GetConsoleKey ('~', "17", ref mod));
			Assert.Equal (ConsoleKey.F7, EscSeqUtils.GetConsoleKey ('~', "18", ref mod));
			Assert.Equal (ConsoleKey.F8, EscSeqUtils.GetConsoleKey ('~', "19", ref mod));
			Assert.Equal (ConsoleKey.F9, EscSeqUtils.GetConsoleKey ('~', "20", ref mod));
			Assert.Equal (ConsoleKey.F10, EscSeqUtils.GetConsoleKey ('~', "21", ref mod));
			Assert.Equal (ConsoleKey.F11, EscSeqUtils.GetConsoleKey ('~', "23", ref mod));
			Assert.Equal (ConsoleKey.F12, EscSeqUtils.GetConsoleKey ('~', "24", ref mod));
			Assert.Equal (0, (int)EscSeqUtils.GetConsoleKey ('~', "", ref mod));
		}

		[Fact]
		public void GetKeyCharArray_Tests ()
		{
			var cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo('\u001b', 0, false, false, false),
				new ConsoleKeyInfo('[', 0, false, false, false),
				new ConsoleKeyInfo('5', 0, false, false, false),
				new ConsoleKeyInfo(';', 0, false, false, false),
				new ConsoleKeyInfo('1', 0, false, false, false),
				new ConsoleKeyInfo('0', 0, false, false, false),
				new ConsoleKeyInfo('r', 0, false, false, false),
			};

			Assert.Equal (new char [] { '\u001b', '[', '5', ';', '1', '0', 'r' }, EscSeqUtils.GetKeyCharArray (cki));
		}

		[Fact, AutoInitShutdown]
		public void GetMouse_Tests ()
		{
			var cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('M', 0, false, false, false)
			};
			EscSeqUtils.GetMouse (cki, out List<MouseFlags> mouseFlags, out Point pos, ProcessContinuousButtonPressed);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1Pressed }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);

			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('m', 0, false, false, false)
			};
			EscSeqUtils.GetMouse (cki, out mouseFlags, out pos, ProcessContinuousButtonPressed);
			Assert.Equal (2, mouseFlags.Count);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1Released, MouseFlags.Button1Clicked }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);

			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('M', 0, false, false, false)
			};
			EscSeqUtils.GetMouse (cki, out mouseFlags, out pos, ProcessContinuousButtonPressed);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1DoubleClicked }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);

			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('M', 0, false, false, false)
			};
			EscSeqUtils.GetMouse (cki, out mouseFlags, out pos, ProcessContinuousButtonPressed);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1TripleClicked }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);

			cki = new ConsoleKeyInfo [] {
				new ConsoleKeyInfo ('\u001b', 0, false, false, false),
				new ConsoleKeyInfo ('[', 0, false, false, false),
				new ConsoleKeyInfo ('<', 0, false, false, false),
				new ConsoleKeyInfo ('0', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('2', 0, false, false, false),
				new ConsoleKeyInfo (';', 0, false, false, false),
				new ConsoleKeyInfo ('3', 0, false, false, false),
				new ConsoleKeyInfo ('m', 0, false, false, false)
			};
			EscSeqUtils.GetMouse (cki, out mouseFlags, out pos, ProcessContinuousButtonPressed);
			Assert.Equal (new List<MouseFlags> () { MouseFlags.Button1Released }, mouseFlags);
			Assert.Equal (new Point (1, 2), pos);
		}

		[Fact]
		public void GetParentProcess_Tests ()
		{
			if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
				Assert.NotNull (EscSeqUtils.GetParentProcess (Process.GetCurrentProcess ()));
			} else {
				Assert.Null (EscSeqUtils.GetParentProcess (Process.GetCurrentProcess ()));
			}
		}
	}
}
