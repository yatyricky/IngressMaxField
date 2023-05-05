using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IngressMaxField
{
    public class Link
    {
        public Portal From;
        public Portal To;

        public Link(Portal from, Portal to)
        {
            From = from;
            To = to;
        }

        public bool CanLink(List<Link> existingLinks)
        {
            var p11 = new Vector2(From.Position.x, From.Position.z);
            var p12 = new Vector2(To.Position.x, To.Position.z);
            return existingLinks.All(link =>
            {
                var p21 = new Vector2(link.From.Position.x, link.From.Position.z);
                var p22 = new Vector2(link.To.Position.x, link.To.Position.z);
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
    }
}
