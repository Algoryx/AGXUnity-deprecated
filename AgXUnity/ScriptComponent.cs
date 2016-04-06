using System;
using System.Reflection;
using UnityEngine;

namespace AgXUnity
{
  /// <summary>
  /// Base class for components. Many of our classes instantiates native objects
  /// during initialize and we've components that are dependent on other components
  /// native instances (e.g., RigidBody vs. Constraint). This base class facilitates
  /// cross dependencies enabling implementations to depend on each other in an
  /// otherwise random initialization order.
  /// </summary>
  /// <example>
  /// RigidBody rb = gameObject1.GetComponent{RigidBody}().GetInitialized{RigidBody}();
  /// // rb should have a native instance
  /// assert( rb.Native != null );
  /// </example>
  public abstract class ScriptComponent : MonoBehaviour
  {
    public enum States
    {
      CONSTRUCTED = 0,
      AWAKE,
      INITIALIZING,
      INITIALIZED,
      DESTROYED
    }

    [HideInInspector]
    public States State { get; private set; }

    protected ScriptComponent()
    {
      NativeHandler.Instance.Register( this );

      agx.Thread.registerAsAgxThread();
    }

    /// <summary>
    /// Returns native simulation object unless the scene is being
    /// destructed.
    /// </summary>
    /// <returns>Native simulation object if not being destructed.</returns>
    public agxSDK.Simulation GetSimulation()
    {
      Simulation simulation = Simulation.Instance;
      return simulation ? simulation.Native : null;
    }

    /// <summary>
    /// Makes sure this component is returned fully initialized, if
    /// e.g., your component depends on native objects in this.
    /// </summary>
    /// <typeparam name="T">Type of this component.</typeparam>
    /// <returns>This component fully initialized, or null if failed.</returns>
    public T GetInitialized<T>() where T : ScriptComponent
    {
      return (T)InitializeCallback();
    }

    /// <summary>
    /// Invoked if GameObject extension method AddChild is used and
    /// a child is added.
    /// </summary>
    /// <param name="child">Child added to this components game object.</param>
    public virtual void OnChildAdded( GameObject child ) { }

    /// <summary>
    /// Internal method when initialize callback should be fired.
    /// </summary>
    protected ScriptComponent InitializeCallback()
    {
      if ( State == States.INITIALIZING )
        throw new Exception( "Initialize call when object is being initialized. Implement wait until initialized?" );

      if ( State == States.AWAKE ) {
        State = States.INITIALIZING;
        bool success = Initialize();
        State = success ? States.INITIALIZED : States.AWAKE;
      }

      return State == States.INITIALIZED ? this : null;
    }

    /// <summary>
    /// Initialize internal and/or native objects.
    /// </summary>
    /// <returns>true if successfully initialized</returns>
    protected virtual bool Initialize() { return true; }

    /// <summary>
    /// Register agx object method. Not possible to implement, use Initialize instead.
    /// </summary>
    protected void Awake()
    {
      State = States.AWAKE;
      OnAwake();
    }

    /// <summary>
    /// On first call, all ScriptComponent objects will get Initialize callback.
    /// NOTE: Implement "Initialize" rather than "Start".
    /// </summary>
    protected void Start()
    {
      InitializeCallback();

      Utils.PropertySynchronizer.Synchronize( this );
    }

    protected virtual void OnAwake() { }

    protected virtual void OnEnable() { }

    protected virtual void OnDisable() { }

    protected virtual void OnDestroy()
    {
      NativeHandler.Instance.Unregister( this );

      State = States.DESTROYED;
    }

    protected virtual void OnApplicationQuit() { }

    /// <summary>
    /// Send message to first ancestor of given type. If the component
    /// is at the same level as this, that component is defined to be
    /// closest and will be called.
    /// </summary>
    /// <typeparam name="T">Any ScriptComponent.</typeparam>
    /// <param name="methodName">Name of method to call.</param>
    /// <param name="arguments">Arguments to method. Note that they have to match!</param>
    protected void SendMessageToAncestor<T>( string methodName, object[] arguments ) where T : ScriptComponent
    {
      // Will start from our level! I.e., ancestor could be a fellow component
      // and not in transform.parent etc.
      T ancestor = Utils.Find.FirstParentWithComponent<T>( transform );
      if ( ancestor != null ) {
        MethodInfo method = typeof( T ).GetMethod( methodName, BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic );
        if ( method != null ) {
          try {
            method.Invoke( ancestor, arguments );
          }
          catch ( TargetParameterCountException ) {
            Debug.LogWarning( "Invoke failed, number of arguments doesn't match in method: " + methodName + " in type: " + typeof( T ) + " sent by: " + GetType() );
          }
          catch ( ArgumentException ) {
            Debug.LogWarning( "Argument mismatch while sending message to method: " + methodName + " in type: " + typeof( T ) + " sent by: " + GetType() );
          }
          catch ( System.Exception e ) {
            Debug.LogException( e );
          }
        }
      }
    }
  }
}
