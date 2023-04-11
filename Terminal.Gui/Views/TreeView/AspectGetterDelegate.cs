
namespace Terminal.Gui {

	/// <summary>
	/// Delegates of this type are used to fetch string representations of user's model objects
	/// </summary>
	/// <param name="toRender">The object that is being rendered</param>
	/// <returns></returns>
	public delegate string AspectGetterDelegate<T> (T toRender) where T : class;

}
