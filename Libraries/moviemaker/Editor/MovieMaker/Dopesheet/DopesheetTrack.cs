using System.Linq;

namespace Editor.MovieMaker;

#nullable enable

public class DopesheetTrack : GraphicsItem
{
	public TrackWidget Track { get; }
	public KeyframeCurve? Curve { get; private set; }

	IEnumerable<DopeHandle> Handles => Children.OfType<DopeHandle>();

	public Color HandleColor { get; private set; }

	public DopesheetTrack( TrackWidget track )
	{
		Track = track;
		HoverEvents = true;

		HandleColor = Theme.Grey;

		// TODO: handle color from track type
	}

	protected override void OnPaint()
	{
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
		Position = new Vector2( 0, r.Top + 1 );
		Size = new Vector2( 50000, r.Height );

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
	}
}
