namespace Sandbox.MovieMaker;

#nullable enable

/// <summary>
/// A <see cref="MovieClip"/> stored as a resource.
/// </summary>
public sealed class MovieFile : GameResource
{
	/// <summary>
	/// The movie clip stored in this resource.
	/// </summary>
	public MovieClip? Clip { get; set; }
}
