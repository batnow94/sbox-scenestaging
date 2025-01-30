using System.Linq;

namespace Editor.MovieMaker;

#nullable enable

[Title( "Keyframe Editor" ), Icon( "timeline" ), Order( 0 )]
internal sealed class KeyframeEditMode : EditMode
{
	private IEnumerable<KeyframeHandle> SelectedHandles => SelectedItems.OfType<KeyframeHandle>();
	private IEnumerable<TrackKeyframes> SelectedTracks => SelectedHandles.Select( x => x.Keyframes ).Distinct();

	private readonly Dictionary<DopeSheetTrack, TrackKeyframes> _keyframeMap = new();

	/// <summary>
	/// If true, we automatically record new keyframes when properties are changed
	/// </summary>
	public bool KeyframeRecording { get; set; }

	private TrackKeyframes? GetKeyframes( TrackWidget? track )
	{
		return track is not null ? GetKeyframes( track.Channel ) : null;
	}

	private TrackKeyframes? GetKeyframes( DopeSheetTrack? track )
	{
		return track is not null ? _keyframeMap.GetValueOrDefault( track ) : null;
	}

	private void WriteTracks( IEnumerable<TrackKeyframes> tracks )
	{
		foreach ( var track in tracks )
		{
			track.Write();
		}
	}

	protected override void OnEnable()
	{
		var btn = Toolbar.AddToggle( "Create Keyframes on Edit", "radio_button_checked",
			() => KeyframeRecording,
			x => KeyframeRecording = x );

		btn.ForegroundActive = Theme.Red;
	}

	protected override void OnTrackAdded( DopeSheetTrack track )
	{
		var keyframes = new TrackKeyframes( track );

		_keyframeMap[track] = keyframes;

		keyframes.Read();
	}

	protected override void OnTrackRemoved( DopeSheetTrack track )
	{
		if ( GetKeyframes( track ) is { } keyframes )
		{
			keyframes.Dispose();

			_keyframeMap.Remove( track );
		}
	}

	protected override void OnKeyPress( KeyEvent e )
	{
		if ( e.Key == KeyCode.Left )
		{
			foreach ( var h in SelectedHandles )
			{
				h.Nudge( e.HasShift ? -1.0f : -0.1f );
			}

			e.Accepted = true;
			WriteTracks( SelectedTracks );
			return;
		}

		if ( e.Key == KeyCode.Right )
		{
			foreach ( var h in SelectedHandles )
			{
				h.Nudge( e.HasShift ? 1.0f : 0.1f );
			}

			e.Accepted = true;
			WriteTracks( SelectedTracks );
			return;
		}
	}

	private record struct CopiedHandle( Guid Track, float Time, object Value );
	private static List<CopiedHandle>? Copied { get; set; }

	protected override void OnCopy()
	{
		Copied = new();

		foreach ( var handle in SelectedHandles )
		{
			Copied.Add( new CopiedHandle( handle.Track.Track.Track.Id, handle.Time, handle.Value ) );
		}
	}

	protected override void OnPaste()
	{
		if ( Copied is not { Count: > 0 } copied )
			return;

		var pastePointer = Session.CurrentPointer;
		pastePointer -= copied.Min( x => x.Time );

		var modified = new HashSet<TrackKeyframes>();

		foreach ( var entry in Copied )
		{
			var track = TrackList.Tracks.FirstOrDefault( x => x.Track.Id == entry.Track );
			if ( track is null || GetKeyframes( track ) is not { } keyframes ) continue;

			keyframes.AddKey( entry.Time + pastePointer, entry.Value );

			modified.Add( keyframes );
		}

		WriteTracks( modified );
	}

	protected override void OnDelete()
	{
		var modifiedTracks = SelectedHandles.Select( x => x.Keyframes )
			.Distinct()
			.ToArray();

		foreach ( var h in SelectedHandles )
		{
			h.Destroy();
		}

		WriteTracks( modifiedTracks );
	}

	protected override void OnTrackLayout( DopeSheetTrack track, Rect rect )
	{
		GetKeyframes( track )?.PositionHandles();
	}

