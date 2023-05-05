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
                if (_position == null)
                {
                    var match = RegexCoordinates.Match(link);
                    if (match.Success)
                    {
                        var x = (float)(Frac(double.Parse(match.Groups["x"].Value)) * Scale);
                        var y = (float)(Frac(double.Parse(match.Groups["y"].Value)) * Scale);
                        var gps = new Vector3(x, 0f, y);
                        _position = Quaternion.AngleAxis(180, new Vector3(1, 0, 1)) * gps;
                    }
                    else
                    {
                        Debug.LogError("fail");
                        _position = Vector3.zero;
                    }
                }

                return _position.Value;
            }
        }

        private static double Frac(double number)
        {
            return number - Math.Truncate(number);
        }

        public Portal(string link, string name)
        {
            this.link = link;
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
