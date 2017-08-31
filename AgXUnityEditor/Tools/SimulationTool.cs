using System;
using AgXUnity;
using UnityEngine;
using UnityEditor;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  [CustomTool( typeof( Simulation ) )]
  public class SimulationTool : Tool
  {
    public Simulation Simulation { get; private set; }

    public SimulationTool( Simulation simulation )
    {
      Simulation = simulation;
    }

    public override void OnPostTargetMembersGUI( GUISkin skin )
    {
      EditorGUI.BeginDisabledGroup( !Application.isPlaying );
      if ( GUILayout.Button( GUI.MakeLabel( "Save current step as .agx",
                                            false,
                                            "Save scene in native file format when the editor is in play mode." ), skin.button ) ) {
        string result = EditorUtility.SaveFilePanel( "Save scene as .agx", "Assets", UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, "agx" );
        if ( result != string.Empty ) {
          var success = Simulation.SaveToNativeFile( result );
          if ( success )
            Debug.Log( "Successfully wrote simulation to file: " + result );
        }
      }
      EditorGUI.EndDisabledGroup();
    }
  }
}
