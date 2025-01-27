using Sandbox.Diagnostics;
using System.Linq;

namespace Editor.MovieMaker;


public partial class TrackListWidget : EditorEvent.ISceneEdited
{
	void EditorEvent.ISceneEdited.GameObjectEdited( GameObject go, string propertyName )
	{
		if ( propertyName.StartsWith( "Transform." ) ) propertyName = propertyName.Replace( "Transform.", "" ).Split( '.' )[0];

		if ( propertyName == "LocalScale" || propertyName == "LocalRotation" || propertyName == "LocalPosition" )
		{
			// make sure the track exists for this property
			var targetTrack = Session.KeyframeRecording
				? Session.Player.GetOrCreateTrack( go, propertyName )
				: Session.Player.GetTrack( go, propertyName );

			if ( targetTrack is null && !Session.KeyframeRecording )
				return;

			// make sure the track widget exists for this track
			RebuildTracksIfNeeded();

			var trackwidget = FindTrack( targetTrack );
			Assert.NotNull( trackwidget, "Track should have been created" );
			trackwidget.AddKey( Session.CurrentPointer );
			trackwidget.Write();

			ScrollArea.MakeVisible( trackwidget );
			trackwidget.NoteInteraction();
		}
	}

	void EditorEvent.ISceneEdited.ComponentEdited( Component cmp, string propertyName )
	{
		propertyName = propertyName.Split( '.' ).First();

		var targetTrack = Session.KeyframeRecording
			? Session.Player.GetOrCreateTrack( cmp, propertyName )
			: Session.Player.GetTrack( cmp, propertyName );

		if ( targetTrack is null && !Session.KeyframeRecording )
			return;

		// make sure the track widget exists for this track
		RebuildTracksIfNeeded();

		var trackwidget = FindTrack( targetTrack );
		Assert.NotNull( trackwidget, "Track should have been created" );
		trackwidget.AddKey( Session.CurrentPointer );
		trackwidget.Write();

		ScrollArea.MakeVisible( trackwidget );
		trackwidget.NoteInteraction();
	}
}

