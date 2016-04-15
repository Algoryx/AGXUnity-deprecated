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
      return Selection.activeGameObject = Factory.Create<AgXUnity.Collide.Box>();
    }

    [MenuItem( "AgXUnity/Collide/Sphere" )]
    public static GameObject Sphere()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.Collide.Sphere>();
    }

    [MenuItem( "AgXUnity/Collide/Capsule" )]
    public static GameObject Capsule()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.Collide.Capsule>();
    }

    [MenuItem( "AgXUnity/Collide/Cylinder" )]
    public static GameObject Cylinder()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.Collide.Cylinder>();
    }

    [MenuItem( "AgXUnity/Collide/Plane" )]
    public static GameObject Plane()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.Collide.Plane>();
    }

    [MenuItem( "AgXUnity/Collide/Mesh" )]
    public static GameObject Mesh()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.Collide.Mesh>();
    }
    #endregion

    #region Rigid bodies
    [MenuItem( "AgXUnity/Rigid body/Empty" )]
    public static GameObject RigidBodyEmpty()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.RigidBody>();
    }

    [MenuItem( "AgXUnity/Rigid body/Box" )]
    public static GameObject RigidBodyBox()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Box>() );
    }

    [MenuItem( "AgXUnity/Rigid body/Sphere" )]
    public static GameObject RigidBodySphere()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Sphere>() );
    }

    [MenuItem( "AgXUnity/Rigid body/Capsule" )]
    public static GameObject RigidBodyCapsule()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Capsule>() );
    }

    [MenuItem( "AgXUnity/Rigid body/Cylinder" )]
    public static GameObject RigidBodyCylinder()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Cylinder>() );
    }

    [MenuItem( "AgXUnity/Rigid body/Mesh" )]
    public static GameObject RigidBodyMesh()
    {
      return Selection.activeGameObject = Factory.Create<AgXUnity.RigidBody>( Factory.Create<AgXUnity.Collide.Mesh>() );
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
  }
}
