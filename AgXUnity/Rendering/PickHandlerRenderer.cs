using System;
using UnityEngine;

namespace AgXUnity.Rendering
{
  public class PickHandlerRenderer : ScriptComponent
  {
    public GameObject ReferenceSphere { get; private set; }
    public GameObject ConnectedSphere { get; private set; }
    public GameObject ConnectingCylinder { get; private set; }

    public void ThisMethodIsntAllowedToBeNamedUpdateByUnity( Constraint constraint )
    {
      if ( constraint == null || State != States.INITIALIZED )
        return;

      if ( constraint.Type == ConstraintType.AngularLockJoint )
        return;

      const float sphereRadius   = 0.05f;
      const float cylinderRadius = 0.5f * sphereRadius;

      Rendering.Spawner.Utils.SetSphereTransform( ReferenceSphere,
                                                  constraint.AttachmentPair.ReferenceFrame.Position,
                                                  Quaternion.identity,
                                                  sphereRadius,
                                                  true );

      Rendering.Spawner.Utils.SetSphereTransform( ConnectedSphere,
                                                  constraint.AttachmentPair.ConnectedFrame.Position,
                                                  Quaternion.identity,
                                                  sphereRadius,
                                                  true );

      Rendering.Spawner.Utils.SetCylinderTransform( ConnectingCylinder,
                                                    constraint.AttachmentPair.ReferenceFrame.Position,
                                                    constraint.AttachmentPair.ConnectedFrame.Position,
                                                    cylinderRadius,
                                                    true );
    }

    protected override bool Initialize()
    {
      const string shader = "Standard";

      if ( GetComponent<Constraint>().Type == ConstraintType.AngularLockJoint )
        return true;

      ReferenceSphere = Rendering.Spawner.Create( Rendering.Spawner.Primitive.Sphere, "PHR_ReferenceSphere", HideFlags.HideAndDontSave, shader );
      ConnectedSphere = Rendering.Spawner.Create( Rendering.Spawner.Primitive.Sphere, "PHR_ConnectedSphere", HideFlags.HideAndDontSave, shader );
      ConnectingCylinder = Rendering.Spawner.Create( Rendering.Spawner.Primitive.Cylinder, "PHR_ConnectingCylinder", HideFlags.HideAndDontSave, shader );

      ReferenceSphere.transform.SetParent( gameObject.transform );
      ConnectedSphere.transform.SetParent( gameObject.transform );
      ConnectingCylinder.transform.SetParent( gameObject.transform );

      Rendering.Spawner.Utils.SetColor( ReferenceSphere, PickHandler.ReferenceSphereColor );
      Rendering.Spawner.Utils.SetColor( ConnectedSphere, PickHandler.ConnectedSphereColor );
      Rendering.Spawner.Utils.SetColor( ConnectingCylinder, PickHandler.ConnectingCylinderColor );

      return true;
    }

    protected override void OnDestroy()
    {
      Rendering.Spawner.Destroy( ReferenceSphere );
      Rendering.Spawner.Destroy( ConnectedSphere );
      Rendering.Spawner.Destroy( ConnectingCylinder );

      base.OnDestroy();
    }
  }
}
