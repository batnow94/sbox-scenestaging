using System.Linq;
using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable

public partial class DopesheetTrack : GraphicsItem
{
	public TrackWidget Track { get; }

	private bool? _canCreatePreview;

	private List<BlockPreview> BlockPreviews { get; } = new();
	public KeyframeCurve? Curve { get; private set; }

	public bool Visible => Track.Visible;

	IEnumerable<DopeHandle> Handles => Children.OfType<DopeHandle>();

	public Color HandleColor { get; private set; }

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

		HandleColor = Theme.Grey;

		if ( HandleColors.TryGetValue( track.Track.PropertyType, out var color ) )
		{
			HandleColor = color;
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
		PrepareGeometryChange();

		Position = new Vector2( 0, r.Top + 1 );
		Size = Visible ? new Vector2( 50000, r.Height ) : 0f;

		UpdateBlockPreviews();
		PositionHandles();
	}

	private void ClearBlockPreviews()
	{
		if ( BlockPreviews.Count == 0 ) return;

		foreach ( var blockPreview in BlockPreviews )
		{
			blockPreview.Destroy();
		}

		BlockPreviews.Clear();
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

	private void UpdateBlockPreviews()
	{
		if ( Visible && _canCreatePreview is not false )
		{
			if ( Track.Track.Blocks.Count != BlockPreviews.Count )
			{
				ClearBlockPreviews();
			}

			var blocks = Track.Track.Blocks;

			for ( var i = 0; i < blocks.Count; ++i )
			{
				var block = blocks[i];

				if ( BlockPreviews.Count <= i )
				{
					if ( BlockPreview.Create( this, block ) is not { } newPreview )
					{
						_canCreatePreview = false;
						return;
					}

					BlockPreviews.Add( newPreview );
				}

				var preview = BlockPreviews[i];
				var duration = block.Duration ?? Track.Track.Clip.Duration - block.StartTime;

				preview.Block = block;
				preview.PrepareGeometryChange();
				preview.Position = new Vector2( Session.Current.TimeToPixels( block.StartTime ), 0f );
				preview.Size = new Vector2( Session.Current.TimeToPixels( duration ), LocalRect.Height );
			}
		}
		else
		{
			ClearBlockPreviews();
		}
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

		Session.Current.ClipModified();

		UpdateBlockPreviews();
	}
}
