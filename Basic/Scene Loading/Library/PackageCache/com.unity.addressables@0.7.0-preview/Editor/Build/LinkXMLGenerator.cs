using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace UnityEditor.AddressableAssets.Build {
    class LinkXmlGenerator
    {
        HashSet<Type> m_Types = new HashSet<Type>();
        public void AddTypes(params Type[] types)
        {
            if (types == null)
                return;
            foreach (var t in types)
                m_Types.Add(t);
        }
        public void AddTypes(IEnumerable<Type> types)
        {
            if (types == null)
                return;
            foreach (var t in types)
                m_Types.Add(t);
        }

        public void Save(string path)
        {
            var assemblyMap = new Dictionary<Assembly, List<Type>> ();
            foreach (var t in m_Types)
            {
                var a = t.Assembly;
                List<Type> types;
                if (!assemblyMap.TryGetValue(a, out types))
                    assemblyMap.Add(a, types = new List<Type>());
                types.Add(t);
            }
            XmlDocument doc = new XmlDocument();
            var linker = doc.AppendChild(doc.CreateElement("linker"));
            foreach (var k in assemblyMap)
            {
                var assembly = linker.AppendChild(doc.CreateElement("assembly"));
                var attr = doc.CreateAttribute("fullname");
                attr.Value = k.Key.FullName;
                if (assembly.Attributes != null)
                {
                    assembly.Attributes.Append(attr);
                    foreach (var t in k.Value)
                    {
                        var typeEl = assembly.AppendChild(doc.CreateElement("type"));
                        var tattr = doc.CreateAttribute("fullname");
                        tattr.Value = t.FullName;
                        if (typeEl.Attributes != null)
                        {
                            typeEl.Attributes.Append(tattr);
                            var pattr = doc.CreateAttribute("preserve");
                            pattr.Value = "all";
                            typeEl.Attributes.Append(pattr);
                        }
                    }
                }
            }
            doc.Save(path);
        }
    }
}
