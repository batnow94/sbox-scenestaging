using System;
using System.Text.Json.Nodes;
using Sandbox.Diagnostics;

namespace Sandbox.MovieMaker;

#nullable enable

/// <summary>
/// A timeline describing changing property values and actions to run in a scene.
/// </summary>
public sealed partial class MovieClip
{
	/// <summary>
	/// List of tracks, sorted by <see cref="MovieTrack.Id"/>.
	/// </summary>
	private readonly List<MovieTrack> _rootTracks = new();
	private readonly Dictionary<Guid, MovieTrack> _trackDict = new();

	/// <summary>
	/// Set of tracks in this clip that are at the root level in the hierarchy.
	/// </summary>
	public IReadOnlyList<MovieTrack> RootTracks => _rootTracks;

	/// <summary>
	/// All tracks in this clip, including children of other tracks.
	/// </summary>
	public IEnumerable<MovieTrack> AllTracks => _trackDict.Values.OrderBy( x => x.Id );

	/// <summary>
	/// Total number of tracks in the clip, including children of other tracks.
	/// </summary>
	public int TrackCount => _trackDict.Count;

	/// <summary>
	/// How long this clip takes to fully play, in seconds.
	/// </summary>
	public float Duration => _trackDict.Values
		.SelectMany( x => x.Blocks.Select( y => y.StartTime + (y.Duration ?? 0f) ) )
		.DefaultIfEmpty( 0f )
		.Max();

	/// <summary>
	/// Editor-only information about this movie.
	/// </summary>
	public JsonObject? EditorData { get; set; }

	/// <summary>
	/// Adds a new track to this clip, with the given property type.
	/// </summary>
	/// <param name="name">Display name of the track, used in the editor and when auto-resolving.</param>
	/// <param name="type">Property type for this track.</param>
	/// <param name="parent">Optional parent track, for grouping in the hierarchy and auto-resolving.</param>
	public MovieTrack AddTrack( string name, Type type, MovieTrack? parent = null )
	{
		if ( parent is not null )
		{
			Assert.True( parent.IsValid && parent.Clip == this,
				$"Can't parent to a track from a different {nameof(MovieClip)}." );
		}

		var track = new MovieTrack( this, Guid.NewGuid(), name, type, parent );

		AddTrackInternal( track );

		return track;
	}

	private void AddTrackInternal( MovieTrack track )
	{
		_trackDict.Add( track.Id, track );

		if ( track.Parent is null )
		{
			_rootTracks.Add( track );
		}
		else
		{
			track.Parent.AddChildInternal( track );
		}
	}

	/// <summary>
	/// Attempts to get a track with the given <paramref name="trackId"/>.
	/// </summary>
	/// <returns>The matching track, or <see langword="null"/> if not found.</returns>
	public MovieTrack? GetTrack( Guid trackId )
	{
		return _trackDict!.GetValueOrDefault( trackId );
	}

	/// <summary>
	/// Should only be called from <see cref="MovieTrack.Remove"/>
	/// </summary>
	internal void RemoveTrackInternal( MovieTrack track )
	{
		if ( GetTrack( track.Id ) == track )
		{
			_trackDict.Remove( track.Id );
		}

		if ( track.Parent is null )
		{
			_rootTracks.Remove( track );
		}
		else
		{
			track.Parent.RemoveChildInternal( track );
		}
	}
}
