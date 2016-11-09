using System;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class ConstraintTool : ConstraintAttachmentFrameTool
  {
    public Constraint Constraint { get; private set; }

    public ConstraintTool( Constraint constraint )
      : base( constraint.AttachmentPair, constraint )
    {
      Constraint = constraint;
    }

    public override void OnAdd()
    {
      base.OnAdd();
    }

    public override void OnRemove()
    {
      base.OnRemove();
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      // Possible undo performed that deleted the constraint. Remove us.
      if ( Constraint == null ) {
        PerformRemoveFromParent();
        return;
      }

      GUILayout.Label( GUI.MakeLabel( Constraint.Type.ToString(), 24, true ), GUI.Align( skin.label, TextAnchor.MiddleCenter ) );
      GUI.Separator();

      // Render ConstraintAttachmentPair GUI.
      base.OnPreTargetMembersGUI( skin );

      GUI.Separator();

      Constraint.CollisionsState = ConstraintCollisionsStateGUI( Constraint.CollisionsState, skin );
      Constraint.SolveType = ConstraintSolveTypeGUI( Constraint.SolveType, skin );

      GUI.Separator();

      ConstraintRowsGUI( skin );
    }

    public static Constraint.ECollisionsState ConstraintCollisionsStateGUI( Constraint.ECollisionsState state, GUISkin skin )
    {
      bool guiWasEnabled = UnityEngine.GUI.enabled;

      using ( new GUI.Indent( 12 ) ) {
        EditorGUILayout.BeginHorizontal();
        {
          GUILayout.Label( GUI.MakeLabel( "Disable collisions: ", true ), GUI.Align( skin.label, TextAnchor.MiddleLeft ), new GUILayoutOption[] { GUILayout.Width( 140 ), GUILayout.Height( 25 ) } );

          UnityEngine.GUI.enabled = !EditorApplication.isPlaying;
          if ( GUILayout.Button( GUI.MakeLabel( "Rb " + GUI.Symbols.Synchronized.ToString() + " Rb", false, "Disable all shapes in rigid body 1 against all shapes in rigid body 2." ),
                                 GUI.ConditionalCreateSelectedStyle( state == Constraint.ECollisionsState.DisableRigidBody1VsRigidBody2, skin.button ),
                                 new GUILayoutOption[] { GUILayout.Width( 76 ), GUILayout.Height( 25 ) } ) )
            state = state == Constraint.ECollisionsState.DisableRigidBody1VsRigidBody2 ?
                      Constraint.ECollisionsState.KeepExternalState :
                      Constraint.ECollisionsState.DisableRigidBody1VsRigidBody2;

          if ( GUILayout.Button( GUI.MakeLabel( "Ref " + GUI.Symbols.Synchronized.ToString() + " Con", false, "Disable Reference object vs. Connected object." ),
                                 GUI.ConditionalCreateSelectedStyle( state == Constraint.ECollisionsState.DisableReferenceVsConnected, skin.button ),
                                 new GUILayoutOption[] { GUILayout.Width( 76 ), GUILayout.Height( 25 ) } ) )
            state = state == Constraint.ECollisionsState.DisableReferenceVsConnected ?
                      Constraint.ECollisionsState.KeepExternalState :
                      Constraint.ECollisionsState.DisableReferenceVsConnected;
          UnityEngine.GUI.enabled = guiWasEnabled;
        }
        EditorGUILayout.EndHorizontal();
      }

      return state;
    }

    public static Constraint.ESolveType ConstraintSolveTypeGUI( Constraint.ESolveType solveType, GUISkin skin )
    {
      GUILayout.BeginHorizontal();
      {
        GUILayout.Space( 12 );
        GUILayout.Label( GUI.MakeLabel( "Solve Type", true ), skin.label, GUILayout.Width( 140 ) );
        solveType = (Constraint.ESolveType)EditorGUILayout.EnumPopup( solveType, skin.button, GUILayout.ExpandWidth( true ), GUILayout.Height( 18 ), GUILayout.Width( 2 * 76 + 4 ) );
      }
      GUILayout.EndHorizontal();

      return solveType;
    }

    public void ConstraintRowsGUI( GUISkin skin )
    {
      try {
        ConstraintUtils.ConstraintRowParser constraintRowParser = ConstraintUtils.ConstraintRowParser.Create( Constraint );
        if ( constraintRowParser.Empty )
          throw new AgXUnity.Exception( "Unable to parse rows." );

        InvokeWrapper[] memberWrappers = InvokeWrapper.FindFieldsAndProperties( null, typeof( ElementaryConstraintRowData ) );
        if ( constraintRowParser.HasTranslationalRows ) {
          if ( GUI.Foldout( Selected( SelectedFoldout.OrdinaryElementaryTranslational ), GUI.MakeLabel( "Translational properties </b>(along constraint axis)<b>", true ), skin ) )
            using ( new GUI.Indent( 12 ) )
              HandleConstraintRowsGUI( constraintRowParser.TranslationalRows, memberWrappers, skin );
        }

        if ( constraintRowParser.HasRotationalRows ) {
          GUI.Separator();

          if ( GUI.Foldout( Selected( SelectedFoldout.OrdinaryElementaryRotational ), GUI.MakeLabel( "Rotational properties </b>(about constraint axis)<b>", true ), skin ) )
            using ( new GUI.Indent( 12 ) )
              HandleConstraintRowsGUI( constraintRowParser.RotationalRows, memberWrappers, skin );
        }

        ElementaryConstraintController[] controllers = Constraint.GetElementaryConstraintControllers();
        if ( controllers.Length > 0 ) {
          GUI.Separator();

          if ( GUI.Foldout( Selected( SelectedFoldout.Controllers ), GUI.MakeLabel( "Controllers", true ), skin ) ) {
            using ( new GUI.Indent( 12 ) ) {
              GUI.Separator();
              foreach ( var controller in controllers ) {
                HandleConstraintControllerGUI( controller, skin );

                GUI.Separator();
              }
            }
          }
        }
      }
      catch ( AgXUnity.Exception e ) {
        GUILayout.Label( GUI.MakeLabel( "Unable to parse constraint rows", true ), skin.label );
        GUILayout.Label( GUI.MakeLabel( "  - " + e.Message, Color.red ), skin.label );
      }
    }

    private enum SelectedFoldout
    {
      OrdinaryElementaryTranslational,
      OrdinaryElementaryRotational,
      Controllers,
      Controller
    }

    private EditorDataEntry Selected( SelectedFoldout sf, string identifier = "", bool defaultSelected = false )
    {
      return EditorData.Instance.GetData( Constraint, sf.ToString() + identifier, newEntry => { newEntry.Bool = defaultSelected; } );
    }

    private class BeginConstraintRowGUI : IDisposable
    {
      private bool m_guiWasEnabled = true;

      public BeginConstraintRowGUI( ConstraintUtils.ConstraintRow row, InvokeWrapper wrapper )
      {
        m_guiWasEnabled = UnityEngine.GUI.enabled;

        if ( row != null )
          Undo.RecordObject( row.ElementaryConstraint, "RowUpdate" );

        UnityEngine.GUI.enabled = m_guiWasEnabled && row != null && row.Valid;
      }

      public void Dispose()
      {
        UnityEngine.GUI.enabled = m_guiWasEnabled;
      }
    }

    private static string[] RowLabels = new string[] { "U", "V", "N" };
    private static GUILayoutOption RowLabelsWidth { get { return GUILayout.Width( 12 ); } }
    private static void RowLabel( int i, GUISkin skin ) { GUILayout.Label( GUI.MakeLabel( RowLabels[ i ] ), skin.label, RowLabelsWidth ); }

    private void HandleConstraintRowsGUI( ConstraintUtils.ConstraintRow[] rows, InvokeWrapper[] wrappers, GUISkin skin )
    {
      foreach ( InvokeWrapper wrapper in wrappers ) {
        if ( wrapper.HasAttribute<HideInInspector>() )
          continue;

        EditorGUILayout.BeginHorizontal();
        {
          GUILayout.Label( GUI.MakeLabel( wrapper.Member.Name ), skin.label, GUILayout.MinWidth( 74 ) );
          GUILayout.FlexibleSpace();
          EditorGUILayout.BeginVertical();
          {
            for ( int i = 0; i < 3; ++i ) {
              using ( new BeginConstraintRowGUI( rows[ i ], wrapper ) ) {
                EditorGUILayout.BeginHorizontal();
                {
                  HandleConstraintRowType( rows[ i ], i, wrapper, skin );
                }
                EditorGUILayout.EndHorizontal();
              }
            }
          }
          EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        GUI.Separator();
      }
    }

    private void HandleConstraintRowType( ConstraintUtils.ConstraintRow row, int rowIndex, InvokeWrapper wrapper, GUISkin skin )
    {
      RowLabel( rowIndex, skin );

      var rowData = row != null ? row.RowData : null;
      object value = null;
      if ( wrapper.IsType<float>() )
        value = EditorGUILayout.FloatField( wrapper.Get<float>( rowData ) );
      else if ( wrapper.IsType<RangeReal>() ) {
        RangeReal currValue = wrapper.Get<RangeReal>( rowData );
        if ( currValue == null )
          currValue = new RangeReal();

        currValue.Min = EditorGUILayout.FloatField( currValue.Min, GUILayout.MaxWidth( 128 ) );
        currValue.Max = EditorGUILayout.FloatField( currValue.Max, GUILayout.MaxWidth( 128 ) );

        if ( currValue.Min > currValue.Max )
          currValue.Min = currValue.Max;

        value = currValue;
      }
      else {
      }

      if ( wrapper.ConditionalSet( rowData, value ) )
        EditorUtility.SetDirty( Constraint );
    }

    private void HandleConstraintControllerGUI( ElementaryConstraintController controller, GUISkin skin )
    {
      var controllerType    = controller.GetControllerType();
      var controllerTypeTag = controllerType.ToString().Substring( 0, 1 );
      string dimString      = "[" + GUI.AddColorTag( controllerTypeTag,
                                                     controllerType == Constraint.ControllerType.Rotational ?
                                                       Color.Lerp( UnityEngine.GUI.color, Color.red, 0.75f ) :
                                                       Color.Lerp( UnityEngine.GUI.color, Color.green, 0.75f ) ) + "] ";
      if ( GUI.Foldout( Selected( SelectedFoldout.Controller, controllerTypeTag + ConstraintUtils.FindName( controller ) ), GUI.MakeLabel( dimString + ConstraintUtils.FindName( controller ), true ), skin ) ) {
        using ( new GUI.Indent( 12 ) ) {
          controller.Enable = GUI.Toggle( GUI.MakeLabel( "Enable", controller.Enable ), controller.Enable, skin.button, skin.label );
          BaseEditor<ElementaryConstraint>.Update( controller, skin );
        }
      }
    }
  }
}
