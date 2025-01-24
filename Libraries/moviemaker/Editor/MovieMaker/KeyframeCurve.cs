using System.Collections;
using System.Linq;
using System.Text.Json.Serialization;
using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable

public interface IKeyframe
{
	float Time { get; }
	object? Value { get; }
	KeyframeInterpolation? Interpolation { get; }
}

public record struct Keyframe<T>(
	float Time, T Value,
	[property: JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
	KeyframeInterpolation? Interpolation ) : IKeyframe
{
	object? IKeyframe.Value => Value;
}

public abstract partial class KeyframeCurve : IEnumerable<IKeyframe>
{
	public abstract Type ValueType { get; }
	public abstract int Count { get; }
	public abstract float Duration { get; }
	public abstract bool CanInterpolate { get; }

	public static KeyframeCurve Create( Type valueType )
	{
		var typeDesc = EditorTypeLibrary.GetType( typeof(KeyframeCurve<>) ).MakeGenericType( [valueType] );

		return EditorTypeLibrary.Create<KeyframeCurve>( typeDesc );
	}

	public abstract void Clear();

	public abstract void SetKeyframe( float time, object? value, KeyframeInterpolation? interpolation = null );

	protected abstract IEnumerator<IKeyframe> OnGetEnumerator();

	public IEnumerator<IKeyframe> GetEnumerator() => OnGetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => OnGetEnumerator();
}

public partial class KeyframeCurve<T> : KeyframeCurve, IEnumerable<Keyframe<T>>
{
	private readonly SortedList<float, Keyframe<T>> _keyframes = new();
	private readonly IInterpolator<T>? _interpolator = Interpolator.GetDefault<T>();

	public override Type ValueType => typeof( T );
	public override int Count => _keyframes.Count;
	public override float Duration => _keyframes.Count == 0 ? 0f : _keyframes.Values[^1].Time;

	public KeyframeInterpolation Interpolation { get; set; }
	public override bool CanInterpolate => _interpolator is not null;

	public KeyframeCurve()
	{
		Interpolation = _interpolator is not null
			? KeyframeInterpolation.QuadraticInOut
			: KeyframeInterpolation.None;
	}

	public Keyframe<T> this[ int index ]
	{
		get => _keyframes.Values[index];
		set => _keyframes.Values[index] = value;
	}

	public override void SetKeyframe( float time, object? value, KeyframeInterpolation? interpolation = null ) =>
		SetKeyframe( new Keyframe<T>( time, (T)value!, interpolation ) );

	public void SetKeyframe( float time, T value, KeyframeInterpolation? interpolation = null ) =>
		SetKeyframe( new Keyframe<T>( time, value, interpolation ) );

	public void SetKeyframe( Keyframe<T> keyframe )
	{
		_keyframes[keyframe.Time] = keyframe;
	}

	public Keyframe<T> GetKeyframe( float time )
	{
		return _keyframes[time];
	}

	public void RemoveKeyframe( float time )
	{
		_keyframes.Remove( time );
	}

	public override void Clear()
	{
		_keyframes.Clear();
	}

	protected override IEnumerator<IKeyframe> OnGetEnumerator()
	{
		return _keyframes.Values.Cast<IKeyframe>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public new IEnumerator<Keyframe<T>> GetEnumerator() => _keyframes.Values.GetEnumerator();
}
