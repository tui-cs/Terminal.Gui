using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using System.IO;

namespace Terminal.Gui.ViewsTests {
	public class AllViewsTests {
		[Fact]
		public void AllViews_Tests_All_Constructors ()
		{
			Application.Init (new FakeDriver ());

			foreach (var type in GetAllViewClassesCollection ()) {
				Assert.True (Constructors_FullTest (type));
			}

			Application.Shutdown ();
		}

		public bool Constructors_FullTest (Type type)
		{
			foreach (var ctor in type.GetConstructors ()) {
				var view = GetTypeInitializer (type, ctor);
				if (view != null) {
					Assert.True (type.FullName == view.GetType ().FullName);
				}
			}

			return true;
		}

		private static View GetTypeInitializer (Type type, ConstructorInfo ctor)
		{
			View viewType = null;

			if (type.IsGenericType && type.IsTypeDefinition) {
				List<Type> gTypes = new List<Type> ();

				foreach (var args in type.GetGenericArguments ()) {
					gTypes.Add (typeof (object));
				}
				type = type.MakeGenericType (gTypes.ToArray ());

				Assert.IsType (type, (View)Activator.CreateInstance (type));

			} else {
				ParameterInfo [] paramsInfo = ctor.GetParameters ();
				Type paramType;
				List<object> pTypes = new List<object> ();

				if (type.IsGenericType) {
					foreach (var args in type.GetGenericArguments ()) {
						paramType = args.GetType ();
						if (args.Name == "T") {
							pTypes.Add (typeof (object));
						} else {
							AddArguments (paramType, pTypes);
						}
					}
				}

				foreach (var p in paramsInfo) {
					paramType = p.ParameterType;
					if (p.HasDefaultValue) {
						pTypes.Add (p.DefaultValue);
					} else {
						AddArguments (paramType, pTypes);
					}

				}

				if (type.IsGenericType && !type.IsTypeDefinition) {
					viewType = (View)Activator.CreateInstance (type);
					Assert.IsType (type, viewType);
				} else {
					viewType = (View)ctor.Invoke (pTypes.ToArray ());
					Assert.IsType (type, viewType);
				}
			}

			return viewType;
		}

		private static void AddArguments (Type paramType, List<object> pTypes)
		{
			if (paramType == typeof (Rect)) {
				pTypes.Add (Rect.Empty);
			} else if (paramType == typeof (NStack.ustring)) {
				pTypes.Add (NStack.ustring.Empty);
			} else if (paramType == typeof (int)) {
				pTypes.Add (0);
			} else if (paramType == typeof (bool)) {
				pTypes.Add (true);
			} else if (paramType.Name == "IList") {
				pTypes.Add (new List<object> ());
			} else if (paramType.Name == "View") {
				var top = new Toplevel ();
				var view = new View ();
				top.Add (view);
				pTypes.Add (view);
			} else if (paramType.Name == "View[]") {
				pTypes.Add (new View [] { });
			} else if (paramType.Name == "Stream") {
				pTypes.Add (new MemoryStream ());
			} else if (paramType.Name == "String") {
				pTypes.Add (string.Empty);
			} else if (paramType.Name == "TreeView`1[T]") {
				pTypes.Add (string.Empty);
			} else {
				pTypes.Add (null);
			}
		}

		List<Type> GetAllViewClassesCollection ()
		{
			List<Type> types = new List<Type> ();
			foreach (Type type in typeof (View).Assembly.GetTypes ()
			 .Where (myType => myType.IsClass && !myType.IsAbstract && myType.IsPublic && myType.IsSubclassOf (typeof (View)))) {
				types.Add (type);
			}
			return types;
		}

		[Fact]
		public void AllViews_Enter_Leave_Events ()
		{
			foreach (var type in GetAllViewClassesCollection ()) {
				Application.Init (new FakeDriver ());

				var top = Application.Top;
				var vType = GetTypeInitializer (type, type.GetConstructor (Array.Empty<Type> ()));
				if (vType == null) {
					Application.Shutdown ();
					continue;
				}
				vType.X = 0;
				vType.Y = 0;
				vType.Width = 10;
				vType.Height = 1;

				var view = new View () {
					X = 0,
					Y = 1,
					Width = 10,
					Height = 1,
					CanFocus = true
				};
				var vTypeEnter = 0;
				var vTypeLeave = 0;
				var viewEnter = 0;
				var viewLeave = 0;

				vType.Enter += (s,e) => vTypeEnter++;
				vType.Leave += (s, e) => vTypeLeave++;
				view.Enter += (s, e) => viewEnter++;
				view.Leave += (s, e) => viewLeave++;

				top.Add (vType, view);
				Application.Begin (top);

				if (!vType.CanFocus || (vType is Toplevel && ((Toplevel)vType).Modal)) {
					Application.Shutdown ();
					continue;
				}

				if (vType is TextView) {
					top.ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask, new KeyModifiers ()));
				} else {
					top.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
				}
				top.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));

				Assert.Equal (2, vTypeEnter);
				Assert.Equal (1, vTypeLeave);
				Assert.Equal (1, viewEnter);
				Assert.Equal (1, viewLeave);

				Application.Shutdown ();
			}
		}
	}
}
