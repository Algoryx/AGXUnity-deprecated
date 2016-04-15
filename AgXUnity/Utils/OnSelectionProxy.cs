using UnityEngine;

namespace AgXUnity.Utils
{
  [AddComponentMenu( "" )]
  [GenerateCustomEditor]
  public class OnSelectionProxy : ScriptComponent
  {
    [SerializeField]
    private GameObject m_target = null;

    public GameObject Target
    {
      get { return m_target; }
      set { m_target = value; }
    }
  }
}
