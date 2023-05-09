using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace IngressMaxField
{
    [Serializable]
    public class Portal
    {
        private static readonly Regex RegexCoordinates = new Regex(@"https://intel\.ingress\.com/intel\?ll=(?<x>[\d\.\-]+),(?<y>[\d\.\-]+)&z=\d+&pll=([\d\.\-]+),([\d\.\-]+)", RegexOptions.Compiled);
        public const double Scale = 100;

        public static (double x, double y)? RefPos;

        public string name;
        public string link;

        [NonSerialized]
        public int Sequence;

        [NonSerialized]
        public float Angle;

        private Vector3? _position;

        public Vector3 Position
        {
            get
            {
                if (RefPos == null)
                {
                    return Vector3.zero;
                }

                if (_position == null)
                {
                    var ll = ExtractCoordinates(link);
                    var x = (float)((ll.x - RefPos.Value.x) * Scale);
                    var y = (float)((ll.y - RefPos.Value.y) * Scale);
                    var gps = new Vector3(x, 0f, y);
                    _position = Quaternion.AngleAxis(180, new Vector3(1, 0, 1)) * gps;
                }

                return _position.Value;
            }
        }

        private static (double x, double y) ExtractCoordinates(string link)
        {
            var match = RegexCoordinates.Match(link);
            if (!match.Success)
            {
                throw new Exception($"Not a valid ingress link {link}");
            }

            var x = double.Parse(match.Groups["x"].Value);
            var y = double.Parse(match.Groups["y"].Value);
            return new ValueTuple<double, double>(x, y);
        }

        public static void SetRefPos(string link)
        {
            RefPos = ExtractCoordinates(link);
        }

        public Portal(string link, string name)
        {
            this.link = link;
            this.name = name;
        }

        public override string ToString()
        {
            return $"[{Sequence}]{name}";
        }
    }
}
