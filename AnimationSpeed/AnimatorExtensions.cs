using System;
using System.Reflection;
using UnityEngine;

public static class AnimatorExtensions
{
    /// <summary>Gets an instance method with single argument of type <typeparamref
    /// name="TArg0"/> and return type of <typeparamref name="TReturn"/> from <typeparamref
    /// name="TThis"/> and compiles it into a fast open delegate.</summary>
    /// <typeparam name="TThis">Type of the class owning the instance method.</typeparam>
    /// <typeparam name="TArg0">Type of the single parameter to the instance method to
    /// find.</typeparam>
    /// <typeparam name="TReturn">Type of the return for the method</typeparam>
    /// <param name="methodName">The name of the method the compile.</param>
    /// <returns>The compiled delegate, which should be about as fast as calling the function
    /// directly on the instance.</returns>
    /// <exception cref="ArgumentException">If the method can't be found, or it has an
    /// unexpected return type (the return type must match exactly).</exception>
    /// <see href="https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/"/>
    private static Func<TThis, TArg0, TReturn> BuildFastOpenMemberDelegate<TThis, TArg0, TReturn>(string methodName)
    {
        var method = typeof(TThis).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            CallingConventions.Any,
            new[] { typeof(TArg0) },
            null);

        if (method == null)
            throw new ArgumentException("Can't find method " + typeof(TThis).FullName + "." + methodName + "(" + typeof(TArg0).FullName + ")");
        else if (method.ReturnType != typeof(TReturn))
            throw new ArgumentException("Expected " + typeof(TThis).FullName + "." + methodName + "(" + typeof(TArg0).FullName + ") to have return type of string but was " + method.ReturnType.FullName);
        return (Func<TThis, TArg0, TReturn>)Delegate.CreateDelegate(typeof(Func<TThis, TArg0, TReturn>), method);
    }

    private static Func<Animator, int, string> _getCurrentStateName;
    /// <summary>[FOR DEBUGGING ONLY] Calls an internal method on <see cref="Animator"/> that
    /// returns the name of the current state for a layer. The internal method could be removed
    /// or refactored at any time, and may not have good performance.</summary>
    /// <param name="animator">The animator to get the current state from.</param>
    /// <param name="layer">The layer to get the current state from.</param>
    /// <returns>The name of the currently running state.</returns>
    public static string GetCurrentStateName(this Animator animator, int layer)
    {
        if (_getCurrentStateName == null)
            _getCurrentStateName = BuildFastOpenMemberDelegate<Animator, int, string>("GetCurrentStateName");
        return _getCurrentStateName(animator, layer);
    }

    private static Func<Animator, int, string> _getNextStateName;
    /// <summary>[FOR DEBUGGING ONLY] Calls an internal method on <see cref="Animator"/> that
    /// returns the name of the next state for a layer. The internal method could be removed or
    /// refactored at any time, and may not have good performance.</summary>
    /// <param name="animator">The animator to get the next state from.</param>
    /// <param name="layer">The layer to get the next state from.</param>
    /// <returns>The name of the next running state.</returns>
    public static string GetNextStateName(this Animator animator, int layer)
    {
        if (_getNextStateName == null)
            _getNextStateName = BuildFastOpenMemberDelegate<Animator, int, string>("GetNextStateName");
        return _getNextStateName(animator, layer);
    }


    private static Func<Animator, int, string> _resolveHash;
    /// <summary>[FOR DEBUGGING ONLY] Calls an internal method on <see cref="Animator"/> that
    /// returns the string used to create a hash from
    /// <see cref="Animator.StringToHash(string)"/>. The internal method could be removed or
    /// refactored at any time, and may not have good performance.</summary>
    /// <param name="animator">The animator to get the string from.</param>
    /// <param name="hash">The hash to get the original string for.</param>
    /// <returns>The name of the string for <paramref name="hash"/>.</returns>
    public static string ResolveHash(this Animator animator, int hash)
    {
        if (_resolveHash == null)
            _resolveHash = BuildFastOpenMemberDelegate<Animator, int, string>("ResolveHash");
        return _resolveHash(animator, hash);
    }
}
