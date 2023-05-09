using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace IngressMaxField
{
    public class Group : MonoBehaviour
    {
        private static Dictionary<string, Portal> _allPortals;

        public static Dictionary<string, Portal> AllPortals
        {
            get
            {
                if (_allPortals == null)
                {
                    LoadFromDisk();
                }

                return _allPortals;
            }
        }

        private static void LoadFromDisk()
        {
            _allPortals = new Dictionary<string, Portal>();
            var fp = Path.Combine(Application.persistentDataPath, "intel.txt");
            if (!File.Exists(fp))
            {
                return;
            }

            foreach (var portal in ParsePortals(File.ReadAllLines(fp)))
            {
                _allPortals[portal.link] = portal;
            }
        }

        private static IEnumerable<Portal> ParsePortals(IEnumerable<string> lines)
        {
            return from line in lines where !string.IsNullOrEmpty(line) select line.Trim().Split(';') into tokens let link = tokens[0].Trim() let portalName = tokens[1].Trim() select new Portal(link, portalName);
        }

        [TextArea(10, 10)]
        [FoldoutGroup("Database")]
        public string portalsInfo;

        [Button]
        [FoldoutGroup("Database")]
        public void SaveDB()
        {
            LoadFromDisk();

            foreach (var portal in ParsePortals(portalsInfo.Split('\n')))
            {
                _allPortals[portal.link] = portal;
            }

            var list = _allPortals.Values.ToList();
            list.Sort((a, b) => string.Compare(a.link, b.link, StringComparison.Ordinal));

            var fp = Path.Combine(Application.persistentDataPath, "intel.txt");
            File.WriteAllLines(fp, from e in list select $"{e.link};{e.name}");
        }

        [ValueDropdown("SelectPortal", IsUniqueList = true)]
        public string[] portals;

        public IEnumerable SelectPortal()
        {
            var list = new ValueDropdownList<string>();
            foreach (var kv in AllPortals)
            {
                list.Add(kv.Value.name, kv.Key);
            }

            return list;
        }

        [ValueDropdown("SelectPortal")]
        public string target;

        [ValueDropdown("SelectPortal")]
        public string first;

        [TextArea(10, 10)]
        public string sequence;

        [ListDrawerSettings(ListElementLabelName = "RenderLink", OnTitleBarGUI = "Copy")]
        public Link[] steps;

        [UsedImplicitly]
        private void Copy()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.File))
            {
                Clipboard.Copy(string.Join("\n", from e in steps select e.RenderLink));
            }
        }

        [TextArea(10, 10)]
        public string requirements;

        public string[] Sort()
        {
            var portalList = portals.ToList();
            var targetPortal = AllPortals[target];
            var firstPortal = AllPortals[first];

            var vec = (firstPortal.Position - targetPortal.Position).normalized;
            var list = portalList.Where(p => p != first && p != target).Select(e => AllPortals[e]).ToList();
            foreach (var portal in list)
            {
                var v2 = (portal.Position - targetPortal.Position).normalized;
                portal.Angle = Vector3.SignedAngle(vec, v2, Vector3.up);
                if (portal.Angle < 0)
                {
                    portal.Angle += 360;
                }
            }

            list.Sort((a, b) => a.Angle < b.Angle ? -1 : 1);
            for (var i = 0; i < list.Count; i++)
            {
                list[i].Sequence = i + 1;
            }

            list.Insert(0, firstPortal);
            list.Insert(0, targetPortal);
            sequence = string.Join("\n", list.Select((t, i) => $"{i + 1}. {t.name}"));
            return list.Select(e => e.link).ToArray();
        }

        [Button]
        public void PlanWithSort()
        {
            Portal.SetRefPos(portals.First());
            portals = Sort();

            var targetPortal = AllPortals[target];
            var detailedSteps = new List<Link>();
            var previousPortals = new List<Portal>();
            var existingLinks = new List<Link>();
            var requiredKeys = new Dictionary<Portal, int>();
            var outBounds = new Dictionary<Portal, int>();

            // add star links first
            for (var i = 1; i < portals.Length; i++)
            {
                var curr = AllPortals[portals[i]];
                existingLinks.Add(new Link(curr, targetPortal));
            }

            void EstablishLink(Portal from, Portal to, bool add = true)
            {
                var newLink = new Link(from, to);
                detailedSteps.Add(newLink);
                if (add)
                {
                    existingLinks.Add(newLink);
                }

                requiredKeys[to] = requiredKeys.ContainsKey(to) ? requiredKeys[to] + 1 : 1;
                outBounds[from] = outBounds.ContainsKey(from) ? outBounds[from] + 1 : 1;
            }

            for (var i = 1; i < portals.Length; i++)
            {
                var curr = AllPortals[portals[i]];
                // create link to target
                EstablishLink(curr, targetPortal, false);
                // try link to previous portals
                foreach (var previousPortal in previousPortals)
                {
                    var pendingLink = new Link(curr, previousPortal);
                    if (pendingLink.CanLink(existingLinks))
                    {
                        EstablishLink(curr, previousPortal);
                    }
                }

                // add self to previous portals
                previousPortals.Add(curr);
            }

            steps = detailedSteps.ToArray();

            var listRequirements = new List<string>();
            foreach (var kv in outBounds)
            {
                if (kv.Value >= 8)
                {
                    listRequirements.Add($"{kv.Key.name} needs SBUL");
                }
            }

            foreach (var portalLink in portals)
            {
                var portal = AllPortals[portalLink];
                if (requiredKeys.TryGetValue(portal, out var count))
                {
                    listRequirements.Add($"Keys for {portal.name} is {count}");
                }
                else
                {
                    listRequirements.Add($"Keys for {portal.name} is !!!ZERO!!!");
                }
            }

            requirements = string.Join("\n", listRequirements);
        }
    }

    [CustomEditor(typeof(Group))]
    public class GroupEditor : OdinEditor
    {
        private Group Target => target as Group;

        private void OnSceneGUI()
        {
            var targetPortal = Group.AllPortals[Target.target];
            foreach (var portalLink in Target.portals)
            {
                if (!Group.AllPortals.ContainsKey(portalLink))
                {
                    continue;
                }

                var portal = Group.AllPortals[portalLink];
                var position = portal.Position;
                var color = Handles.color;
                if (portal.link == Target.target)
                {
                    Handles.color = Color.red;
                }
                else if (portal.link == Target.first)
                {
                    Handles.color = Color.yellow;
                }
                else
                {
                    Handles.color = Color.white;
                }

                Handles.DrawSolidArc(position, Vector3.up, Vector3.forward, 360, 0.001f);
                Handles.color = color;
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.black } };
                Handles.Label(position, $"[{portal.Sequence}]{portal.name}", style);

                if (portal.link != Target.target)
                {
                    Handles.DrawLine(targetPortal.Position, portal.Position);
                }
            }
        }
    }
}
