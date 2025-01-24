using System;

namespace Sandbox.MovieMaker;

#nullable enable

partial class MoviePlayer
{
	/// <summary>
	/// Maps tracks in a <see cref="MovieClip"/> to objects in the scene. We reference tracks by <see cref="Guid"/>
	/// so tracks from multiple clips can bind to the same property if they share an id. These get serialized with
	/// this component.
	/// </summary>
	private readonly Dictionary<Guid, ISceneReferenceMovieProperty> _sceneRefMap = new();

	/// <summary>
	/// Maps tracks in a <see cref="MovieClip"/> to properties in the scene. These are bound automatically based on
	/// <see cref="_sceneRefMap"/>, so not serialized with this component.
	/// </summary>
	private readonly Dictionary<Guid, IMemberMovieProperty> _memberMap = new();

	/// <summary>
	/// Try to get a property that maps to the given track, returning null if not found.
	/// </summary>
	public IMovieProperty? GetProperty( MovieTrack track )
	{
		var property = (IMovieProperty)_sceneRefMap.GetValueOrDefault( track.Id ) ?? _memberMap.GetValueOrDefault( track.Id );

		if ( property is null ) return null;
		if ( property.PropertyType != track.PropertyType ) return null;

		return property;
	}

	private IMovieProperty? GetOrAutoResolveProperty( MovieTrack track )
	{
		if ( GetProperty( track ) is { } existing ) return existing;

		// Can only try to auto-resolve if we know the parent's identity

		if ( track.Parent is null || GetProperty( track.Parent ) is not { } parentProperty ) return null;

		// If we're looking for a component, find it in the containing GameObject

		if ( track.Parent.PropertyType == typeof(GameObject) && track.PropertyType.IsAssignableTo( typeof(Component) ) && parentProperty.Value is GameObject go )
		{
			if ( go.Components.Get( track.PropertyType ) is { } cmp )
			{
				return _sceneRefMap[track.Id] = MovieProperty.FromComponent( cmp );
			}

			// Not found

			return null;
		}

		// Otherwise must be a named member property

		return _memberMap[track.Id] = MovieProperty.FromMember( parentProperty, track.Name );
	}

	public MovieTrack? GetTrack( GameObject go )
	{
		if ( MovieClip is null ) return null;

		foreach ( var track in MovieClip.AllTracks )
		{
			if ( GetProperty( track ) is not ISceneReferenceMovieProperty property ) continue;

			// References a component instead of a GameObject
			if ( property.Component is not null ) continue;
			if ( property.GameObject == go ) return track;
		}

		return null;
	}

	public MovieTrack? GetTrack( Component cmp )
	{
		if ( MovieClip is null ) return null;

		foreach ( var track in MovieClip.AllTracks )
		{
			if ( GetProperty( track ) is not ISceneReferenceMovieProperty property ) continue;
			if ( property.Component == cmp ) return track;
		}

		return null;
	}

	public MovieTrack? GetTrack( GameObject go, string propertyName )
	{
		return GetTrack( go )?.Children.FirstOrDefault( x => x.Name == propertyName );
	}

	public MovieTrack? GetTrack( Component cmp, string propertyName )
	{
		return GetTrack( cmp )?.Children.FirstOrDefault( x => x.Name == propertyName );
	}

	public MovieTrack GetOrCreateTrack( GameObject go )
	{
		if ( GetTrack( go ) is { } existing ) return existing;

		var property = MovieProperty.FromGameObject( go );
		var track = MovieClip!.AddTrack( property.PropertyName, property.PropertyType );

		_sceneRefMap[track.Id] = property;

		return track;
	}

	public MovieTrack GetOrCreateTrack( Component cmp )
	{
		if ( GetTrack( cmp ) is { } existing ) return existing;

		// Nest component tracks inside the containing game object's track
		var goTrack = GetOrCreateTrack( cmp.GameObject );

		var property = MovieProperty.FromComponent( cmp );
		var track = MovieClip!.AddTrack( property.PropertyName, property.PropertyType, goTrack );

		_sceneRefMap[track.Id] = property;

		return track;
	}

	public MovieTrack GetOrCreateTrack( GameObject go, string propertyName )
	{
		if ( GetTrack( go, propertyName ) is { } existing ) return existing;

		// Nest property tracks inside the containing game object's track
		var goTrack = GetOrCreateTrack( go );
		var goProperty = GetProperty( goTrack )!;

		var property = MovieProperty.FromMember( goProperty, propertyName );
		var track = MovieClip!.AddTrack( property.PropertyName, property.PropertyType, goTrack );

		_memberMap[track.Id] = property;

		return track;
	}

	public MovieTrack GetOrCreateTrack( Component cmp, string propertyName )
	{
		if ( GetTrack( cmp, propertyName ) is { } existing ) return existing;

		// Nest property tracks inside the containing component's track
		var cmpTrack = GetOrCreateTrack( cmp );
		var cmpProperty = GetProperty( cmpTrack )!;

		var property = MovieProperty.FromMember( cmpProperty, propertyName );
		var track = MovieClip!.AddTrack( property.PropertyName, property.PropertyType, cmpTrack );

		_memberMap[track.Id] = property;

		return track;
	}
}
