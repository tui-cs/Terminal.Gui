using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// <see cref="ITableSource"/> implementation that wraps arbitrary data.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class EnumerableTableSource<T> : ITableSource {
		private T [] data;
		private string [] cols;
		private Dictionary<string, Func<T, object>> lamdas;

		/// <summary>
		/// Creates a new instance of the class that presents <paramref name="data"/>
		/// collection as a table.
		/// </summary>
		/// <remarks>The elements of the <paramref name="data"/> collection are recorded during
		/// construction (immutable) but the properties of those objects are permitted to
		/// change.</remarks>
		/// <param name="data">The data that you want to present.  The members of this collection
		/// will be frozen after construction.</param>
		/// <param name="columnDefinitions">
		/// Getter methods for each property you want to present in the table. For example:
		/// <code>
		/// new () {
		///    { "Colname1", (t)=>t.SomeField},
		///    { "Colname2", (t)=>t.SomeOtherField}
		///}
		/// </code></param>
		public EnumerableTableSource (IEnumerable<T> data, Dictionary<string, Func<T, object>> columnDefinitions)
		{
			this.data = data.ToArray ();
			this.cols = columnDefinitions.Keys.ToArray ();
			this.lamdas = columnDefinitions;
		}

		/// <inheritdoc/>
		public object this [int row, int col] {
			get => this.lamdas [ColumnNames [col]] (this.data [row]);
		}

		/// <inheritdoc/>
		public int Rows => data.Length;

		/// <inheritdoc/>
		public int Columns => cols.Length;

		/// <inheritdoc/>
		public string [] ColumnNames => cols;

		/// <summary>
		/// Gets the object collection hosted by this wrapper.
		/// </summary>
		public IReadOnlyCollection<T> Data => this.data.AsReadOnly();
	}
}
