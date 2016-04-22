using System;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  /// <summary>
  /// Static factory object to create shapes, bodies, constraints, wires etc.
  /// </summary>
  public class Factory
  {
    /// <summary>
    /// Creates unique name. If for example there's a game object with name
    /// "body", then Factory.CreateName( "body" ) will return "body (1)".
    /// </summary>
    public static string CreateName( string name )
    {
      int counter = 0;
      string finalName = name;
      while ( GameObject.Find( finalName ) != null )
        finalName = name + " (" + (++counter) + ")";

      return finalName;
    }

    /// <summary>
    /// Creates unique name given type. E.g., "RigidBody_0" for the first
    /// AgXUnity.RigidBody instance, "RigidBody_1" for second etc.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    /// <returns>Unique name containing the type name.</returns>
    public static string CreateName<T>() where T : ScriptComponent
    {
      Type t = typeof( T );
      string namespaceName = t.Namespace != null ? t.Namespace + "." : "";
      return CreateName( namespaceName + t.Name );
    }

    /// <summary>
    /// Creates a new game object with component of type <typeparamref name="T"/>.
    /// </summary>
    /// <example>
    /// GameObject rb = Factory.Create<RigidBody>();
    /// </example>
    /// <typeparam name="T">Type of component.</typeparam>
    /// <returns>New game object with component <typeparamref name="T"/>.</returns>
    public static GameObject Create<T>() where T : ScriptComponent
    {
      GameObject go = new GameObject( CreateName<T>() );
      go.AddComponent<T>();
      return go;
    }

    /// <summary>
    /// Create new game object with component <typeparamref name="T"/> and adds
    /// <paramref name="child"/> as child.
    /// </summary>
    /// <example>
    /// GameObject rbBox = Factory.Create<RigidBody>( Factor.Create<Box>() );
    /// </example>
    /// <typeparam name="T">Type of component.</typeparam>
    /// <param name="child">Child to new game object.</param>
    /// <returns>New game object with component <typeparamref name="T"/> and child <paramref name="child"/>.</returns>
    public static GameObject Create<T>( GameObject child ) where T : ScriptComponent
    {
      return Create<T>().AddChild( child );
    }

// TODO: Replace code.
#if false
    /// <summary>
    /// Creates a constraint given an array of game objects (one or two supported).
    /// </summary>
    /// <typeparam name="T">Type of constraint.</typeparam>
    /// <param name="gameObjects">Constraint frame game objects.</param>
    /// <returns>Constraint of type <typeparamref name="T"/> if the configuration is valid.</returns>
    public static GameObject CreateConstraint<T>( GameObject[] gameObjects ) where T : Constraint
    {
      if ( gameObjects == null || gameObjects.Length == 0 || gameObjects.Length > 2 ) {
        Debug.LogWarning( "One or two objects has to be selected to create constraint: Number of objects selected: " + gameObjects.Length );
        return null;
      }

      if ( gameObjects.Length > 1 )
        return CreateConstraint<T>( gameObjects[ 0 ], gameObjects[ 1 ] );

      GameObject frame2 = new GameObject( CreateName<T>() + "_worldFrame" );
      frame2.transform.localPosition = gameObjects[ 0 ].transform.position;
      frame2.transform.localRotation = gameObjects[ 0 ].transform.rotation;

      GameObject constraint = CreateConstraint<T>( gameObjects[ 0 ], frame2 );
      // Just to make a nice group with the world reference as
      // child (i.e., dropdown in inspector).
      if ( constraint != null )
        constraint.AddChild( frame2, false );
      else
        GameObject.DestroyImmediate( frame2 );

      return constraint;
    }

    /// <summary>
    /// Create constraint of given type given two constraint frame game objects.
    /// </summary>
    /// <typeparam name="T">Type of constraint.</typeparam>
    /// <param name="frame1">First constraint frame game object.</param>
    /// <param name="frame2">Second constraint frame game object.</param>
    /// <returns>Constraint of type <typeparamref name="T"/> if the configuration is valid.</returns>
    public static GameObject CreateConstraint<T>( GameObject frame1, GameObject frame2 ) where T : Constraint
    {
      T constraint = Constraint.Create<T>( frame1, frame2 );
      return constraint != null ? constraint.gameObject : null;
    }

    /// <summary>
    /// Creates constraint of given type given local position and rotation relative to <paramref name="rb1"/>.
    /// </summary>
    /// <typeparam name="T">Type of constraint.</typeparam>
    /// <param name="localPosition">Position in rb1 frame.</param>
    /// <param name="localRotation">Rotation in rb1 frame.</param>
    /// <param name="rb1">First rigid body instance.</param>
    /// <param name="rb2">Second rigid body instance (world if null).</param>
    /// <returns>Constraint of type <typeparamref name="T"/> if the configuration is valid.</returns>
    public static GameObject CreateConstraint<T>( Vector3 localPosition, Quaternion localRotation, RigidBody rb1, RigidBody rb2 ) where T : Constraint
    {
      if ( rb1 == null ) {
        Debug.LogWarning( "Unable to create constraint: First rigid body is null." );
        return null;
      }

      string constraintName = Constraint.CreateName<T>( rb1, rb2 );

      GameObject f1 = new GameObject( constraintName + "_frame" );
      f1.transform.localPosition = localPosition;
      f1.transform.localRotation = localRotation;
      rb1.gameObject.AddChild( f1 );

      GameObject f2 = new GameObject( constraintName + ( rb2 == null ? "_worldFrame" : "_frame" ) );
      f2.transform.position = f1.transform.position;
      f2.transform.rotation = f1.transform.rotation;
      if ( rb2 != null )
        rb2.gameObject.AddChild( f2, false );

      GameObject constraint = CreateConstraint<T>( f1, f2 );
      if ( constraint == null ) {
        GameObject.DestroyImmediate( f1 );
        GameObject.DestroyImmediate( f2 );

        return null;
      }

      if ( rb2 == null )
        constraint.AddChild( f2 );

      return constraint;
    }

    /// <summary>
    /// Create constraint of given type given axis and position relative to <paramref name="rb1"/>.
    /// </summary>
    /// <typeparam name="T">Type of constraint.</typeparam>
    /// <param name="localAxis">Axis in rb1 frame.</param>
    /// <param name="localPosition">Position in rb1 frame.</param>
    /// <param name="rb1">First rigid body.</param>
    /// <param name="rb2">Second rigid body (world if null).</param>
    /// <returns>Constraint of type <typeparamref name="T"/> if the configuration is valid.</returns>
    public static GameObject CreateConstraint<T>( Vector3 localAxis, Vector3 localPosition, RigidBody rb1, RigidBody rb2 ) where T : Constraint
    {
      return CreateConstraint<T>( localPosition, Quaternion.FromToRotation( Vector3.forward, localAxis ), rb1, rb2 );
    }
#endif

    /// <summary>
    /// Create a wire given route, radius, resolution and material.
    /// </summary>
    /// <param name="route">Wire route.</param>
    /// <param name="radius">Radius if the wire.</param>
    /// <param name="resolutionPerUnitLength">Resolution of the wire.</param>
    /// <param name="material">Shape material of the wire.</param>
    /// <returns>A new game object with a Wire component.</returns>
    public static Wire CreateWire( WireRoute route, float radius = 0.02f, float resolutionPerUnitLength = 1.5f, ShapeMaterial material = null )
    {
      if ( route == null )
        return null;

      GameObject go = new GameObject( CreateName<Wire>() );
      Wire wire     = go.AddComponent<Wire>();
      wire.Route    = route;

      wire.Radius                  = radius;
      wire.ResolutionPerUnitLength = resolutionPerUnitLength;
      wire.Material                = material;

      return wire;
    }

    //public static Cable CreateCable( Deformable1D.RouteNode[] route, float radius, float resolutionPerUnitLength, ShapeMaterial material = null )
    //{
    //  GameObject go = new GameObject( CreateName<Deformable1D>() );
    //  Cable cable = go.AddComponent<Cable>().Construct( route, radius, resolutionPerUnitLength );
    //  cable.Material = material;
    //  return cable;
    //}
  }
}
