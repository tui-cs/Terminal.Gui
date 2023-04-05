using NStack;
using System;
using Terminal.Gui.Graphs;
using System.Text.Json.Serialization;
using System.Data;
using System.Text;
using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Specifies the border style for a <see cref="View"/> and to be used by the <see cref="Border"/> class.
	/// </summary>
	public enum BorderStyle {
		/// <summary>
		/// No border is drawn.
		/// </summary>
		None,
		/// <summary>
		/// The border is drawn using single-width line glyphs.
		/// </summary>
		Single,
		/// <summary>
		/// The border is drawn using double-width line glyphs.
		/// </summary>
		Double,
		/// <summary>
		/// The border is drawn using single-width line glyphs with rounded corners.
		/// </summary>
		Rounded,
		// TODO: Support Ruler
		///// <summary> 
		///// The border is drawn as a diagnostic ruler ("|123456789...").
		///// </summary>
		//Ruler
	}

	/// <summary>
	/// Defines the visual border for a <see cref="Frame"/>. Also provides helper APIS for rendering the border.
	/// </summary>
	public class Border {

		/// <summary>
		/// Raised if any of the properties that define the border are changed.
		/// </summary>
		public event Action<Border> BorderChanged;

		private BorderStyle _style;
		private Color _forgroundColor;
		private Color _backgroundColor;

		/// <summary>
		/// Specifies the <see cref="Gui.BorderStyle"/> for a view.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (JsonStringEnumConverter))]
		public BorderStyle BorderStyle {
			get => _style;
			set {
				_style = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Color"/> that draws the outer border color.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (Configuration.ColorJsonConverter))]
		public Color ForgroundColor {
			get => _forgroundColor;
			set {
				_forgroundColor = value;
				OnBorderChanged ();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Color"/> that fills the area between the bounds of a <see cref="Border"/>.
		/// </summary>
		[JsonInclude, JsonConverter (typeof (Configuration.ColorJsonConverter))]
		public Color BackgroundColor {
			get => _backgroundColor;
			set {
				_backgroundColor = value;
				OnBorderChanged ();
			}
		}

		// TODO: These are all temporary to keep code compiling
		/// <summary>
		/// 
		/// </summary>
		public bool DrawMarginFrame { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public Point Effect3DOffset { get; set; } = new Point (1, 1);
		/// <summary>
		/// 
		/// </summary>
		public bool Effect3D { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public Thickness BorderThickness { get; set; } = new Thickness (0);
		/// <summary>
		/// 
		/// </summary>
		public object Effect3DBrush { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public Thickness PaddingThickness { get; set; } = new Thickness (0);

		/// <summary>
		/// Invoke the <see cref="BorderChanged"/> event.
		/// </summary>
		public virtual void OnBorderChanged ()
		{
			BorderChanged?.Invoke (this);
		}
	}
}