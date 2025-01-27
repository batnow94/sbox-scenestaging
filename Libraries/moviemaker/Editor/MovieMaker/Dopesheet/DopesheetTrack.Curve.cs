using System.Linq;

namespace Editor.MovieMaker;

#nullable enable

partial class DopesheetTrack
{
	private GraphicsLine[]? Lines { get; set; }

	[field: ThreadStatic]
	private static List<float>? PaintCurve_Times { get; set; }

	private void PaintCurve()
	{
		if ( Decomposer is not { } decomposer || Curve is not { } curve )
		{
			return;
		}

		var scrubBar = Track.TrackList.Editor.ScrubBar;

		var xOffset = scrubBar.ToPixels( 0f );
		var t0 = Math.Max( 0f, scrubBar.GetTimeAt( 0f ) );
		var t1 = scrubBar.GetTimeAt( scrubBar.Width );

		var times = PaintCurve_Times ??= new();

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

		foreach ( var t in times )
		{
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

		foreach ( var t in times )
		{
			var value = curve.GetValue( t );
			var x = scrubBar.ToPixels( t );

			decomposer.Decompose( value, floats );

			for ( var j = 0; j < elements.Count; ++j )
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
