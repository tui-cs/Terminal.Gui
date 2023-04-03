using System;
using Xunit;

namespace Terminal.Gui.TypeTests {
	public class SizeTests {
		[Fact]
		public void Size_New ()
		{
			var size = new Size ();
			Assert.True (size.IsEmpty);

			size = new Size (new Point ());
			Assert.True (size.IsEmpty);

			size = new Size (3, 4);
			Assert.False (size.IsEmpty);

			Action action = () => new Size (-3, 4);
			var ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Either Width and Height must be greater or equal to 0.", ex.Message);

			action = () => new Size (3, -4);
			ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Either Width and Height must be greater or equal to 0.", ex.Message);

			action = () => new Size (-3, -4);
			ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Either Width and Height must be greater or equal to 0.", ex.Message);

		}

		[Fact]
		public void Size_SetsValue ()
		{
			var size = new Size () {
				Width = 0,
				Height = 0
			};
			Assert.True (size.IsEmpty);

			size = new Size () {
				Width = 3,
				Height = 4
			};
			Assert.False (size.IsEmpty);

			Action action = () => {
				size = new Size () {
					Width = -3,
					Height = 4
				};
			};
			var ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Width must be greater or equal to 0.", ex.Message);

			action = () => {
				size = new Size () {
					Width = 3,
					Height = -4
				};
			};
			ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Height must be greater or equal to 0.", ex.Message);

			action = () => {
				size = new Size () {
					Width = -3,
					Height = -4
				};
			};
			ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Width must be greater or equal to 0.", ex.Message);
		}

		[Fact]
		public void Size_Equals ()
		{
			var size1 = new Size ();
			var size2 = new Size ();
			Assert.Equal (size1, size2);

			size1 = new Size (3, 4);
			size2 = new Size (3, 4);
			Assert.Equal (size1, size2);

			size1 = new Size (3, 4);
			size2 = new Size (4, 4);
			Assert.NotEqual (size1, size2);
		}
	}
}
