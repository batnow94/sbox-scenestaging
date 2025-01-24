using System;

namespace Sandbox.MovieMaker;

#nullable enable

[Icon( "movie" )]
public sealed partial class MoviePlayer : Component
{
	private MovieClip? _embeddedClip;
	private MovieFile? _referencedFile;

	private float _position;

	[Property, Group( "Source" ), Hide]
	public MovieClip? EmbeddedClip
	{
		get => _embeddedClip;
		set
		{
			_embeddedClip = value;
			_referencedFile = value is not null ? null : _referencedFile;

			UpdatePosition();
		}
	}

	[Property, Group( "Source" ), Title( "Movie File" )]
	public MovieFile? ReferencedFile
	{
		get => _referencedFile;
		set
		{
			_referencedFile = value;
			_embeddedClip = value is not null ? null : _embeddedClip;

			UpdatePosition();
		}
	}

	public MovieClip? MovieClip => _embeddedClip ?? _referencedFile?.Clip;

	[Property, Group( "Playback" )]
	public bool IsPlaying { get; set; }

	[Property, Group( "Playback" )]
	public bool IsLooping { get; set; }

	[Property, Group( "Playback" )]
	public float TimeScale { get; set; } = 1f;

	[Property, Group( "Playback" )]
	public float Position
	{
		get => _position;
		set
		{
			_position = value;
			UpdatePosition();
		}
	}

	/// <summary>
	/// Apply the movie clip to the scene at the current time position.
	/// If we reach the end, check <see cref="IsLooping"/> to either jump back to the start, or stop playback.
	/// </summary>
	private void UpdatePosition()
	{
		if ( MovieClip is null ) return;

		var duration = MovieClip.Duration;

		if ( _position >= duration )
		{
			if ( IsLooping && duration > 0f )
			{
				_position -= MathF.Floor( _position / duration ) * duration;
			}
			else
			{
				_position = duration;
				IsPlaying = false;
			}
		}

		// We allow negative positions so we can have a delayed start

		if ( _sceneRefMap.Count > 0 && _position >= 0f )
		{
			ApplyFrame( _position );
		}
	}

	internal void ApplyFrame( float time )
	{
		if ( MovieClip is null ) return;

		foreach ( var track in MovieClip.RootTracks )
		{
			ApplyFrame( track, time );
		}
	}

	internal void ApplyFrame( MovieTrack track, float time )
	{
		// TODO: this is a slow placeholder implementation, we can avoid boxing / reflection when we're in the engine

		if ( GetOrAutoResolveProperty( track ) is { } property && track.GetBlock( time ) is { } block )
		{
			switch ( block.Data )
			{
				case IConstantData constantData:
					property.Value = constantData.Value;
					break;

				case ISamplesData samplesData:
					property.Value = samplesData.GetValue( time - block.StartTime );
					break;

				case ActionData:
					throw new NotImplementedException();
			}
		}

		foreach ( var child in track.Children )
		{
			ApplyFrame( child, time );
		}
	}

	protected override void OnEnabled()
	{
		UpdatePosition();
	}

	protected override void OnUpdate()
	{
		if ( IsPlaying )
		{
			Position += Time.Delta * TimeScale;
		}
	}
}
