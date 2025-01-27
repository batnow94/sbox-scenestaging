using Editor.MovieMaker;

namespace Editor.TrackPainter;

#nullable enable

partial class VectorPreview<T>
{
	[field: ThreadStatic]
	private static List<float>? PaintCurve_Times { get; set; }

	private GraphicsLine[]? Lines { get; set; }


	protected override void OnPaint( PaintContext context )
	{
		if ( Curve is not { } curve ) return;

		var times = PaintCurve_Times ??= new();

		var t0 = context.MinTime;
		var t1 = context.MaxTime;

		times.Clear();
		times.Add( t0 );

		var lastTime = t0;
		var maxDt = (t1 - t0) / 128f;

		const float epsilon = 0.0001f;

		foreach ( var keyframe in curve )
		{
			if ( keyframe.Time > t0 && keyframe.Time < t1 )
			{
				var interpolation = keyframe.Interpolation ?? curve.Interpolation;

				if ( interpolation is KeyframeInterpolation.None && lastTime < keyframe.Time - epsilon )
				{
					times.Add( keyframe.Time - epsilon );
				}

				times.Add( keyframe.Time );

				lastTime = keyframe.Time;
			}
		}

		times.Add( t1 );
		times.Sort();

		const int safetyLimit = 2048;

		for ( var i = 1; i < times.Count && times.Count < safetyLimit; ++i )
		{
			var prev = times[i - 1];
			var next = times[i];

			if ( next - prev > maxDt )
			{
				times.Insert( i--, (next + prev) * 0.5f );
			}
		}

		var margin = 2f;
		var height = LocalRect.Height - margin * 2f;

		if ( Elements.Count < 1 ) return;

		if ( Lines is null )
		{
			Lines = new GraphicsLine[Elements.Count];

			for ( var i = 0; i < Elements.Count; ++i )
			{
				Lines[i] = new CurveLine( this, Elements[i].Color );
			}
		}

		Span<float> floats = stackalloc float[Elements.Count];

		Span<float> mins = stackalloc float[Elements.Count];
		Span<float> maxs = stackalloc float[Elements.Count];
		Span<float> mids = stackalloc float[Elements.Count];

		for ( var j = 0; j < Elements.Count; ++j )
		{
			mins[j] = Elements[j].Min ?? float.PositiveInfinity;
			maxs[j] = Elements[j].Max ?? float.NegativeInfinity;
		}

		// First pass, find mins and maxs

		foreach ( var t in times )
		{
			if ( t < 0f ) continue;

			var value = curve.GetValue( t );
			Decompose( value, floats );

			for ( var j = 0; j < Elements.Count; ++j )
			{
				mins[j] = Math.Min( mins[j], floats[j] );
				maxs[j] = Math.Max( maxs[j], floats[j] );
			}
		}

		var range = 0f;

		for ( var j = 0; j < Elements.Count; ++j )
		{
			range = Math.Max( range, maxs[j] - mins[j] );

			mids[j] = (mins[j] + maxs[j]) * 0.5f;

			Lines[j].Clear();
			Lines[j].Position = new Vector2( 0f, margin );
			Lines[j].Size = new Vector2( context.LocalRect.Width, height );
		}

		var scale = range <= 0f ? 0f : height / range;

		// Second pass, update lines

		var dxdt = context.LocalRect.Width / (t1 - t0);

		foreach ( var t in times )
		{
			var value = curve.GetValue( t );
			var x = LocalRect.Left + (t - t0) * dxdt;

			Decompose( value, floats );

			for ( var j = 0; j < Elements.Count; ++j )
			{
				var y = (mids[j] - floats[j]) * scale + 0.5f * height;

				if ( t <= 0f )
				{
					Lines[j].MoveTo( new Vector2( x, y ) );
				}
				else
				{
					Lines[j].LineTo( new Vector2( x, y ) );
				}
			}
		}
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
