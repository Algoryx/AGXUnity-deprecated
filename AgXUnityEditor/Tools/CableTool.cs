using System;
using UnityEngine;
using UnityEditor;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  [CustomTool( typeof( AgXUnity.Cable ) )]
  public class CableTool : Tool
  {
    public AgXUnity.Cable Cable { get; private set; }

    public CableTool( AgXUnity.Cable cable )
    {
      Cable = cable;
    }
  }

  [CustomEditor( typeof( AgXUnity.CableProperties ) )]
  public class CablePropertiesEditor : BaseEditor<AgXUnity.CableProperties>
  {
    protected override bool OverrideOnInspectorGUI( AgXUnity.CableProperties properties, GUISkin skin )
    {
      Undo.RecordObject( properties, "Cable properties" );

      GUI.Separator();

      using ( new GUI.Indent( 12 ) ) {
        foreach ( AgXUnity.CableProperties.Direction dir in AgXUnity.CableProperties.Directions ) {
          OnPropertyGUI( dir, properties, skin );
          GUI.Separator();
        }
      }

      if ( UnityEngine.GUI.changed )
        EditorUtility.SetDirty( properties );

      return true;
    }

    private void OnPropertyGUI( AgXUnity.CableProperties.Direction dir, AgXUnity.CableProperties properties, GUISkin skin )
    {
      Undo.RecordObject( properties[ dir ], "Cable property " + dir.ToString() );

      if ( GUI.Foldout( EditorData.Instance.GetData( properties, "CableProperty" + dir.ToString() ), GUI.MakeLabel( dir.ToString() ), skin ) ) {
        using ( new GUI.Indent( 12 ) ) {
          properties[ dir ].YoungsModulus = Mathf.Clamp( EditorGUILayout.FloatField( GUI.MakeLabel( "Young's modulus" ), properties[ dir ].YoungsModulus ), 0.0f, float.PositiveInfinity );
          properties[ dir ].YieldPoint    = Mathf.Clamp( EditorGUILayout.FloatField( GUI.MakeLabel( "Yield point" ), properties[ dir ].YieldPoint ), 0.0f, float.PositiveInfinity );
          properties[ dir ].Damping       = Mathf.Clamp( EditorGUILayout.FloatField( GUI.MakeLabel( "Spook damping" ), properties[ dir ].Damping ), 0.0f, float.PositiveInfinity );
        }
      }

      if ( UnityEngine.GUI.changed )
        EditorUtility.SetDirty( properties[ dir ] );
    }
  }
}
