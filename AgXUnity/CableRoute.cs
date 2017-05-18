using System;
using UnityEngine;
using AgXUnity.Utils;

namespace AgXUnity
{
  /// <summary>
  /// Cable route object containing nodes that initializes a cable.
  /// This object is an IEnumerable, add "using System.Linq" to
  /// get a wide range of "features" such as ToArray().
  /// </summary>
  [HideInInspector]
  public class CableRoute : Route<CableRouteNode>
  {
    /// <summary>
    /// Add node to this route given type, parent, local position and local rotation.
    /// </summary>
    /// <param name="nodeType">Node type.</param>
    /// <param name="parent">Node parent object.</param>
    /// <param name="localPosition">Local position relative parent.</param>
    /// <param name="localRotation">Local rotation relative parent.</param>
    /// <returns></returns>
    public CableRouteNode Add( Cable.NodeType nodeType,
                               GameObject parent = null,
                               Vector3 localPosition = default( Vector3 ),
                               Quaternion localRotation = default( Quaternion ) )
    {
      var node = CableRouteNode.Create( nodeType, parent, localPosition, localRotation );
      if ( !Add( node ) )
        return null;

      return node;
    }

    public CableRouteNode Add( agxCable.CableSegment nativeSegment, Func<agxCable.SegmentAttachment, GameObject> attachmentParentCallback )
    {
      if ( nativeSegment == null )
        return null;

      // TODO: Use attachments instead of node type when we're reading from native?
      Cable.NodeType nodeType = nativeSegment.getAttachments().Count > 0 ?
                                  Cable.NodeType.BodyFixedNode :
                                  Cable.NodeType.FreeNode;

      CableRouteNode node = null;
      if ( nodeType == Cable.NodeType.BodyFixedNode ) {
        var attachment = nativeSegment.getAttachments()[ 0 ].get();
        var parent = attachmentParentCallback( attachment );
        node = CableRouteNode.Create( Cable.NodeType.BodyFixedNode,
                                      parent,
                                      attachment.getFrame().getLocalTranslate().ToHandedVector3(),
                                      attachment.getFrame().getLocalRotate().ToHandedQuaternion() );
      }
      else {
        node = CableRouteNode.Create( Cable.NodeType.FreeNode,
                                      null,
                                      nativeSegment.getBeginPosition().ToHandedVector3(),
                                      Quaternion.LookRotation( nativeSegment.getDirection().ToHandedVector3(), Vector3.up ) );
      }

      if ( !Add( node ) )
        return null;

      return null;
    }
  }
}
