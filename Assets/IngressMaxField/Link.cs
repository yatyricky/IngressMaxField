using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IngressMaxField
{
    [Serializable]
    public class Link
    {
        public Portal from;
        public Portal to;

        public string RenderLink => $"{from} -> {to}";

        public Link(Portal from, Portal to)
        {
            this.from = from;
            this.to = to;
        }

        public bool CanLink(List<Link> existingLinks)
        {
            var p11 = new Vector2(from.Position.x, from.Position.z);
            var p12 = new Vector2(to.Position.x, to.Position.z);
            return existingLinks.All(link =>
            {
                var p21 = new Vector2(link.from.Position.x, link.from.Position.z);
                var p22 = new Vector2(link.to.Position.x, link.to.Position.z);
                var test = LineUtil.IntersectLineSegments2D(p11, p12, p21, p22, out var intersection);
                if (!test)
                {
                    return true;
                }

                if (LineUtil.Approximately(intersection, p11) || LineUtil.Approximately(intersection, p12))
                {
                    return true;
                }

                return false;
            });
        }

        [Button]
        public void Reverse()
        {
            (from, to) = (to, from);
        }
    }
}
