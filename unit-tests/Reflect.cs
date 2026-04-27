using System.Reflection;

namespace unit_tests;

internal static class Reflect {
	private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
	private const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

	public static void SetField(object target, string name, object? value) {
		FieldInfo field = ResolveField(target.GetType(), name, InstanceMembers);
		field.SetValue(target, value);
	}

	public static void SetField(object target, Type declaring, string name, object? value) {
		FieldInfo field = ResolveField(declaring, name, InstanceMembers);
		field.SetValue(target, value);
	}

	public static T GetField<T>(object target, string name) {
		FieldInfo field = ResolveField(target.GetType(), name, InstanceMembers);
		return (T)field.GetValue(target)!;
	}

	public static T GetField<T>(object target, Type declaring, string name) {
		FieldInfo field = ResolveField(declaring, name, InstanceMembers);
		return (T)field.GetValue(target)!;
	}

	public static void SetStatic(Type declaring, string name, object? value) {
		FieldInfo field = ResolveField(declaring, name, StaticMembers);
		field.SetValue(null, value);
	}

	public static T? GetStatic<T>(Type declaring, string name) {
		FieldInfo field = ResolveField(declaring, name, StaticMembers);
		return (T?)field.GetValue(null);
	}

	public static object? Invoke(object target, string name, params object?[] args) {
		MethodInfo method = ResolveMethod(target.GetType(), name, InstanceMembers);
		return method.Invoke(target, args);
	}

	private static FieldInfo ResolveField(Type type, string name, BindingFlags flags) {
		Type? current = type;
		while (current != null) {
			FieldInfo? field = current.GetField(name, flags);
			if (field != null) {
				return field;
			}
			current = current.BaseType;
		}

		throw new ArgumentException($"Could not find field '{name}' on {type}");
	}

	private static MethodInfo ResolveMethod(Type type, string name, BindingFlags flags) {
		Type? current = type;
		while (current != null) {
			MethodInfo? method = current.GetMethod(name, flags);
			if (method != null) {
				return method;
			}
			current = current.BaseType;
		}

		throw new ArgumentException($"Could not find method '{name}' on {type}");
	}
}
