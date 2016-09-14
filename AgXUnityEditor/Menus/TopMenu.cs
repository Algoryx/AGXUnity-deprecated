using System;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor
{
  public static class TopMenu
  {
    #region Shapes
    [MenuItem( "AgXUnity/Collide/Box" )]
    public static GameObject Box()
    {
      GameObject go = Factory.Create<AgXUnity.Collide.Box>();
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "shape" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Collide/Sphere" )]
    public static GameObject Sphere()
    {
      GameObject go = Factory.Create<AgXUnity.Collide.Sphere>();
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "shape" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Collide/Capsule" )]
    public static GameObject Capsule()
    {
      GameObject go = Factory.Create<AgXUnity.Collide.Capsule>();
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "shape" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Collide/Cylinder" )]
    public static GameObject Cylinder()
    {
      GameObject go = Factory.Create<AgXUnity.Collide.Cylinder>();
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "shape" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Collide/Plane" )]
    public static GameObject Plane()
    {
      GameObject go = Factory.Create<AgXUnity.Collide.Plane>();
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "shape" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Collide/Mesh" )]
    public static GameObject Mesh()
    {
      GameObject go = Factory.Create<AgXUnity.Collide.Mesh>();
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "shape" );
      return Selection.activeGameObject = go;
    }
    #endregion

    #region Rigid bodies
    [MenuItem( "AgXUnity/Rigid body/Empty" )]
    public static GameObject RigidBodyEmpty()
    {
      GameObject go = Factory.Create<AgXUnity.RigidBody>();
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "body" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Rigid body/Box" )]
    public static GameObject RigidBodyBox()
    {
      GameObject go = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Box>() );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "body" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Rigid body/Sphere" )]
    public static GameObject RigidBodySphere()
    {
      GameObject go = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Sphere>() );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "body" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Rigid body/Capsule" )]
    public static GameObject RigidBodyCapsule()
    {
      GameObject go = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Capsule>() );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "body" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Rigid body/Cylinder" )]
    public static GameObject RigidBodyCylinder()
    {
      GameObject go = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Cylinder>() );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "body" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Rigid body/Mesh" )]
    public static GameObject RigidBodyMesh()
    {
      GameObject go = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Mesh>() );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "body" );
      return Selection.activeGameObject = go;
    }
    #endregion

    #region Constraint
    [MenuItem( "AgXUnity/Constraints/Hinge" )]
    public static GameObject ConstraintHinge()
    {
      GameObject go = Factory.Create( ConstraintType.Hinge );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "constraint" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Constraints/Prismatic" )]
    public static GameObject ConstraintPrismatic()
    {
      GameObject go = Factory.Create( ConstraintType.Prismatic );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "constraint" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Constraints/Lock Joint" )]
    public static GameObject ConstraintLockJoint()
    {
      GameObject go = Factory.Create( ConstraintType.LockJoint );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "constraint" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Constraints/Cylindrical Joint" )]
    public static GameObject ConstraintCylindricalJoint()
    {
      GameObject go = Factory.Create( ConstraintType.CylindricalJoint );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "constraint" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Constraints/Ball Joint" )]
    public static GameObject ConstraintBallJoint()
    {
      GameObject go = Factory.Create( ConstraintType.BallJoint );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "constraint" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Constraints/Distance Joint" )]
    public static GameObject ConstraintDistanceJoint()
    {
      GameObject go = Factory.Create( ConstraintType.DistanceJoint );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "constraint" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Constraints/Angular Lock Joint" )]
    public static GameObject ConstraintAngularLockJoint()
    {
      GameObject go = Factory.Create( ConstraintType.AngularLockJoint );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "constraint" );
      return Selection.activeGameObject = go;
    }

    [MenuItem( "AgXUnity/Constraints/Plane Joint" )]
    public static GameObject ConstraintPlaneJoint()
    {
      GameObject go = Factory.Create( ConstraintType.PlaneJoint );
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "constraint" );
      return Selection.activeGameObject = go;
    }
    #endregion

    #region Wire
    [MenuItem( "AgXUnity/Wire/Empty" )]
    public static GameObject WireEmpty()
    {
      GameObject go = Factory.Create<Wire>();
      if ( go != null )
        Undo.RegisterCreatedObjectUndo( go, "wire" );
      return Selection.activeGameObject = go;
    }
    #endregion

    #region Managers
    [MenuItem( "AgXUnity/Debug Render Manager" )]
    public static GameObject DebugRenderer()
    {
      if ( AgXUnity.Rendering.DebugRenderManager.Instance == null )
        AgXUnity.Rendering.DebugRenderManager.ResetDestroyedState();

      return Selection.activeGameObject = AgXUnity.Rendering.DebugRenderManager.Instance.gameObject;
    }

    [MenuItem( "AgXUnity/Simulation" )]
    public static GameObject Simulation()
    {
      if ( AgXUnity.Simulation.Instance == null )
        AgXUnity.Simulation.ResetDestroyedState();

      return Selection.activeGameObject = AgXUnity.Simulation.Instance.gameObject;
    }

    [MenuItem( "AgXUnity/Collision Groups Manager" )]
    public static GameObject CollisionsGroupManager()
    {
      if ( AgXUnity.CollisionGroupsManager.Instance == null )
        AgXUnity.CollisionGroupsManager.ResetDestroyedState();

      return Selection.activeGameObject = AgXUnity.CollisionGroupsManager.Instance.gameObject;
    }

    [MenuItem( "AgXUnity/Contact Material Manager" )]
    public static GameObject ContactMaterialManager()
    {
      if ( AgXUnity.ContactMaterialManager.Instance == null )
        AgXUnity.ContactMaterialManager.ResetDestroyedState();
      return Selection.activeGameObject = AgXUnity.ContactMaterialManager.Instance.gameObject;
    }

    [MenuItem( "AgXUnity/Wind and Water Manager" )]
    public static GameObject WindAndWaterManager()
    {
      if ( AgXUnity.WindAndWaterManager.Instance == null )
        AgXUnity.WindAndWaterManager.ResetDestroyedState();
      return Selection.activeGameObject = AgXUnity.WindAndWaterManager.Instance.gameObject;
    }
    #endregion

    #region Utils
    [MenuItem( "AgXUnity/Utils/Generate Custom Editors" )]
    public static void GenerateEditors()
    {
      Utils.CustomEditorGenerator.Generate();
    }
    #endregion
  }
}
