namespace Editor.MovieMaker;

#nullable enable

[Title( "Motion Editor"), Icon( "brush" ), Order( 1 )]
internal sealed partial class MotionEditMode : EditMode
{
	private TimeSelectionItem? TimeSelection { get; set; }
	public KeyframeInterpolation DefaultInterpolation { get; private set; } = KeyframeInterpolation.QuadraticInOut;

	private float? _selectionStartTime;

	protected override void OnEnable()
	{
		Toolbar.AddSpacingCell();

		foreach ( var interpolation in Enum.GetValues<KeyframeInterpolation>() )
		{
			Toolbar.AddToggle( interpolation,
				() => (TimeSelection?.FadeInInterpolation ?? DefaultInterpolation) == interpolation,
				_ =>
				{
					DefaultInterpolation = interpolation;

					if ( TimeSelection is { } timeSelection )
					{
						timeSelection.FadeInInterpolation = interpolation;
						timeSelection.FadeOutInterpolation = interpolation;
					}
				} );
		}
	}

	protected override void OnDisable()
	{
		TimeSelection?.Destroy();
		TimeSelection = null;
	}

	[Shortcut( "motion-edit.back", "ESC" )]
	private void Back()
	{
		if ( TimeSelection is not null )
		{
			TimeSelection.Destroy();
			TimeSelection = null;
		}
	}

	protected override void OnMousePress( MouseEvent e )
	{
		if ( e.LeftMouseButton && e.HasShift && Session.PreviewPointer is { } time )
		{
			if ( TimeSelection is null )
			{
				TimeSelection = new TimeSelectionItem( this );
				DopeSheet.Add( TimeSelection );
			}

			TimeSelection.Duration = TimeSelection.FadeInDuration = TimeSelection.FadeOutDuration = 0f;
			TimeSelection.StartTime = time;
			TimeSelection.FadeInInterpolation = DefaultInterpolation;
			TimeSelection.FadeOutInterpolation = DefaultInterpolation;

			_selectionStartTime = time;

			e.Accepted = true;
			return;
		}
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		if ( (e.ButtonState & MouseButtons.Left) != 0 && e.HasShift
			&& TimeSelection is { } selection
			&& Session.PreviewPointer is { } time
			&& _selectionStartTime is { } dragStartTime )
		{
			var start = Math.Min( time, dragStartTime );
			var end = Math.Max( time, dragStartTime );

			selection.StartTime = start;
			selection.Duration = end - start;
		}
	}

	protected override void OnMouseRelease( MouseEvent e )
	{
		if ( _selectionStartTime is { } dragStartTIme && TimeSelection is { } selection )
		{
			_selectionStartTime = null;

			var midTime = selection.StartTime + selection.Duration * 0.5f;

			Session.SetCurrentPointer( midTime );
			Session.ClearPreviewPointer();
		}
	}

	protected override void OnMouseWheel( WheelEvent e )
	{
		if ( TimeSelection is { } selection && e.HasShift )
		{
			var delta = Math.Sign( e.Delta ) * 0.1f;

			selection.FadeInDuration += delta;
			selection.FadeOutDuration += delta;
		}
	}

	protected override void OnScrubberPaint( ScrubberWidget scrubber )
	{
		TimeSelection?.ScrubberPaint( scrubber );
	}
}
