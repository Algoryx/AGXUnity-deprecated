using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class ConstraintCreateTool : Tool
  {
    public GameObject Parent { get; private set; }

    public bool MakeConstraintChildToParent { get; set; }

    public ConstraintAttachmentFrameTool AttachmentFrameTool
    {
      get { return GetChild<ConstraintAttachmentFrameTool>(); }
      set
      {
        RemoveChild( AttachmentFrameTool );
        AddChild( value );
      }
    }

    public ConstraintCreateTool( GameObject parent, bool makeConstraintChildToParent )
    {
      Parent = parent;
      MakeConstraintChildToParent = makeConstraintChildToParent;
    }

    public override void OnAdd()
    {
      m_createConstraintData.CreateInitialState( Parent.name );
      AttachmentFrameTool = new ConstraintAttachmentFrameTool( m_createConstraintData.AttachmentPair, Parent );
    }

    public override void OnRemove()
    {
      m_createConstraintData.Reset( true );
    }

    public override void OnSceneViewGUI( SceneView sceneView )
    {
      if ( Parent == null ) {
        PerformRemoveFromParent();
        return;
      }
    }

    public void OnInspectorGUI( GUISkin skin )
    {
      if ( AttachmentFrameTool == null || AttachmentFrameTool.AttachmentPair == null ) {
        PerformRemoveFromParent();
        return;
      }

      using ( new GUI.Indent( 16 ) ) {
        GUILayout.BeginHorizontal();
        {
          GUILayout.Label( GUI.MakeLabel( "Name", true ), skin.label, GUILayout.Width( 64 ) );
          m_createConstraintData.Name = GUILayout.TextField( m_createConstraintData.Name, skin.textField, GUILayout.ExpandWidth( true ) );
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
          GUILayout.Label( GUI.MakeLabel( "Type", true ), skin.label, GUILayout.Width( 64 ) );
          using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.yellow, 0.1f ) ) )
            m_createConstraintData.ConstraintType = (ConstraintType)EditorGUILayout.EnumPopup( m_createConstraintData.ConstraintType, skin.button, GUILayout.ExpandWidth( true ), GUILayout.Height( 18 ) );
        }
        GUILayout.EndHorizontal();
      }

      GUI.Separator3D();

      AttachmentFrameTool.OnPreTargetMembersGUI( skin );

      m_createConstraintData.CollisionState = ConstraintTool.ConstraintCollisionsStateGUI( m_createConstraintData.CollisionState, skin );
      m_createConstraintData.SolveType = ConstraintTool.ConstraintSolveTypeGUI( m_createConstraintData.SolveType, skin );

      GUI.Separator3D();

      bool createConstraintPressed = false;
      bool cancelPressed = false;
      GUILayout.BeginHorizontal();
      {
        GUILayout.Space( 12 );

        using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.green, 0.1f ) ) )
          createConstraintPressed = GUILayout.Button( GUI.MakeLabel( "Create", true, "Create the constraint" ), skin.button, GUILayout.Width( 120 ), GUILayout.Height( 26 ) );

        GUILayout.BeginVertical();
        {
          GUILayout.Space( 13 );
          using ( new GUI.ColorBlock( Color.Lerp( UnityEngine.GUI.color, Color.red, 0.1f ) ) )
            cancelPressed = GUILayout.Button( GUI.MakeLabel( "Cancel", false ), skin.button, GUILayout.Width( 96 ), GUILayout.Height( 16 ) );
          GUILayout.EndVertical();
        }
      }
      GUILayout.EndHorizontal();

      GUI.Separator3D();

      if ( createConstraintPressed ) {
        GameObject constraintGameObject = Factory.Create( m_createConstraintData.ConstraintType, m_createConstraintData.AttachmentPair );
        constraintGameObject.name = m_createConstraintData.Name;
        constraintGameObject.GetComponent<Constraint>().CollisionsState = m_createConstraintData.CollisionState;

        if ( MakeConstraintChildToParent )
          constraintGameObject.transform.SetParent( Parent.transform );

        Undo.RegisterCreatedObjectUndo( constraintGameObject, "New constraint '" + constraintGameObject.name + "' created" );

        m_createConstraintData.Reset( false );
      }

      if ( cancelPressed || createConstraintPressed )
        PerformRemoveFromParent();
    }

    private class CreateConstraintData
    {
      public ConstraintType ConstraintType
      {
        get { return (ConstraintType)EditorData.Instance.GetStaticData( "CreateConstraintData.ConstraintType" ).Int; }
        set { EditorData.Instance.GetStaticData( "CreateConstraintData.ConstraintType" ).Int = (int)value; }
      }

      private Action<EditorDataEntry> m_defaultCollisionState = new Action<EditorDataEntry>( entry => { entry.Int = (int)Constraint.ECollisionsState.DisableRigidBody1VsRigidBody2; } );
      public Constraint.ECollisionsState CollisionState
      {
        get { return (Constraint.ECollisionsState)EditorData.Instance.GetStaticData( "CreateConstraintData.CollisionState", m_defaultCollisionState ).Int; }
        set { EditorData.Instance.GetStaticData( "CreateConstraintData.CollisionState", m_defaultCollisionState ).Int = (int)value; }
      }

      private Action<EditorDataEntry> m_defaultSolveType = new Action<EditorDataEntry>( entry => { entry.Int = (int)Constraint.ESolveType.Direct; } );
      public Constraint.ESolveType SolveType
      {
        get { return (Constraint.ESolveType)EditorData.Instance.GetStaticData( "CreateConstraintData.SolveType", m_defaultSolveType ).Int; }
        set { EditorData.Instance.GetStaticData( "CreateConstraintData.SolveType", m_defaultSolveType ).Int = (int)value; }
      }

      public string Name                             = string.Empty;
      public ConstraintAttachmentPair AttachmentPair = null;

      public void CreateInitialState( string name )
      {
        if ( AttachmentPair != null ) {
          Debug.LogError( "Attachment pair already created. Make sure to clean any previous state before initializing a new one.", AttachmentPair );
          return;
        }

        AttachmentPair = ConstraintAttachmentPair.Create<ConstraintAttachmentPair>();
        Name           = Factory.CreateName( name + "_constraint" );
      }

      public void Reset( bool deleteAttachmentPair )
      {
        if ( AttachmentPair != null && deleteAttachmentPair )
          ScriptComponent.DestroyImmediate( AttachmentPair );
        AttachmentPair = null;
      }
    }

    private CreateConstraintData m_createConstraintData = new CreateConstraintData();
  }
}
