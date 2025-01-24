using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable


public class TrackWidget : Widget
{
	public TrackListWidget TrackList { get; }
	public MovieTrack Track { get; }
	internal IMovieProperty? Property { get; }

	public DopesheetTrack? Channel { get; set; }

	RealTimeSince timeSinceInteraction = 1000;

	public TrackWidget( MovieTrack track, TrackListWidget list )
	{
		TrackList = list;
		Track = track;
		FocusMode = FocusMode.TabOrClickOrWheel;
		VerticalSizeMode = SizeMode.CanGrow;

		Layout = Layout.Row();
		Layout.Margin = new Sandbox.UI.Margin( 4, 4, 32, 4 );

		Property = TrackList.Session.Player.GetProperty( Track );

		// Track might not be mapped to any property in the current scene

		if ( Property is null )
		{
			return;
		}

		if ( Property is ISceneReferenceMovieProperty )
		{
			// Add control to retarget a scene reference (Component / GameObject)

			var so = Property.GetSerialized();
			var ctrl = ControlWidget.Create( so.GetProperty( nameof( IMovieProperty.Value ) ) );

			if ( ctrl.IsValid() )
			{
				ctrl.MaximumWidth = 300;
				Layout.Add( ctrl );
			}
		}

		Layout.AddSpacingCell( 16 );
		Layout.Add( new Label( Property.PropertyName ) );
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		Channel?.Destroy();
		Channel = default;
	}

	protected override Vector2 SizeHint()
	{
		return 32;
	}

	protected override void OnPaint()
	{
		var bg = Extensions.PaintSelectColor( TrackDopesheet.Colors.ChannelBackground, TrackDopesheet.Colors.ChannelBackground.Lighten( 0.1f ), Theme.Primary );

		if ( menu.IsValid() && menu.Visible )
			bg = Color.Lerp( TrackDopesheet.Colors.ChannelBackground, Theme.Primary, 0.2f );

		Paint.Antialiasing = false;
		Paint.SetBrushAndPen( bg );
		Paint.DrawRect( new Rect( LocalRect.Left, LocalRect.Top, LocalRect.Width + 100, LocalRect.Height ), 4 );

		if ( timeSinceInteraction < 2.0f )
		{
			var delta = timeSinceInteraction.Relative.Remap( 2.0f, 0, 0, 1 );
			Paint.SetBrush( Theme.Yellow.WithAlpha( delta ) );
			Paint.DrawRect( new Rect( LocalRect.Right - 4, LocalRect.Top, 32, LocalRect.Height ) );
			Update();
		}
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		if ( Channel is null ) return;

		var pos = Channel.GraphicsView.FromScreen( ScreenPosition );

		Channel.DoLayout( new Rect( pos, Size ) );
	}

	internal void AddKey( float currentPointer )
	{
		Channel.AddKey( currentPointer );
	}

	/// <summary>
	/// Write data from this widget to the Clip
	/// </summary>
	public void Write()
	{
		if ( Channel is null ) return;

		Channel.Write();
	}

	internal void AddKey( float time, object value )
	{
		Channel.AddKey( time, value );
	}

	Menu menu;

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		menu = new Menu( this );
		menu.AddOption( "Delete", "delete", RemoveTrack );
		menu.OpenAtCursor();
	}

	void RemoveTrack()
	{
		Track.Remove();
		TrackList.RebuildTracksIfNeeded();
	}

	public void NoteInteraction()
	{
		timeSinceInteraction = 0;
		Update();
	}
}
