namespace Editor.MovieMaker;

#nullable enable

public partial class DopeSheetTrack : GraphicsItem
{
	public TrackWidget Track { get; }

	private bool? _canCreatePreview;

	private List<BlockPreview> BlockPreviews { get; } = new();

	public bool Visible => Track.Visible;

	public DopeSheetTrack( TrackWidget track )
	{
		Track = track;
		HoverEvents = true;
	}

	internal void DoLayout( Rect r )
	{
		PrepareGeometryChange();

		Position = new Vector2( 0, r.Top + 1 );
		Size = Visible ? new Vector2( 50000, r.Height ) : 0f;

		UpdateBlockPreviews();

		Track.TrackList.Session.EditMode?.TrackLayout( this, r );
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

	public void UpdateBlockPreviews()
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
}
