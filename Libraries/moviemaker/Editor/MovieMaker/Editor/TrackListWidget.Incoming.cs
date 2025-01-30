using Sandbox.Diagnostics;
using System.Linq;
using Sandbox.MovieMaker;

namespace Editor.MovieMaker;


public partial class TrackListWidget : EditorEvent.ISceneEdited
{
	void EditorEvent.ISceneEdited.GameObjectPreEdited( GameObject go, string propertyName )
	{
		if ( !CanRecord( typeof( GameObject ), ref propertyName ) ) return;

		try
		{
			var targetTrack = Session.Player.GetOrCreateTrack( go, propertyName );

			// make sure the track widget exists for this track
			RebuildTracksIfNeeded();

			PreChange( targetTrack );
		}
		catch
		{
			// Not an editable track
		}
	}

	void EditorEvent.ISceneEdited.ComponentPreEdited( Component cmp, string propertyName )
	{
		if ( !CanRecord( cmp.GetType(), ref propertyName ) ) return;

		try
		{
			var targetTrack = Session.Player.GetOrCreateTrack( cmp, propertyName );

			// make sure the track widget exists for this track
			RebuildTracksIfNeeded();

			PreChange( targetTrack );
		}
		catch
		{
			// Not an editable track
		}
	}

	void EditorEvent.ISceneEdited.GameObjectEdited( GameObject go, string propertyName )
	{
		if ( !CanRecord( typeof(GameObject), ref propertyName ) ) return;

		if ( Session.Player.GetTrack( go, propertyName ) is not { } targetTrack ) return;

		PostChange( targetTrack );
	}

	void EditorEvent.ISceneEdited.ComponentEdited( Component cmp, string propertyName )
	{
		if ( !CanRecord( cmp.GetType(), ref propertyName ) ) return;
		if ( Session.Player.GetTrack( cmp, propertyName ) is not { } targetTrack ) return;

		PostChange( targetTrack );
	}

	private string NormalizeGameObjectProperty( string propertyName )
	{
		const string transformPrefix = "Transform.";

		if ( propertyName.StartsWith( transformPrefix, StringComparison.Ordinal ) )
		{
			return propertyName[transformPrefix.Length..];
		}

		return propertyName;
	}

	private bool CanRecord( Type targetType, ref string propertyName )
	{
		if ( targetType == typeof(GameObject) )
		{
			propertyName = NormalizeGameObjectProperty( propertyName );
		}

		if ( propertyName[^2..] is ".x" or ".y" or ".z" or ".w" )
		{
			propertyName = propertyName[..^2];
		}

		if ( targetType == typeof(GameObject))
		{
			if ( propertyName is not ("LocalScale" or "LocalRotation" or "LocalPosition") )
			{
				return false;
			}
		}

		return true;
	}

	private bool PreChange( MovieTrack track )
	{
		if ( Session.EditMode?.PreChange( track ) is true )
		{
			NoteInteraction( track );
			return true;
		}

		return true;
	}

	private bool PostChange( MovieTrack track )
	{
		if ( Session.EditMode?.PostChange( track ) is true )
		{
			NoteInteraction( track );
			return true;
		}

		return true;
	}

	private void NoteInteraction( MovieTrack track )
	{
		var trackWidget = FindTrack( track );

		Assert.NotNull( trackWidget, "Track should have been created" );

		trackWidget.NoteInteraction();

		ScrollArea.MakeVisible( trackWidget );
	}
}
