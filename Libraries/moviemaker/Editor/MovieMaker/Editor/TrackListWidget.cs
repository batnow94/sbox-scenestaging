using Sandbox.MovieMaker;
using System.Linq;

namespace Editor.MovieMaker;

/// <summary>
/// A split view, with a list of tracks on the left and the dopesheet/curve view on the right
/// </summary>
public partial class TrackListWidget : Widget, EditorEvent.ISceneEdited
{
	public MovieEditor Editor { get; init; }
	public Session Session { get; private set; }

	Layout TrackList;
	TrackDopesheet DopeSheet;

	public Widget LeftWidget { get; init; }
	public Widget RightWidget { get; init; }

	public List<TrackWidget> Tracks = new List<TrackWidget>();

	ScrollArea ScrollArea;

	public TrackListWidget( MovieEditor parent ) : base( parent )
	{
		Session = parent.Session;
		Editor = parent;
		Layout = Layout.Column();

		ScrollArea = new ScrollArea( this );
		var splitter = new Splitter( ScrollArea );

		{
			var left = new Widget( this );
			splitter.AddWidget( left );

			var leftLayout = Layout.Column();
			leftLayout.AddSpacingCell( 24 );

			var trackListWidget = leftLayout.Add( new Widget( this ) );
			trackListWidget.VerticalSizeMode = SizeMode.CanShrink;
			trackListWidget.MinimumWidth = 256;
			trackListWidget.Layout = Layout.Column();
			trackListWidget.Layout.Spacing = 8f;
			trackListWidget.Layout.Margin = new Sandbox.UI.Margin( 16, 0, 0, 16f );

			TrackList = trackListWidget.Layout;

			left.Layout = leftLayout;

			leftLayout.AddStretchCell();
		}

		{
			RightWidget = new Widget( this );
			splitter.AddWidget( RightWidget );

			RightWidget.Layout = Layout.Column();
			DopeSheet = RightWidget.Layout.Add( new TrackDopesheet( this ), 1 );
		}

		splitter.SetCollapsible( 0, false );
		splitter.SetStretch( 0, 1 );
		splitter.SetCollapsible( 1, false );
		splitter.SetStretch( 1, 3 );

		ScrollArea.Canvas = splitter;
		Layout.Add( ScrollArea );

		MouseTracking = true;

		Load( Session.Clip );
	}

	void ScrubToTime( float time )
	{
		Session.Player.Position = time;
	}

	private void Load( MovieClip clip )
	{
		if ( clip is null )
			return;

		RebuildTracks();
	}

	/// <summary>
	/// Called when a track was added or removed
	/// </summary>
	void RebuildTracks()
	{
		TrackList.Clear( true );
		Tracks.Clear();

		var groups = new Dictionary<MovieTrack, TrackGroup>();

		foreach ( var track in Session.Clip.AllTracks )
		{
			var editorTrack = AddTrack( track );

			var parentGroup = track.Parent is null ? null : groups.GetValueOrDefault( track.Parent );

			if ( track.Children.Count == 0 )
			{
				(parentGroup?.Content ?? TrackList).Add( editorTrack );
				continue;
			}

			var group = new TrackGroup( editorTrack );

			groups[track] = group;

			(parentGroup?.Content ?? TrackList).Add( group );
		}

		DopeSheet.UpdateTracks();
	}

	private static MovieTrack GetRoot( MovieTrack track )
	{
		while ( track.Parent is { } parent )
		{
			track = parent;
		}

		return track;
	}

	/// <summary>
	/// Check tracks hash, rebuild if needed
	/// </summary>
	public void RebuildTracksIfNeeded()
	{
		if ( Tracks.Count == Session.Clip.TrackCount )
			return;

		RebuildTracks();
	}

	TrackWidget FindTrack( MovieTrack track )
	{
		return Tracks.FirstOrDefault( x => x.Track == track );
	}

	public TrackWidget AddTrack( MovieTrack track )
	{
		var trackWidget = new TrackWidget( track, this );

		Tracks.Add( trackWidget );

		return trackWidget;
	}

	protected override void OnVisibilityChanged( bool visible )
	{
		base.OnVisibilityChanged( visible );

		if ( visible )
		{
			DopeSheet?.UpdateTracks();
		}
	}

	public float MinTimeVisible => 5;
	public float MaxTimeVisible => 120;

	internal bool OnCanvasWheel( WheelEvent e )
	{
		// scoll
		if ( e.HasShift )
		{
			Session.ScrollBy( -e.Delta / 10.0f * (Session.TimeVisible / 10.0f), true );
			DopeSheet?.UpdateTracks();
			return true;
		}

		// zoom
		if ( e.HasCtrl )
		{
			Session.Zoom( e.Delta / 10.0f );
			Update();
			DopeSheet?.UpdateTracks();
			return true;
		}

		return false;
	}

	public void ScrollBy( float x )
	{
		Session.ScrollBy( x, false );
		Update();
		DopeSheet?.UpdateTracks();
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;

		Paint.SetBrushAndPen( TrackDopesheet.Colors.Background );
		Paint.DrawRect( LocalRect );
	}

	/// <summary>
	/// Write all tracks from the editor to the clip
	/// </summary>
	public void WriteTracks()
	{
		foreach ( var t in Tracks )
		{
			t.Write();
		}
	}

	public void OnCopy()
	{
		DopeSheet?.OnCopy();
	}

	public void OnPaste()
	{
		DopeSheet?.OnPaste();
	}


	public void OnDelete()
	{
		DopeSheet?.OnDelete();
	}
}

