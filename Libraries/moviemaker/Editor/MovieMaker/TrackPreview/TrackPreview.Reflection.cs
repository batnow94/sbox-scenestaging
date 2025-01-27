using System.Linq;
using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable

partial class TrackPreview
{
	public static TrackPreview? Create( DopesheetTrack parent, MovieTrack track )
	{
		foreach ( var typeDesc in EditorTypeLibrary.GetTypes<TrackPreview>() )
		{
			var type = typeDesc.TargetType;

			if ( type.IsAbstract ) continue;
			if ( type.IsGenericType ) continue; // TODO?

			if ( !SupportsPropertyType( type, track.PropertyType ) ) continue;

			try
			{
				var inst = (TrackPreview)Activator.CreateInstance( type )!;

				inst.Initialize( parent, track );

				return inst;
			}
			catch
			{
				continue;
			}
		}

		return null;
	}

	private static bool SupportsPropertyType( Type trackPreviewType, Type propertyType )
	{
		return trackPreviewType.GetInterfaces()
			.Where( x => x.IsConstructedGenericType )
			.Where( x => x.GetGenericTypeDefinition() == typeof(ITrackPreview<>) )
			.Any( x => x.GetGenericArguments()[0].IsAssignableFrom( propertyType ) );
	}
}