	protected override bool OnPreChange( DopeSheetTrack track )
	{
		if ( !KeyframeRecording )
		{
			return false;
		}

		if ( GetKeyframes( track ) is not { } keyframes )
		{
			return false;
		}

		// When about to change a track that doesn't have any keyframes, make a keyframe at t=0
		// with the old value.

		var movieTrack = track.Track.Track;

		if ( movieTrack.Blocks.Count > 0 )
		{
			return false;
		}

		keyframes.AddKey( 0f );
		keyframes.Write();

		return true;
	}

	protected override bool OnPostChange( DopeSheetTrack track )
	{
		if ( GetKeyframes( track ) is not { } keyframes )
		{
			return false;
		}

		if ( KeyframeRecording )
		{
			keyframes.AddKey( Session.CurrentPointer );
		}
		else if ( !keyframes.UpdateKey( Session.CurrentPointer ) )
		{
			return false;
		}

		keyframes.Write();

		return true;
	}
}

public sealed class TrackKeyframes : IDisposable
{
	private static Dictionary<Type, Color> HandleColors { get; } = new()
	{
		{ typeof(Vector3), Theme.Blue },
		{ typeof(Rotation), Theme.Green },
		{ typeof(Color), Theme.Pink },
		{ typeof(float), Theme.Yellow },
	};

	public KeyframeCurve? Curve { get; private set; }

	public DopeSheetTrack DopeSheetTrack { get; }
	public TrackWidget TrackWidget { get; }
	public Color HandleColor { get; private set; }

	public TrackKeyframes( DopeSheetTrack track )
	{
		DopeSheetTrack = track;
		TrackWidget = track.Track;

		HandleColor = Theme.Grey;

		if ( HandleColors.TryGetValue( track.Track.Track.PropertyType, out var color ) )
		{
			HandleColor = color;
		}
	}

	private IEnumerable<KeyframeHandle> Handles => DopeSheetTrack.Children.OfType<KeyframeHandle>();

	public void PositionHandles()
	{
		foreach ( var handle in Handles )
		{
			handle.UpdatePosition();
		}

		DopeSheetTrack.Update();
	}

	internal void AddKey( float time ) => AddKey( time, TrackWidget.Property!.Value );

	internal bool UpdateKey( float time ) => UpdateKey( time, TrackWidget.Property!.Value );

	internal void AddKey( float time, object? value )
	{
		var h = FindKey( time ) ?? new KeyframeHandle( this );

		UpdateKey( h, time, value );
	}

	internal bool UpdateKey( float time, object? value )
	{
		if ( FindKey( time ) is not { } h ) return false;

		UpdateKey( h, time, value );

		return true;
	}

	private void UpdateKey( KeyframeHandle h, float time, object? value )
	{
		//EditorUtility.PlayRawSound( "sounds/editor/add.wav" );
		h.Time = time;
		h.Value = value;

		h.UpdatePosition();

		DopeSheetTrack.Update();
	}

	private KeyframeHandle? FindKey( float time ) => Handles.FirstOrDefault( x => x.Time.AlmostEqual( time, 0.001f ) );

	/// <summary>
	/// Read from the Clip
	/// </summary>
	public void Read()
	{
		foreach ( var h in Handles )
		{
			h.Destroy();
		}

		if ( TrackWidget.Property?.CanHaveKeyframes() ?? false )
		{
			Curve = TrackWidget.Track.ReadKeyframes() ?? KeyframeCurve.Create( TrackWidget.Track.PropertyType );

			foreach ( var keyframe in Curve )
			{
				_ = new KeyframeHandle( this )
				{
					Time = keyframe.Time,
					Value = keyframe.Value
				};
			}
		}

		PositionHandles();
	}

	/// <summary>
	/// Write from this sheet to the target
	/// </summary>
	public void Write()
	{
		if ( Curve is null ) return;

		Curve.Clear();

		foreach ( var handle in Handles )
		{
			Curve.SetKeyframe( handle.Time, handle.Value );
		}

		TrackWidget.Track.WriteKeyframes( Curve );

		Session.Current?.ClipModified();

		DopeSheetTrack.UpdateBlockPreviews();
	}

	public void Dispose()
	{
		foreach ( var handle in Handles )
		{
			handle.Destroy();
		}
	}
}
