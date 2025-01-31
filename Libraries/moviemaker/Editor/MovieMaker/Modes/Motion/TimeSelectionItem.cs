using Editor.MapEditor;
using Sandbox;

namespace Editor.MovieMaker;

partial class MotionEditMode
{
	private sealed class TimeSelectionItem : GraphicsItem
	{
		public MotionEditMode EditMode { get; }

		public bool HasChanges { get; set; }

		private float _startTime;
		private float _duration;

		private float _fadeInDuration;
		private float _fadeOutDuration;

		private KeyframeInterpolation _fadeInInterpolation = KeyframeInterpolation.QuadraticInOut;
		private KeyframeInterpolation _fadeOutInterpolation = KeyframeInterpolation.QuadraticInOut;

		public KeyframeInterpolation FadeInInterpolation
		{
			get => _fadeInInterpolation;
			set
			{
				_fadeInInterpolation = value;
				UpdatePosition();
			}
		}

		public KeyframeInterpolation FadeOutInterpolation
		{
			get => _fadeOutInterpolation;
			set
			{
				_fadeOutInterpolation = value;
				UpdatePosition();
			}
		}

		public float StartTime
		{
			get => _startTime;
			set
			{
				_startTime = Math.Max( value, 0f );
				UpdatePosition();
			}
		}

		public float Duration
		{
			get => _duration;
			set
			{
				_duration = Math.Max( value, 0f );
				UpdatePosition();
			}
		}

		public float FadeInDuration
		{
			get => _fadeInDuration;
			set
			{
				_fadeInDuration = Math.Max( value, 0f );
				UpdatePosition();
			}
		}

		public float FadeOutDuration
		{
			get => _fadeOutDuration;
			set
			{
				_fadeOutDuration = Math.Max( value, 0f );
				UpdatePosition();
			}
		}

		public Color Color => (HasChanges ? Theme.Yellow : Theme.Blue).WithAlpha( 0.25f );

		public TimeSelectionItem( MotionEditMode editMode )
		{
			EditMode = editMode;

			ZIndex = 10000;

			EditMode.Session.ViewChanged += Session_ViewChanged;
		}

		protected override void OnDestroy()
		{
			EditMode.Session.ViewChanged -= Session_ViewChanged;
		}

		private void Session_ViewChanged()
		{
			UpdatePosition();
		}

		private void UpdatePosition()
		{
			PrepareGeometryChange();

			Position = new Vector2( EditMode.Session.TimeToPixels( StartTime - FadeInDuration ), 0f );
			Size = new Vector2( EditMode.Session.TimeToPixels( Duration + FadeInDuration + FadeOutDuration ), EditMode.DopeSheet.Height );

			Update();

			EditMode.Session.Editor.ScrubBarTop.Update();
			EditMode.Session.Editor.ScrubBarBottom.Update();
		}

		protected override void OnPaint()
		{
			var color = Color;
			var fadeInWidth = FadeInDuration > 0f ? EditMode.Session.TimeToPixels( FadeInDuration ) : 0f;
			var fadeOutWidth = FadeOutDuration > 0f ? EditMode.Session.TimeToPixels( FadeOutDuration ) : 0f;

			Paint.Antialiasing = true;

			Paint.ClearPen();

			if ( FadeInDuration > 0f )
			{
				Paint.SetBrushLinear( new Vector2( 0f, 0f ), new Vector2( fadeInWidth, 0f ), color.WithAlpha( 0.02f ), color );
				Paint.DrawRect( new Rect( 0f, 0f, fadeInWidth, LocalRect.Height ) );
			}

			Paint.SetBrush( color );
			Paint.DrawRect( new Rect( fadeInWidth, 0f, LocalRect.Width - fadeInWidth - fadeOutWidth, LocalRect.Height ) );

			if ( FadeOutDuration > 0f )
			{
				Paint.SetBrushLinear( new Vector2( LocalRect.Width - fadeOutWidth, 0f ), new Vector2( LocalRect.Width, 0f ), color, color.WithAlpha( 0.02f ) );
				Paint.DrawRect( new Rect( LocalRect.Width - fadeOutWidth, 0f, fadeOutWidth, LocalRect.Height ) );
			}

			Paint.ClearBrush();
			Paint.SetPen( Color.Black.WithAlpha( 0.25f ), 0.5f );
			Paint.DrawLine( new Vector2( 0f, 0f ), new Vector2( 0f, LocalRect.Height ) );
			Paint.DrawLine( new Vector2( LocalRect.Width, 0f ), new Vector2( LocalRect.Width, LocalRect.Height ) );

			Paint.SetPen( Color.White.WithAlpha( 0.5f ), 0.5f );
			Paint.DrawLine( new Vector2( fadeInWidth, 0f ), new Vector2( fadeInWidth, LocalRect.Height ) );
			Paint.DrawLine( new Vector2( LocalRect.Width - fadeOutWidth, 0f ), new Vector2( LocalRect.Width - fadeOutWidth, LocalRect.Height ) );
		}

		private List<Vector2> TempPoints { get; } = new();

		public void ScrubberPaint( ScrubberWidget scrubber )
		{
			var x0 = scrubber.ToPixels( StartTime - FadeInDuration );
			var x1 = scrubber.ToPixels( StartTime );
			var x2 = scrubber.ToPixels( StartTime + Duration );
			var x3 = scrubber.ToPixels( StartTime + Duration + FadeOutDuration );

			var y0 = scrubber.IsTop ? scrubber.LocalRect.Bottom : scrubber.LocalRect.Top;
			var y1 = scrubber.IsTop ? scrubber.LocalRect.Top : scrubber.LocalRect.Bottom;

			var points = TempPoints;

			points.Clear();

			AddCurve( points,
				new Vector2( x0, y0 ),
				new Vector2( x1 - x0, y1 - y0 ),
				FadeInInterpolation );

			AddCurve( points,
				new Vector2( x2, y1 ),
				new Vector2( x3 - x2, y0 - y1 ),
				FadeOutInterpolation );

			Paint.SetBrushAndPen( Color );
			Paint.DrawPolygon( points );

			Paint.SetPen( Color.WithAlpha( 0.5f ), 1f );
			Paint.DrawLine( points );

			Paint.SetPen( Color.White.WithAlpha( 0.5f ), 0.5f );
			Paint.DrawLine( new Vector2( x1, 0f ), new Vector2( x1, LocalRect.Height ) );
			Paint.DrawLine( new Vector2( x2, 0f ), new Vector2( x2, LocalRect.Height ) );
		}

		private void AddCurve( List<Vector2> points, Vector2 origin, Vector2 delta, KeyframeInterpolation interpolation )
		{
			const int steps = 16;

			for ( var i = 0; i <= steps; ++i )
			{
				var t = (float)i / steps;
				var x = origin.x + t * delta.x;
				var y = origin.y + interpolation.Apply( t ) * delta.y;

				points.Add( new Vector2( x, y ) );
			}
		}
	}
}
