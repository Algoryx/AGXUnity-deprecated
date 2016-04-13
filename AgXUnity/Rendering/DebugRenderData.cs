using UnityEngine;

namespace AgXUnity.Rendering
{
  [GenerateCustomEditor]
  public abstract class DebugRenderData : ScriptComponent
  {
    [SerializeField]
    private GameObject m_node = null;
    public GameObject Node
    {
      get { return m_node; }
      set
      {
        m_node = value;
      }
    }

    public abstract string GetTypeName();

    public abstract void Synchronize();

    [HideInInspector]
    public string PrefabName { get { return GetPrefabName( GetTypeName() ); } }

    public static string GetPrefabName( string typeName )
    {
      return "Debug." + typeName + "Renderer";
    }
  }
}
