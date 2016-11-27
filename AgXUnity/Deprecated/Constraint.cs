using System;
using System.Collections.Generic;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity.Deprecated
{
  [AddComponentMenu( "" )]
  public class Constraint : ScriptComponent
  {
    public static Constraint Create( ConstraintType type, GameObject givenGameObject = null )
    {
      if ( givenGameObject != null && givenGameObject.GetComponent<Constraint>() != null ) {
        Debug.LogError( "Game object already have a AgXUnity.Constraint component. Create constraint failed.", givenGameObject );
        return null;
      }

      List<ElementaryDef> elements = null;
      switch ( type ) {
        case ConstraintType.Hinge:
          elements = new List<ElementaryDef>
          {
            ElementaryDef.CreateLinear<ElementarySphericalRel>( "SR" ),
            ElementaryDef.CreateAngular<ElementaryDot1>( "D1_VN", ConstraintRowData.Def.V ),
            ElementaryDef.CreateAngular<ElementaryDot1>( "D1_UN", ConstraintRowData.Def.U ),
            ElementaryDef.CreateAngular<RangeController>( "RR", ConstraintRowData.Def.N ),
            ElementaryDef.CreateAngular<TargetSpeedMotor>( "MR", ConstraintRowData.Def.N ),
            ElementaryDef.CreateAngular<LockController>( "LR", ConstraintRowData.Def.N )
          };
          break;
        case ConstraintType.Prismatic:
          elements = new List<ElementaryDef>
          {
            ElementaryDef.CreateAngular<ElementaryQuatLock>( "QL" ),
            ElementaryDef.CreateLinear<ElementaryDot2>( "D2_U", ConstraintRowData.Def.U ),
            ElementaryDef.CreateLinear<ElementaryDot2>( "D2_V", ConstraintRowData.Def.V ),
            ElementaryDef.CreateLinear<RangeController>( "RT", ConstraintRowData.Def.N ),
            ElementaryDef.CreateLinear<TargetSpeedMotor>( "MT", ConstraintRowData.Def.N ),
            ElementaryDef.CreateLinear<LockController>( "LT", ConstraintRowData.Def.N )
          };
          break;
        case ConstraintType.LockJoint:
          elements = new List<ElementaryDef>
          {
            ElementaryDef.CreateLinear<ElementarySphericalRel>( "SR" ),
            ElementaryDef.CreateAngular<ElementaryQuatLock>( "QL" )
          };
          break;
        case ConstraintType.CylindricalJoint:
          elements = new List<ElementaryDef>
          {
            ElementaryDef.CreateAngular<ElementaryDot1>( "D1_VN", ConstraintRowData.Def.U ),
            ElementaryDef.CreateAngular<ElementaryDot1>( "D1_UN", ConstraintRowData.Def.V ),
            ElementaryDef.CreateLinear<ElementaryDot2>( "D2_U", ConstraintRowData.Def.U ),
            ElementaryDef.CreateLinear<ElementaryDot2>( "D2_V", ConstraintRowData.Def.V ),
            ElementaryDef.CreateLinear<RangeController>( "RT", ConstraintRowData.Def.N ),
            ElementaryDef.CreateAngular<RangeController>( "RR", ConstraintRowData.Def.N ),
            ElementaryDef.CreateLinear<TargetSpeedMotor>( "MT", ConstraintRowData.Def.N ),
            ElementaryDef.CreateAngular<TargetSpeedMotor>( "MR", ConstraintRowData.Def.N ),
            ElementaryDef.CreateLinear<LockController>( "LT", ConstraintRowData.Def.N ),
            ElementaryDef.CreateAngular<LockController>( "LR", ConstraintRowData.Def.N )
          };
          break;
        case ConstraintType.BallJoint:
          elements = new List<ElementaryDef>
          {
            ElementaryDef.CreateLinear<ElementarySphericalRel>( "SR" )
          };
          break;
        case ConstraintType.AngularLockJoint:
          elements = new List<ElementaryDef>
          {
            ElementaryDef.CreateAngular<ElementaryQuatLock>( "QL" )
          };
          break;
        case ConstraintType.DistanceJoint:
          elements = new List<ElementaryDef>
          {
            ElementaryDef.CreateLinear<RangeController>( "RT", ConstraintRowData.Def.N ),
            ElementaryDef.CreateLinear<TargetSpeedMotor>( "MT", ConstraintRowData.Def.N ),
            ElementaryDef.CreateLinear<LockController>( "LT", ConstraintRowData.Def.N )
          };
          break;
        case ConstraintType.PlaneJoint:
          elements = new List<ElementaryDef>
          {
            ElementaryDef.CreateLinear<ElementaryContactNormal>( "CN", ConstraintRowData.Def.N )
          };
          break;
        default:
          Debug.LogError( "Unknown constraint type: " + type + "." );
          break;
      }

      if ( elements == null )
        return null;

      GameObject constraintGameObject = givenGameObject ?? new GameObject( Factory.CreateName( "AgXUnity." + type ) );
      Constraint constraint           = constraintGameObject.AddComponent<Constraint>();
      constraint.m_attachmentPair     = ScriptAsset.Create<ConstraintAttachmentPair>();
      constraint.m_type               = type;

      foreach ( var element in elements ) {
        ElementaryConstraint ec = (ElementaryConstraint)constraintGameObject.AddComponent( element.Type );
        ec.NativeName = element.Name;

        ec.RowData = new ConstraintRowData[ ec.NumRows ];
        for ( int i = 0; i < ec.RowData.Length; ++i )
          ec.RowData[ i ] = new ConstraintRowData();

        // Check if the element supports enhanced editor description.
        for ( int i = 0; ec.NumRows == element.Variables.Count && i < ec.NumRows; ++i )
          ec.RowData[ i ].Definition = element.Variables[ i ];
      }

      return constraint;
    }

    private agx.Constraint m_native = null;
    public agx.Constraint Native { get { return m_native; } }

    [SerializeField]
    private ConstraintType m_type = ConstraintType.Hinge;

    [HideInInspector]
    public ConstraintType Type { get { return m_type; } }

    public Type NativeType { get { return System.Type.GetType( "agx." + m_type + ", agxDotNet" ); } }

    [SerializeField]
    private ConstraintAttachmentPair m_attachmentPair = null;

    [HideInInspector]
    public ConstraintAttachmentPair AttachmentPair { get { return m_attachmentPair; } }

    protected override bool Initialize()
    {
      if ( m_attachmentPair.ReferenceObject == null ) {
        Debug.LogError( "Unable to initialize constraint. Reference object must be valid and contain a rigid body component.", this );
        return false;
      }

      m_attachmentPair.Update();

      RigidBody rb1 = m_attachmentPair.ReferenceObject.GetInitializedComponentInParent<RigidBody>();
      if ( rb1 == null ) {
        Debug.LogError( "Unable to initialize constraint. Reference object must contain a rigid body component.", m_attachmentPair.ReferenceObject );
        return false;
      }

      agx.Frame f1 = new agx.Frame();
      agx.Frame f2 = new agx.Frame();
      f1.setLocalTranslate( m_attachmentPair.ReferenceFrame.CalculateLocalPosition( rb1.gameObject ).ToHandedVec3() );
      f1.setLocalRotate( m_attachmentPair.ReferenceFrame.CalculateLocalRotation( rb1.gameObject ).ToHandedQuat() );

      RigidBody rb2 = m_attachmentPair.ConnectedObject != null ? m_attachmentPair.ConnectedObject.GetInitializedComponentInParent<RigidBody>() : null;
      if ( rb2 != null ) {
        f2.setLocalTranslate( m_attachmentPair.ConnectedFrame.CalculateLocalPosition( rb2.gameObject ).ToHandedVec3() );
        f2.setLocalRotate( m_attachmentPair.ConnectedFrame.CalculateLocalRotation( rb2.gameObject ).ToHandedQuat() );
      }
      else {
        f2.setLocalTranslate( m_attachmentPair.ConnectedFrame.Position.ToHandedVec3() );
        f2.setLocalRotate( m_attachmentPair.ConnectedFrame.Rotation.ToHandedQuat() );
      }

      try {
        m_native = (agx.Constraint)Activator.CreateInstance( NativeType, new object[] { rb1.Native, f1, ( rb2 != null ? rb2.Native : null ), f2 } );
      }
      catch ( System.Exception e ) {
        Debug.LogException( e, gameObject );
        return false;
      }

      GetSimulation().add( m_native );

      return m_native != null && m_native.getValid() && base.Initialize();
    }


    private static Mesh m_gizmosMesh = null;
    private static Mesh GetOrCreateGizmosMesh()
    {
      if ( m_gizmosMesh != null )
        return m_gizmosMesh;

      GameObject tmp = PrefabLoader.Instantiate<GameObject>( @"Debug/ConstraintRenderer" );
      MeshFilter[] filters = tmp.GetComponentsInChildren<MeshFilter>();
      CombineInstance[] combine = new CombineInstance[ filters.Length ];

      for ( int i = 0; i < filters.Length; ++i ) {
        combine[ i ].mesh = filters[ i ].sharedMesh;
        combine[ i ].transform = filters[ i ].transform.localToWorldMatrix;
      }

      m_gizmosMesh = new Mesh();
      m_gizmosMesh.CombineMeshes( combine );

      GameObject.DestroyImmediate( tmp );

      return m_gizmosMesh;
    }

    private static void DrawGizmos( Color color, ConstraintAttachmentPair attachmentPair )
    {
      Gizmos.color = color;
      Gizmos.DrawMesh( GetOrCreateGizmosMesh(), attachmentPair.ReferenceFrame.Position, attachmentPair.ReferenceFrame.Rotation * Quaternion.FromToRotation( Vector3.up, Vector3.forward ), 0.3f * Vector3.one );

      if ( !attachmentPair.Synchronized ) {
        Gizmos.color = Color.red;
        Gizmos.DrawLine( attachmentPair.ReferenceFrame.Position, attachmentPair.ConnectedFrame.Position );
        Gizmos.DrawMesh( GetOrCreateGizmosMesh(), attachmentPair.ConnectedFrame.Position, attachmentPair.ConnectedFrame.Rotation * Quaternion.FromToRotation( Vector3.up, Vector3.forward ), 0.2f * Vector3.one );
      }
    }

    private void OnDrawGizmos()
    {
      DrawGizmos( Color.blue, AttachmentPair );
    }

    private void OnDrawGizmosSelected()
    {
      DrawGizmos( Color.green, AttachmentPair );
    }
  }
}
