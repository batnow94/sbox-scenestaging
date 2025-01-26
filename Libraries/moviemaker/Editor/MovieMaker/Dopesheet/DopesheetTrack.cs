using System.Linq;
using static Editor.TextureResidencyInfo;

namespace Editor.MovieMaker;

#nullable enable

public class DopesheetTrack : GraphicsItem
{
	public TrackWidget Track { get; }
	private IVectorDecomposer? Decomposer { get; }

	public KeyframeCurve? Curve { get; private set; }

	IEnumerable<DopeHandle> Handles => Children.OfType<DopeHandle>();

	public Color HandleColor { get; private set; }

	private GraphicsLine[]? Lines { get; set; }

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
		Decomposer = VectorDecomposer.GetDefault( Track.Track.PropertyType );

		HandleColor = Theme.Grey;

		if ( HandleColors.TryGetValue( track.Track.PropertyType, out var color ) )
		{
			HandleColor = color;
		}
	}

	protected override void OnPaint()
	{
		base.OnPaint();

		Paint.SetBrushAndPen( TrackDopesheet.Colors.ChannelBackground );
		Paint.DrawRect( LocalRect );

		PaintCurve();
	}

	private void PaintCurve()
	{
		if ( Decomposer is not { } decomposer || Curve is not { } curve )
		{
			return;
		}

		var scrubBar = Track.TrackList.Editor.ScrubBar;

		var xOffset = scrubBar.ToPixels( 0f );
		var t0 = scrubBar.GetTimeAt( 0f );
		var t1 = scrubBar.GetTimeAt( scrubBar.Width );

		var margin = 2f;
		var height = LocalRect.Height - margin * 2f;

		const int steps = 128;

		var dx = scrubBar.Width / steps;
		var dt = (t1 - t0) / steps;
		var elements = decomposer.Elements;

		if ( elements.Count < 1 ) return;

		if ( Lines is null )
		{
			Lines = new GraphicsLine[elements.Count];

			for ( var i = 0; i < elements.Count; ++i )
			{
				Lines[i] = new CurveLine( this, elements[i].Color );
			}
		}

		Span<float> floats = stackalloc float[elements.Count];

		Span<float> mins = stackalloc float[elements.Count];
		Span<float> maxs = stackalloc float[elements.Count];
		Span<float> mids = stackalloc float[elements.Count];

		for ( var j = 0; j < elements.Count; ++j )
		{
			mins[j] = elements[j].Min ?? float.PositiveInfinity;
			maxs[j] = elements[j].Max ?? float.NegativeInfinity;
		}

		// First pass, find mins and maxs

		for ( var i = 0; i <= steps; ++i )
		{
			var t = t0 + i * dt;

			if ( t < 0f ) continue;

			var value = curve.GetValue( t );
			decomposer.Decompose( value, floats );

			for ( var j = 0; j < elements.Count; ++j )
			{
				mins[j] = Math.Min( mins[j], floats[j] );
				maxs[j] = Math.Max( maxs[j], floats[j] );
			}
		}

		var range = 0f;

		for ( var j = 0; j < elements.Count; ++j )
		{
			range = Math.Max( range, maxs[j] - mins[j] );

			mids[j] = (mins[j] + maxs[j]) * 0.5f;

			Lines[j].Clear();
			Lines[j].Position = new Vector2( -xOffset, margin );
			Lines[j].Size = new Vector2( scrubBar.Width, height );
		}

		var scale = range <= 0f ? 0f : height / range;

		// Second pass, update lines

		for ( var i = 0; i <= steps; ++i )
		{
			var x = i * dx;
			var t = t0 + i * dt;

			if ( t < 0f ) continue;

			var value = curve.GetValue( t );
			decomposer.Decompose( value, floats );

			for ( var j = 0; j < elements.Count; ++j )
			{
				var y = (floats[j] - mids[j]) * scale + 0.5f * height;

				Lines[j].LineTo( new Vector2( x, y ) );
			}
		}
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
	}
}

file class CurveLine : GraphicsLine
{
	public Color Color { get; }

	public CurveLine( GraphicsItem parent, Color color )
		: base( parent )
	{
		Color = color;
	}

	protected override void OnPaint()
	{
		Paint.SetPen( Color.WithAlpha( 0.25f ), 2f );
		PaintLine();
	}
}
