using System.Reflection;

namespace Editor.MovieMaker;

#nullable enable

public record struct VectorElementDisplay( string Name, Color Color, float? Min = null, float? Max = null );

public interface IVectorDecomposer
{
	IReadOnlyList<VectorElementDisplay> Elements { get; }
	void Decompose( object vector, Span<float> result );
}

public interface IVectorDecomposer<in T>
	where T : struct
{
	IReadOnlyList<VectorElementDisplay> Elements { get; }

	void Decompose( T vector, Span<float> result );
}

public static class VectorDecomposer
{
	public static IVectorDecomposer<T>? GetDefault<T>()
		where T : struct
	{
		return DefaultVectorDecomposer.Instance as IVectorDecomposer<T>;
	}

	[SkipHotload]
	private static Dictionary<Type, IVectorDecomposer?> CachedWrappers { get; } = new();

	private static MethodInfo GetWrappedMethod { get; } =
		typeof(VectorDecomposer).GetMethod( nameof(GetWrapped), BindingFlags.Static | BindingFlags.NonPublic )!;

	public static IVectorDecomposer? GetDefault( Type vectorType )
	{
		if ( CachedWrappers.TryGetValue( vectorType, out var cached ) ) return cached;

		try
		{
			return CachedWrappers[vectorType] = (IVectorDecomposer?)GetWrappedMethod.MakeGenericMethod( vectorType ).Invoke( null, [] );
		}
		catch
		{
			return CachedWrappers[vectorType] = null;
		}
	}

	private static IVectorDecomposer? GetWrapped<T>()
		where T : struct
	{
		return GetDefault<T>() is { } implementation
			? new VectorDecomposerWrapper<T>( implementation ) : null;
	}

	[EditorEvent.Hotload]
	private static void OnHotload()
	{
		CachedWrappers.Clear();
	}
}

file record VectorDecomposerWrapper<T>( IVectorDecomposer<T> Implementation ) : IVectorDecomposer
	where T : struct
{
	public IReadOnlyList<VectorElementDisplay> Elements => Implementation.Elements;

	public void Decompose( object vector, Span<float> result ) => Implementation.Decompose( (T)vector, result );
}

file class DefaultVectorDecomposer :
	IVectorDecomposer<bool>,
	IVectorDecomposer<float>, IVectorDecomposer<Vector2>, IVectorDecomposer<Vector3>, IVectorDecomposer<Vector4>,
	IVectorDecomposer<Rotation>, IVectorDecomposer<Angles>,
	IVectorDecomposer<Color>
{
	public static DefaultVectorDecomposer Instance { get; } = new();

	private static VectorElementDisplay[] DefaultBoolElements { get; } =
	[
		new( "X", Color.White, 0f, 1f )
	];

	private static VectorElementDisplay[] DefaultFloatElements { get; } =
	[
		new( "X", Color.White )
	];

	private static VectorElementDisplay[] DefaultVectorElements { get; } =
	[
		new( "X", Theme.Red ),
		new( "Y", Theme.Green ),
		new( "Z", Theme.Blue ),
		new( "W", Theme.White )
	];

	private static VectorElementDisplay[] DefaultRotationElements { get; } =
	[
		new( "X", Theme.Red, -1f, 1f ),
		new( "Y", Theme.Green, -1f, 1f ),
		new( "Z", Theme.Blue, -1f, 1f ),
		new( "W", Theme.White, -1f, 1f )
	];

	private static VectorElementDisplay[] DefaultAngleElements { get; } =
	[
		new( "P", Theme.Red, -180f, 180f ),
		new( "Y", Theme.Green, -180f, 180f ),
		new( "R", Theme.Blue, -180f, 180f )
	];

	private static VectorElementDisplay[] DefaultColorElements { get; } =
	[
		new( "R", Color.Red, 0f, 1f ),
		new( "G", Color.Green, 0f, 1f ),
		new( "B", Color.Blue, 0f, 1f ),
		new( "A", Color.White, 0f, 1f )
	];

	IReadOnlyList<VectorElementDisplay> IVectorDecomposer<bool>.Elements => DefaultBoolElements;
	IReadOnlyList<VectorElementDisplay> IVectorDecomposer<float>.Elements => DefaultFloatElements;
	IReadOnlyList<VectorElementDisplay> IVectorDecomposer<Vector2>.Elements => DefaultVectorElements[..2];
	IReadOnlyList<VectorElementDisplay> IVectorDecomposer<Vector3>.Elements => DefaultVectorElements[..3];
	IReadOnlyList<VectorElementDisplay> IVectorDecomposer<Vector4>.Elements => DefaultVectorElements[..4];
	IReadOnlyList<VectorElementDisplay> IVectorDecomposer<Rotation>.Elements => DefaultRotationElements;
	IReadOnlyList<VectorElementDisplay> IVectorDecomposer<Angles>.Elements => DefaultAngleElements;
	IReadOnlyList<VectorElementDisplay> IVectorDecomposer<Color>.Elements => DefaultColorElements;

	public void Decompose( bool vector, Span<float> result )
	{
		result[0] = vector ? 1f : 0f;
	}

	public void Decompose( float vector, Span<float> result )
	{
		result[0] = vector;
	}

	public void Decompose( Vector2 vector, Span<float> result )
	{
		result[0] = vector.x;
		result[1] = vector.y;
	}

	public void Decompose( Vector3 vector, Span<float> result )
	{
		result[0] = vector.x;
		result[1] = vector.y;
		result[2] = vector.z;
	}

	public void Decompose( Vector4 vector, Span<float> result )
	{
		result[0] = vector.x;
		result[1] = vector.y;
		result[2] = vector.z;
		result[3] = vector.w;
	}

	public void Decompose( Rotation vector, Span<float> result )
	{
		// Decompose it as the forward vector + how much the right vector is pointing up,
		// because that looks nice and smooth

		var forward = vector.Forward;
		var right = vector.Right;

		Decompose( forward, result[..3] );

		result[3] = right.z;
	}

	public void Decompose( Angles vector, Span<float> result )
	{
		result[0] = vector.pitch;
		result[1] = vector.yaw;
		result[2] = vector.roll;
	}

	public void Decompose( Color vector, Span<float> result )
	{
		result[0] = vector.r;
		result[1] = vector.g;
		result[2] = vector.b;
		result[3] = vector.a;
	}
}
