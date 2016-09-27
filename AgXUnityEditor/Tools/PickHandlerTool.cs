using System;
using UnityEngine;
using UnityEditor;
using AgXUnity;

namespace AgXUnityEditor.Tools
{
  public class PickHandlerTool : Tool
  {
    private float m_distanceFromCamera = -1f;
    private Predicate<Event> m_removePredicate = null;

    [Flags]
    public enum DofTypes
    {
      Translation = 1 << 0,
      Rotation    = 1 << 1
    }

    public GameObject ConstraintGameObject { get; private set; }
    public Constraint Constraint { get { return ConstraintGameObject != null ? ConstraintGameObject.GetComponent<Constraint>() : null; } }
    public DofTypes ConstrainedDofTypes { get; private set; }

    public PickHandlerTool( DofTypes constrainedDofTypes, Predicate<Event> removePredicate )
    {
      if ( removePredicate == null )
        throw new ArgumentNullException( "removePredicate", "When to remove callback is null - undefined." );

      ConstrainedDofTypes = constrainedDofTypes;
      m_removePredicate = removePredicate;
    }

    public override void OnAdd()
    {
      Initialize();
    }

    public override void OnRemove()
    {
      if ( ConstraintGameObject != null )
        GameObject.DestroyImmediate( ConstraintGameObject );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      // Remove us if the constraint never were created or at the mouse up event.
      if ( ConstraintGameObject == null || m_removePredicate( Event.current ) ) {
        PerformRemoveFromParent();
        return;
      }

      // NOTE: camera.ScreenToWorldPoint is not stable during all types of events. Pick one!
      if ( !Event.current.isMouse )
        return;

      // Initialize and keep distance from camera.
      if ( m_distanceFromCamera < 0f )
        m_distanceFromCamera = sceneView.camera.WorldToViewportPoint( Constraint.AttachmentPair.ReferenceFrame.Position ).z;

      Constraint.AttachmentPair.ConnectedFrame.Position = sceneView.camera.ScreenToWorldPoint( new Vector3( Event.current.mousePosition.x,
                                                                                                            sceneView.camera.pixelHeight - Event.current.mousePosition.y,
                                                                                                            m_distanceFromCamera ) );

      SetComplianceDamping( Constraint );
    }

    private void Initialize()
    {
      if ( Manager.MouseOverObject == null )
        return;

      var hit = AgXUnity.Utils.Raycast.Test( Manager.MouseOverObject, HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ) );
      if ( !hit.Triangle.Valid || hit.Triangle.Target.GetComponentInParent<RigidBody>() == null )
        return;

      ConstraintType constraintType = ConstrainedDofTypes == DofTypes.Translation ?
                                        ConstraintType.BallJoint :
                                      ConstrainedDofTypes == DofTypes.Rotation ?
                                        ConstraintType.AngularLockJoint :
                                      ( ConstrainedDofTypes & DofTypes.Translation ) != 0 && ( ConstrainedDofTypes & DofTypes.Rotation ) != 0 ?
                                        ConstraintType.LockJoint :
                                        ConstraintType.BallJoint;

      ConstraintGameObject  = Factory.Create( constraintType );
      Constraint constraint = Constraint;

      constraint.AttachmentPair.ReferenceObject         = hit.Triangle.Target;
      constraint.AttachmentPair.ReferenceFrame.Position = hit.Triangle.Point;

      constraint.AttachmentPair.ConnectedObject         = null;
      constraint.AttachmentPair.ConnectedFrame.Position = hit.Triangle.Point;

      constraint.AttachmentPair.Synchronized = false;

      ConstraintGameObject.name = "PickHandlerConstraint";

      SetComplianceDamping( Constraint );
    }

    private void SetComplianceDamping( Constraint constraint )
    {
      RigidBody rb1 = constraint.AttachmentPair.ReferenceObject.GetComponentInParent<RigidBody>();
      if ( rb1 == null )
        return;

      float mass = rb1.MassProperties.Mass.Value;
      float distVal = Vector3.SqrMagnitude( constraint.AttachmentPair.ReferenceFrame.Position - constraint.AttachmentPair.ConnectedFrame.Position ) + 0.1f;
      distVal = distVal > 1.5f ? distVal * distVal : distVal;

      float translationalCompliance = 1.0E-3f / ( distVal * Mathf.Max( mass, 1.0f ) );
      float rotationalCompliance    = 1.0E-10f / ( Mathf.Max( mass, 1.0f ) );
      float damping                 = 10.0f / 60.0f;

      var rowParser = AgXUnity.Utils.ConstraintUtils.ConstraintRowParser.Create( constraint );
      if ( rowParser == null )
        return;

      foreach ( var translationalRow in rowParser.TranslationalRows ) {
        if ( translationalRow == null )
          continue;

        translationalRow.RowData.Compliance = translationalCompliance;
        translationalRow.RowData.Damping = damping;
      }

      foreach ( var rotationalRow in rowParser.RotationalRows ) {
        if ( rotationalRow == null )
          continue;

        rotationalRow.RowData.Compliance = rotationalCompliance;
        rotationalRow.RowData.Damping = damping;
      }
    }
  }
}
