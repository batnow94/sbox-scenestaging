using System.Linq;

namespace Editor.MovieMaker;

#nullable enable

public partial class DopesheetTrack : GraphicsItem
{
	public TrackWidget Track { get; }

	private bool _canCreatePreview;

	private TrackPreview? Preview { get; set; }
	public KeyframeCurve? Curve { get; private set; }

	public bool Visible => Track.Visible;

	IEnumerable<DopeHandle> Handles => Children.OfType<DopeHandle>();

	public Color HandleColor { get; private set; }

	private static Dictionary<Type, Color> HandleColors { get; } = new()
	{
		{ typeof(Vector3), Theme.Blue },
		{ typeof(Rotation), Theme.Green },
		{ typeof(Color), Theme.Pink },
		{ typeof(float), Theme.Yellow },
	};

	public DopesheetTrack( TrackWidget track )
	{
		Track = track;
		HoverEvents = true;
		Preview = TrackPreview.Create( this, track.Track );

		_canCreatePreview = Preview is not null;

		HandleColor = Theme.Grey;

		if ( HandleColors.TryGetValue( track.Track.PropertyType, out var color ) )
		{
			HandleColor = color;
		}
	}

	protected override void OnPaint()
	{
		if ( !Visible ) return;

		base.OnPaint();

		Paint.SetBrushAndPen( TrackDopesheet.Colors.ChannelBackground );
		Paint.DrawRect( LocalRect );
	}

	public void PositionHandles()
	{
		foreach ( var handle in Handles )
		{
			handle.UpdatePosition();
		}

		Update();
	}

	internal void DoLayout( Rect r )
	{
		PrepareGeometryChange();

		Position = new Vector2( 0, r.Top + 1 );
		Size = Visible ? new Vector2( 50000, r.Height ) : 0f;

		if ( Visible && _canCreatePreview )
		{
			Preview ??= TrackPreview.Create( this, Track.Track )!;

			var scrubBar = Track.TrackList.Editor.ScrubBar;

			Preview.PrepareGeometryChange();
			Preview.Position = new Vector2( Session.Current.TimeToPixels( Math.Max( 0f, scrubBar.GetTimeAt( 0f ) ) ), 0f );
			Preview.Size = new Vector2( scrubBar.Width, r.Height );
		}
		else
		{
			Preview?.Destroy();
			Preview = null;
		}

		PositionHandles();
	}

	internal void OnSelected()
	{
		if ( Track.Property?.GetTargetGameObject() is { } gameObject )
		{
			SceneEditorSession.Active.Selection.Set( gameObject );
		}
	}

	protected override void OnMousePressed( GraphicsMouseEvent e )
	{
		base.OnMousePressed( e );

		if ( e.LeftMouseButton )
		{
			OnSelected();
		}
	}

	internal void AddKey( float time )
	{
		AddKey( time, Track.Property!.Value );
	}

	internal void AddKey( float currentPointer, object value )
	{
		var h = Handles.Where( x => MathX.AlmostEqual( x.Time, currentPointer ) ).FirstOrDefault();

		if ( h is null )
		{
			h = new DopeHandle( this );

			//EditorUtility.PlayRawSound( "sounds/editor/add.wav" );
		}

		h.Time = currentPointer;
		h.Value = value;

		h.UpdatePosition();

		Update();
	}

	/// <summary>
	/// Read from the Clip
	/// </summary>
	public void Read()
	{
		foreach ( var h in Handles )
		{
			h.Destroy();
		}

		if ( Track.Property?.CanHaveKeyframes() ?? false )
		{
			Curve = Track.Track.ReadKeyframes() ?? KeyframeCurve.Create( Track.Track.PropertyType );

			foreach ( var keyframe in Curve )
			{
				_ = new DopeHandle( this )
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

		Track.Track.WriteKeyframes( Curve );

		Session.Current.ClipModified();
	}
}
