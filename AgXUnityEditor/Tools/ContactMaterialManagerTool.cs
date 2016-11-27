using UnityEngine;
using UnityEditor;
using AgXUnity;
using GUI = AgXUnityEditor.Utils.GUI;

namespace AgXUnityEditor.Tools
{
  [CustomTool( typeof ( ContactMaterialManager ) )]
  public class ContactMaterialManagerTool : Tool
  {
    public ContactMaterialManager Manager { get; private set; }

    public ContactMaterialManagerTool( ContactMaterialManager manager )
    {
      Manager = manager;
    }

    public override void OnPreTargetMembersGUI( GUISkin skin )
    {
      OnContactMaterialsList( skin );
    }

    private void OnContactMaterialsList( GUISkin skin )
    {
      if ( !GUI.Foldout( EditorData.Instance.GetData( Manager, "ContactMaterials" ), GUI.MakeLabel( "Contact Materials" ), skin ) )
        return;

      var contactMaterials = Manager.ContactMaterials;
      using ( new GUI.Indent( 12 ) ) {
        foreach ( var contactMaterial in contactMaterials ) {
          GUI.Separator();

          if ( GUI.Foldout( EditorData.Instance.GetData( Manager, contactMaterial.name ), GUI.MakeLabel( contactMaterial.name ), skin ) )
            BaseEditor<ContactMaterial>.Update( contactMaterial, skin );
        }

        if ( contactMaterials.Length == 0 )
          GUILayout.Label( GUI.MakeLabel( "Empty", true ), skin.label );
      }
    }
  }
}
