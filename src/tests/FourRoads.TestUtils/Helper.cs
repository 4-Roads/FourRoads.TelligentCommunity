using System;
using System.Reflection;

namespace FourRoads.General.TestUtils
{
	/// <summary>
	/// Helper class to assist unit testing private or protected class methods
	/// </summary>
	public class Helper
	{
		private Helper()
		{
		}

		/// <summary>
		/// Invokes a static method of a given type
		/// </summary>
		/// <param name="type">The Type the method will be invoked from </param>
		/// <param name="method">The name of the method to be run</param>
		/// <param name="args">The method arguments</param>
		/// <returns>The output of the specified method</returns>
		public static object InvokeStaticMethod(Type type, string method, ref object[] args)
		{
			BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

			return InvokeMethod(type, method, null, ref args, flags);
		}

		/// <summary>
		/// Invokes an instance method of a given type
		/// </summary>
		/// <param name="instance">The class instance the method will be invoked from </param>
		/// <param name="method">The name of the method to be run</param>
		/// <param name="args">The method arguments</param>
		/// <returns>The output of the specified method</returns>
		public static object InvokeInstanceMethod(object instance, string method, ref object[] args)
		{
			BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			return InvokeMethod(instance.GetType(), method, instance, ref args, flags);
		}

		private static object InvokeMethod(Type type, string method, object instance, ref object[] args, BindingFlags flags)
		{
			MethodInfo methodInfo;

			try
			{
				methodInfo = type.GetMethod(method, flags);

				if (methodInfo == null)
				{
					throw new ArgumentException(String.Format("There is no method '{0}' in type '{1}'.", method, type.ToString()));
				}

				return methodInfo.Invoke(instance, args);
			}
			catch (TargetInvocationException exc)
			{
				if (exc.InnerException != null)
				{
					throw exc.InnerException;
				}
				else
				{
					throw exc;
				}
			}
		}
	}
}
