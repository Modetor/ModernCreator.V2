using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EmbededServer
{
    public class ComponentsLoader
    {
        public static void Load(ref Server server) 
        {
            System.Collections.IEnumerator enumerator = server.Configuration.Components.GetEnumerator();
            while(enumerator.MoveNext())
            {
                DLL dll = (DLL)enumerator.Current;
                Component component;
                try
                {
                    Assembly assembly = Assembly.LoadFile(dll.Path);
                    Type? type = assembly.GetType(dll.EntryPoint);
                    if (type == null)
                        continue;

                    component = new()
                    {
                        Type = type,
                        Name = dll.EntryPoint
                    };

                    MethodInfo? info = type.GetMethod("GetDefinedRout", BindingFlags.Static | BindingFlags.Public);
                    string? rout = info?.Invoke(null, null) as string;
                    if (rout != null)
                        server.AddComponent(rout, component);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("Error -----------------\n{0}\n{1}", exp.Message, exp.StackTrace);
                }
            }
        }
        public static void Loadx(ref Server server)
        {
            string[] dlls = Directory.GetFiles("components\\*.dll");
            System.Collections.IEnumerator enumerator = dlls.GetEnumerator();
            while(enumerator.MoveNext())
            {
                string dllName = (string)enumerator.Current;
                Index in1 = -3, in2 = -2;

                Component component;
                try
                {
                    string[] namespaceWithClassName = dllName.Split('.');    // try to get Project.ClassA from filename
                    string nsNclass = $"{namespaceWithClassName[in1]}.{namespaceWithClassName[in2]}";
                    
                    Assembly assembly = Assembly.LoadFile(dllName);
                    Type? type = assembly.GetType(nsNclass);
                    if(type== null)
                        continue;

                    component = new()
                    {
                        Type = type,
                        Name = nsNclass
                    };

                    MethodInfo? info = type.GetMethod("GetDefinedRout", BindingFlags.Static | BindingFlags.Public);
                    string? rout = info?.Invoke(null, null) as string;
                    if (rout != null)
                        server.AddComponent(rout, component);
                } 
                catch(Exception exp)
                {
                    Console.WriteLine("Error -----------------\n{0}\n{1}", exp.Message, exp.StackTrace);
                }
            }
        }
    }
}
