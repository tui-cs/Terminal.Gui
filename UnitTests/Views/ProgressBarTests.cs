using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewTests {
	public class ProgressBarTests {
		[Fact]
		[AutoInitShutdown]
		public void Default_Contructor ()
		{
			var pb = new ProgressBar ();

			Assert.False (pb.CanFocus);
			Assert.Equal (0, pb.Fraction);
			Assert.Equal (new Attribute (Color.BrightGreen, Color.Gray),
				new Attribute (pb.ColorScheme.Normal.Foreground, pb.ColorScheme.Normal.Background));
			Assert.Equal (Colors.Base.Normal, pb.ColorScheme.HotNormal);
			Assert.Equal (1, pb.Height);
			Assert.Equal (ProgressBarStyle.Blocks, pb.ProgressBarStyle);
			Assert.Equal (ProgressBarFormat.Simple, pb.ProgressBarFormat);
			Assert.Equal (Application.Driver.BlocksMeterSegment, pb.SegmentCharacter);
		}

		[Fact]
		[AutoInitShutdown]
		public void ProgressBarStyle_Setter ()
		{
			var driver = ((FakeDriver)Application.Driver);

			var pb = new ProgressBar ();

			pb.ProgressBarStyle = ProgressBarStyle.Blocks;
			Assert.Equal (driver.BlocksMeterSegment, pb.SegmentCharacter);

			pb.ProgressBarStyle = ProgressBarStyle.Continuous;
			Assert.Equal (driver.ContinuousMeterSegment, pb.SegmentCharacter);

			pb.ProgressBarStyle = ProgressBarStyle.MarqueeBlocks;
			Assert.Equal (driver.BlocksMeterSegment, pb.SegmentCharacter);

			pb.ProgressBarStyle = ProgressBarStyle.MarqueeContinuous;
			Assert.Equal (driver.ContinuousMeterSegment, pb.SegmentCharacter);
		}

		[Fact]
		[AutoInitShutdown]
		public void ProgressBarFormat_Setter ()
		{
			var pb = new ProgressBar ();

			pb.ProgressBarFormat = ProgressBarFormat.Simple;
			Assert.Equal (1, pb.Height);

			pb.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
			Assert.Equal (2, pb.Height);

			pb.ProgressBarFormat = ProgressBarFormat.Framed;
			Assert.Equal (3, pb.Height);

			pb.ProgressBarFormat = ProgressBarFormat.FramedPlusPercentage;
			Assert.Equal (4, pb.Height);

			pb.ProgressBarFormat = ProgressBarFormat.FramedProgressPadded;
			Assert.Equal (6, pb.Height);
		}

		[Fact]
		[AutoInitShutdown]
		public void ProgressBarFormat_MarqueeBlocks_MarqueeContinuous_Setter ()
		{
			var driver = ((FakeDriver)Application.Driver);

			var pb1 = new ProgressBar () { ProgressBarStyle = ProgressBarStyle.MarqueeBlocks };
			var pb2 = new ProgressBar () { ProgressBarStyle = ProgressBarStyle.MarqueeContinuous };

			pb1.ProgressBarFormat = ProgressBarFormat.Simple;
			Assert.Equal (ProgressBarFormat.Simple, pb1.ProgressBarFormat);
			Assert.Equal (1, pb1.Height);
			pb2.ProgressBarFormat = ProgressBarFormat.Simple;
			Assert.Equal (ProgressBarFormat.Simple, pb2.ProgressBarFormat);
			Assert.Equal (1, pb2.Height);

			pb1.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
			Assert.Equal (ProgressBarFormat.SimplePlusPercentage, pb1.ProgressBarFormat);
			Assert.Equal (2, pb1.Height);
			pb2.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
			Assert.Equal (ProgressBarFormat.SimplePlusPercentage, pb2.ProgressBarFormat);
			Assert.Equal (2, pb2.Height);

			pb1.ProgressBarFormat = ProgressBarFormat.Framed;
			Assert.Equal (ProgressBarFormat.Framed, pb1.ProgressBarFormat);
			Assert.Equal (3, pb1.Height);
			pb2.ProgressBarFormat = ProgressBarFormat.Framed;
			Assert.Equal (ProgressBarFormat.Framed, pb2.ProgressBarFormat);
			Assert.Equal (3, pb2.Height);

			pb1.ProgressBarFormat = ProgressBarFormat.FramedPlusPercentage;
			Assert.Equal (ProgressBarFormat.FramedPlusPercentage, pb1.ProgressBarFormat);
			Assert.Equal (4, pb1.Height);
			pb2.ProgressBarFormat = ProgressBarFormat.FramedPlusPercentage;
			Assert.Equal (ProgressBarFormat.FramedPlusPercentage, pb2.ProgressBarFormat);
			Assert.Equal (4, pb2.Height);

			pb1.ProgressBarFormat = ProgressBarFormat.FramedProgressPadded;
			Assert.Equal (ProgressBarFormat.FramedProgressPadded, pb1.ProgressBarFormat);
			Assert.Equal (6, pb1.Height);
			pb2.ProgressBarFormat = ProgressBarFormat.FramedProgressPadded;
			Assert.Equal (ProgressBarFormat.FramedProgressPadded, pb2.ProgressBarFormat);
			Assert.Equal (6, pb2.Height);
		}

		[Fact]
		[AutoInitShutdown]
		public void Text_Setter_Not_Marquee ()
		{
			var pb = new ProgressBar () { Fraction = 0.25F };

			pb.ProgressBarFormat = ProgressBarFormat.Simple;
			pb.Text = "blabla";
			Assert.Equal ("25%", pb.Text);

			pb.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
			pb.Text = "bleble";
			Assert.Equal ("25%", pb.Text);

			pb.ProgressBarFormat = ProgressBarFormat.Framed;
			Assert.Equal ("25%", pb.Text);

			pb.ProgressBarFormat = ProgressBarFormat.FramedPlusPercentage;
			Assert.Equal ("25%", pb.Text);

			pb.ProgressBarFormat = ProgressBarFormat.FramedProgressPadded;
			Assert.Equal ("25%", pb.Text);
		}

		[Fact]
		[AutoInitShutdown]
		public void Text_Setter_Marquee ()
		{
			var pb = new ProgressBar () { Fraction = 0.25F, ProgressBarStyle = ProgressBarStyle.MarqueeBlocks };

			pb.ProgressBarFormat = ProgressBarFormat.Simple;
			pb.Text = "blabla";
			Assert.Equal ("blabla", pb.Text);

			pb.ProgressBarFormat = ProgressBarFormat.SimplePlusPercentage;
			pb.Text = "bleble";
			Assert.Equal ("bleble", pb.Text);

			pb.ProgressBarFormat = ProgressBarFormat.Framed;
			Assert.Equal ("bleble", pb.Text);

			pb.ProgressBarFormat = ProgressBarFormat.FramedPlusPercentage;
			Assert.Equal ("bleble", pb.Text);

			pb.ProgressBarFormat = ProgressBarFormat.FramedProgressPadded;
			Assert.Equal ("bleble", pb.Text);
		}

		[Fact]
		[AutoInitShutdown]
		public void Pulse_Redraw_BidirectionalMarquee_True_Default ()
		{
			var driver = ((FakeDriver)Application.Driver);

			var pb = new ProgressBar () {
				Width = 15,
				ProgressBarStyle = ProgressBarStyle.MarqueeBlocks
			};

			for (int i = 0; i < 38; i++) {
				pb.Pulse ();
				pb.Redraw (pb.Bounds);
				if (i == 0) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 1) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 2) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 3) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 4) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 5) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 6) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 7) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 8) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 9) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 10) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 11) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 12) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 13) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 14) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 15) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 16) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 17) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 18) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 19) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 20) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 21) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 22) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 23) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 24) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 25) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 26) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 27) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 28) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 29) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 30) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 31) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 32) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 33) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 34) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 35) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 36) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 37) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				}
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void Pulse_Redraw_BidirectionalMarquee_False ()
		{
			var driver = ((FakeDriver)Application.Driver);

			var pb = new ProgressBar () {
				Width = 15,
				ProgressBarStyle = ProgressBarStyle.MarqueeBlocks,
				BidirectionalMarquee = false
			};

			for (int i = 0; i < 38; i++) {
				pb.Pulse ();
				pb.Redraw (pb.Bounds);
				if (i == 0) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 1) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 2) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 3) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 4) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 5) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 6) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 7) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 8) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 9) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 10) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 11) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 12) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 13) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 14) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 15) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 16) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 17) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 18) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 19) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 20) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 21) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 22) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 23) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 24) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 25) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 26) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 27) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 28) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 29) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 30) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 31) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 32) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 14, 0]);
				} else if (i == 33) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 34) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 35) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 36) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				} else if (i == 37) {
					Assert.Equal (' ', (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 6, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 7, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 8, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 9, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 10, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 11, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 12, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 13, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 14, 0]);
				}
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void Fraction_Redraw ()
		{
			var driver = ((FakeDriver)Application.Driver);

			var pb = new ProgressBar () {
				Width = 5
			};

			for (int i = 0; i <= pb.Frame.Width; i++) {
				pb.Fraction += 0.2F;
				pb.Redraw (pb.Bounds);
				if (i == 0) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
				} else if (i == 1) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
				} else if (i == 2) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
				} else if (i == 3) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
				} else if (i == 4) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
				} else if (i == 5) {
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 0, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 1, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 2, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 3, 0]);
					Assert.Equal (driver.BlocksMeterSegment, (double)driver.Contents [0, 4, 0]);
					Assert.Equal (' ', (double)driver.Contents [0, 5, 0]);
				}
			}
		}
	}
}
