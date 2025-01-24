using System;
using System.Text.Json.Serialization;

namespace Sandbox.MovieMaker;

#nullable enable

partial class MoviePlayer
{
	internal record struct MappingModel( Guid TrackId,
		[property: JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
		Guid? GameObject = null,
		[property: JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
		Guid? Component = null );

	[Property, Hide]
	internal IReadOnlyList<MappingModel> Mapping
	{
		get => _sceneRefMap
			.Select( x => x.Value.Component is { } comp
				? new MappingModel( x.Key, Component: comp.Id )
				: new MappingModel( x.Key, x.Value.GameObject.Id ) )
			.ToArray();

		set
		{
			_sceneRefMap.Clear();
			_memberMap.Clear();

			foreach ( var mapping in value )
			{
				if ( mapping.GameObject is { } goId && Scene.Directory.FindByGuid( goId ) is { } go )
				{
					_sceneRefMap.Add( mapping.TrackId, MovieProperty.FromGameObject( go ) );
				}

				if ( mapping.Component is { } cmpId && Scene.Directory.FindComponentByGuid( cmpId ) is { } cmp )
				{
					_sceneRefMap.Add( mapping.TrackId, MovieProperty.FromComponent( cmp ) );
				}
			}
		}
	}
}
