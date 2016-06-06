using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  public class RigidBodyTool : Tool
  {
    public RigidBody RigidBody { get; private set; }

    public RigidBodyTool( RigidBody rb )
    {
      RigidBody = rb;
    }

    public override void OnAdd()
    {
    }

    public override void OnRemove()
    {
    }

    public override void OnInspectorGUI( GUISkin skin )
    {
      GUILayout.Label( GUI.MakeLabel( "Mass properties", true ), skin.label );
      using ( new GUI.Indent( 12 ) )
        BaseEditor<MassProperties>.Update( RigidBody.MassProperties, skin );
      GUI.Separator();
    }
  }
}
