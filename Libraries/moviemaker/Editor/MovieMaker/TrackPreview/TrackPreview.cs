using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable

public abstract partial class TrackPreview : GraphicsItem
{
	public MovieTrack Track { get; private set; } = null!;
	public new DopesheetTrack Parent { get; private set; } = null!;

	public record struct PaintContext( Rect LocalRect, float MinTime, float MaxTime );

	private void Initialize( DopesheetTrack parent, MovieTrack track )
	{
		base.Parent = Parent = parent;
		Track = track;
	}

	protected override void OnPaint()
	{
		var scrubBar = Parent.Track.TrackList.Editor.ScrubBar;
		var t0 = Math.Max( 0f, scrubBar.GetTimeAt( 0f ) );
		var t1 = scrubBar.GetTimeAt( scrubBar.Width );

		var x0 = scrubBar.ToPixels( t0 );
		var x1 = scrubBar.ToPixels( t1 );

		OnPaint( new PaintContext( new Rect( x0, LocalRect.Top, x1 - x0, LocalRect.Height ), t0, t1 ) );
	}

	protected abstract void OnPaint( PaintContext context );
}

internal interface ITrackPreview<T>;

public abstract class TrackPreview<T> : TrackPreview, ITrackPreview<T>
{
	public KeyframeCurve<T>? Curve => Parent.Curve as KeyframeCurve<T>;
}
