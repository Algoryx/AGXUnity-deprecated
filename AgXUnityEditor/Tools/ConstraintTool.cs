using System;
using UnityEngine;
using UnityEditor;
using AgXUnity;
using AgXUnity.Utils;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class ConstraintTool : Tool
  {
    public Constraint Constraint { get; private set; }

    public FrameTool ReferenceFrameTool { get; private set; }
    
    public FrameTool ConnectedFrameTool { get; private set; }

    public ConstraintTool( Constraint constraint )
    {
      Constraint = constraint;
    }

    public override void OnAdd()
    {
      HideDefaultHandlesEnableWhenRemoved();

      ReferenceFrameTool = new FrameTool( Constraint.AttachmentPair.ReferenceFrame ) { OnChangeDirtyTarget = Constraint };
      ConnectedFrameTool = new FrameTool( Constraint.AttachmentPair.ConnectedFrame ) { OnChangeDirtyTarget = Constraint, TransformHandleActive = !Constraint.AttachmentPair.Synchronized };

      AddChild( ReferenceFrameTool );
      AddChild( ConnectedFrameTool );
    }

    public override void OnRemove()
    {
      RemoveChild( ReferenceFrameTool );
      RemoveChild( ConnectedFrameTool );

      ReferenceFrameTool = ConnectedFrameTool = null;
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      bool guiWasEnabled = UnityEngine.GUI.enabled;

      GUILayout.Label( GUI.MakeLabel( Constraint.Type.ToString(), 24, true ), GUI.Align( skin.label, TextAnchor.MiddleCenter ) );
      GUI.Separator();

      using ( new GUI.Indent( 12 ) ) {
        GUILayout.Label( GUI.MakeLabel( "Reference frame", true ) );
        GUI.HandleFrame( Constraint.AttachmentPair.ReferenceFrame, skin, 4 + 12 );
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space( 12 );
        if ( GUILayout.Button( GUI.MakeLabel( "\u2194", false, "Synchronized with reference frame" ),
                               GUI.ConditionalCreateSelectedStyle( Constraint.AttachmentPair.Synchronized, skin.button ),
                               new GUILayoutOption[] { GUILayout.Width( 24 ), GUILayout.Height( 14 ) } ) ) {
          Undo.RecordObject( Constraint.AttachmentPair, "ConstraintTool" );

          Constraint.AttachmentPair.Synchronized = !Constraint.AttachmentPair.Synchronized;
          if ( Constraint.AttachmentPair.Synchronized )
            ConnectedFrameTool.TransformHandleActive = false;
        }
        GUILayout.Label( GUI.MakeLabel( "Connected frame", true ) );
        EditorGUILayout.EndHorizontal();
        UnityEngine.GUI.enabled = !Constraint.AttachmentPair.Synchronized;
        GUI.HandleFrame( Constraint.AttachmentPair.ConnectedFrame, skin, 4 + 12 );
        UnityEngine.GUI.enabled = guiWasEnabled;
      }

      GUI.Separator();

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

          if ( GUI.Foldout( Selected( SelectedFoldout.Controllers ), GUI.MakeLabel( "Controllers", true ), skin ) )
            using ( new GUI.Indent( 12 ) ) {
              GUI.Separator();
              foreach ( var controller in controllers ) {
                HandleConstraintControllerGUI( controller, skin );

                GUI.Separator();
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

    private EditorData.SelectedState Selected( SelectedFoldout sf, string identifier = "", bool defaultSelected = false )
    {
      return Manager.EditorData.Selected( Constraint, sf.ToString() + identifier, defaultSelected );
    }

    private class BeginConstraintRowGUI : IDisposable
    {
      private bool m_guiWasEnabled = true;

      public BeginConstraintRowGUI( ConstraintUtils.ConstraintRow row, InvokeWrapper wrapper )
      {
        m_guiWasEnabled = UnityEngine.GUI.enabled;

        wrapper.Object = row != null ? row.RowData : null;
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

      object value = null;
      if ( wrapper.IsType<float>() )
        value = EditorGUILayout.FloatField( wrapper.Get<float>() );
      else if ( wrapper.IsType<RangeReal>() ) {
        RangeReal currValue = wrapper.Get<RangeReal>();
        if ( value == null )
          currValue = new RangeReal();

        currValue.Min = EditorGUILayout.FloatField( currValue.Min, GUILayout.MaxWidth( 128 ) );
        currValue.Max = EditorGUILayout.FloatField( currValue.Max, GUILayout.MaxWidth( 128 ) );

        if ( currValue.Min > currValue.Max )
          currValue.Min = currValue.Max;

        value = currValue;
      }
      else {
      }

      if ( wrapper.ConditionalSet( value ) )
        EditorUtility.SetDirty( Constraint );
    }

    private void HandleConstraintControllerGUI( ElementaryConstraintController controller, GUISkin skin )
    {
      if ( GUI.Foldout( Selected( SelectedFoldout.Controller, ConstraintUtils.FindName( controller ) ), GUI.MakeLabel( ConstraintUtils.FindName( controller ), true ), skin ) ) {
        using ( new GUI.Indent( 12 ) ) {
          controller.Enable = GUILayout.Toggle( controller.Enable, GUI.MakeLabel( "Enable", controller.Enable ), skin.toggle );
          using ( new GUI.Indent( 18 ) )
            BaseEditor<ElementaryConstraint>.Update( controller, skin );
        }
      }
    }
  }
}
