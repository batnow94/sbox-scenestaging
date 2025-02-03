using System.Collections.Immutable;
using Editor.MapEditor;
using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable

partial class MotionEditMode
{
	public override bool AllowTrackCreation => TimeSelection is not null;

	/// <summary>
	/// Captures the state of a track before it was modified, and records any pending changes.
	/// </summary>
	private sealed class TrackState
	{
		// TODO: we assume block start times / durations don't change

		public MovieTrack Track { get; }

		public IReadOnlyDictionary<int, MovieBlockData> Before { get; }
		public Dictionary<int, MovieBlockData> After { get; } = new();

		private object? _changedValue;

		public object? ChangedValue
		{
			get => _changedValue;
			set
			{
				_changedValue = value;
				HasChanges = true;
			}
		}

		public bool HasChanges { get; private set; }

		public TrackModifier? Modifier { get; }

		public TrackState( MovieTrack track )
		{
			Track = track;

			Before = track.Blocks.ToImmutableDictionary(
				x => x.Id,
				x => x.Data );

			Modifier = TrackModifier.Get( track.PropertyType );
		}

		public bool Update( TimeSelection selection )
		{
			if ( Modifier is not { } modifier )
			{
				return false;
			}

			var changed = false;

			foreach ( var (id, data) in Before )
			{
				if ( Track.GetBlock( id ) is not { } block )
				{
					continue;
				}

				if ( !HasChanges )
				{
					After.Remove( id );
					block.Data = data;
					continue;
				}

				var newData = modifier.Modify( block, data, selection, ChangedValue );

				if ( ReferenceEquals( newData, data ) )
				{
					After.Remove( id );
					block.Data = data;
				}
				else
				{
					After[id] = newData;
					block.Data = newData;
					changed = true;
				}
			}

			return changed;
		}
	}

	private Dictionary<MovieTrack, TrackState> TrackStates { get; } = new();

	private void ClearChanges()
	{
		foreach ( var (track, state) in TrackStates )
		{
			if ( !track.IsValid ) continue;

			foreach ( var (blockId, data) in state.Before )
			{
				if ( track.GetBlock( blockId ) is { } block )
				{
					block.Data = data;
				}
			}
		}

		TrackStates.Clear();

		if ( TimeSelection is { } selection )
		{
			selection.HasChanges = false;
		}
	}

	private void CommitChanges()
	{
		if ( TimeSelection is not { } selection )
		{
			return;
		}

		foreach ( var (track, state) in TrackStates )
		{
			state.Update( selection.Value );
		}

		TrackStates.Clear();
		selection.HasChanges = false;

		Session.Current?.ClipModified();
	}

	protected override bool OnPreChange( DopeSheetTrack track )
	{
		if ( TimeSelection is not { } selection ) return false;
		if ( track.TrackWidget.Property is not { } property )
		{
			return false;
		}

		var movieTrack = track.TrackWidget.MovieTrack;

		if ( TrackStates.ContainsKey( movieTrack ) )
		{
			return false;
		}

		var state = TrackStates[movieTrack] = new TrackState( movieTrack );

		Log.Info( $"Adding change: {movieTrack.FullName}" );

		if ( state.Modifier is null )
		{
			Log.Warning( $"Can't motion edit tracks of type '{movieTrack.PropertyType}'." );
		}

		return true;
	}

	protected override bool OnPostChange( DopeSheetTrack track )
	{
		if ( TimeSelection is not { } selection ) return false;

		var movieTrack = track.TrackWidget.MovieTrack;

		if ( track.TrackWidget.Property is not { } property )
		{
			return false;
		}

		if ( !TrackStates.TryGetValue( movieTrack, out var state ) )
		{
			return false;
		}

		state.ChangedValue = property.Value;
		selection.HasChanges = true;

		return state.Update( selection.Value );
	}

	private void SelectionChanged()
	{
		if ( TimeSelection is not { } selection )
		{
			return;
		}

		foreach ( var (track, state) in TrackStates )
		{
			state.Update( selection.Value );
		}
	}
}
