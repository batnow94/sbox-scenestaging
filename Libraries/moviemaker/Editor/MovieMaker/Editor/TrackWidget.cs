﻿using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable

public class TrackWidget : Widget
{
	public TrackListWidget TrackList { get; }
	public MovieTrack Track { get; }
	internal IMovieProperty? Property { get; }

	public DopeSheetTrack? Channel { get; set; }

	public Layout Buttons { get; }

	RealTimeSince timeSinceInteraction = 1000;

	public TrackWidget( MovieTrack track, TrackListWidget list )
	{
		TrackList = list;
		Track = track;
		FocusMode = FocusMode.TabOrClickOrWheel;
		VerticalSizeMode = SizeMode.CanGrow;

		Layout = Layout.Row();
		Layout.Spacing = 12f;
		Layout.Margin = 4f;

		Buttons = Layout.AddRow();
		Buttons.Spacing = 2f;
		Buttons.Margin = 2f;

		Property = TrackList.Session.Player.GetOrAutoResolveProperty( Track );

		// Track might not be mapped to any property in the current scene

		if ( Property is null )
		{
			return;
		}

		Layout.Add( new Label( Property.PropertyName ) );

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
	}

	protected override void OnMoved()
	{
		UpdateChannelPosition();
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		Channel?.Destroy();
		Channel = default;
	}

	protected override void OnMouseEnter()
	{
		base.OnMouseEnter();

		if ( Parent is TrackGroup ) Parent.Update();
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();

		if ( Parent is TrackGroup ) Parent.Update();
	}

	protected override void OnFocus( FocusChangeReason reason )
	{
		base.OnFocus( reason );

		if ( Parent is TrackGroup ) Parent.Update();
	}

	protected override void OnBlur( FocusChangeReason reason )
	{
		base.OnBlur( reason );

		if ( Parent is TrackGroup ) Parent.Update();
	}

	protected override Vector2 SizeHint()
	{
		return 32;
	}

	public Color BackgroundColor
	{
		get
		{
			var defaultColor = DopeSheet.Colors.ChannelBackground;
			var hoveredColor = DopeSheet.Colors.ChannelBackground.Lighten( 0.1f );
			var selectedColor = Color.Lerp( defaultColor, Theme.Primary, 0.5f );

			var isHovered = IsUnderMouse;
			var isSelected = IsFocused || menu.IsValid() && menu.Visible;

			return isSelected ? selectedColor
				: isHovered ? hoveredColor
					: defaultColor;
		}
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = false;
		Paint.SetBrushAndPen( BackgroundColor );
		Paint.DrawRect( new Rect( LocalRect.Left, LocalRect.Top, LocalRect.Width, LocalRect.Height ), 4 );

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

		UpdateChannelPosition();
	}

	public void UpdateChannelPosition()
	{
		if ( Channel is null ) return;

		var pos = Channel.GraphicsView.FromScreen( ScreenPosition );

		Channel.DoLayout( new Rect( pos, Size ) );
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

		Session.Current?.ClipModified();
	}

	public void NoteInteraction()
	{
		timeSinceInteraction = 0;
		Update();
	}
}
