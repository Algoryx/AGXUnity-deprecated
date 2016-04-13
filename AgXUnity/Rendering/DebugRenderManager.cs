using System.Linq;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity.Rendering
{
  [ExecuteInEditMode]
  [GenerateCustomEditor]
  public class DebugRenderManager : UniqueGameObject<DebugRenderManager>
  {
    protected override bool Initialize()
    {
      gameObject.transform.position = Vector3.zero;
      gameObject.transform.rotation = Quaternion.identity;
      gameObject.isStatic = true;
      gameObject.hideFlags = HideFlags.None;

      return base.Initialize();
    }

    protected void Update()
    {
      UnityEngine.Object.FindObjectsOfType<Collide.Shape>().ToList().ForEach(
        shape =>
        {
          var data = shape.gameObject.GetOrCreateComponent<ShapeDebugRenderData>();

          bool hadNode = data.Node != null;
          data.Synchronize();
          bool debugDataCreated = !hadNode && data.Node != null;

          if ( debugDataCreated ) {
            gameObject.AddChild( data.Node );
            data.Node.AddComponent<OnSelectionProxy>().Target = shape.gameObject;
          }
        }
      );
    }
  }
}
