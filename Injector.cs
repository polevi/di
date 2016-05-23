using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

namespace DI
{
    [Singleton]
    public class Injector
    {
        private static Injector injector;

        private Dictionary<Type, object> singletons = new Dictionary<Type, object>();
        private Dictionary<Type, Type> bindings = new Dictionary<Type, Type>();
        private Dictionary<Type, ConstructorInfo> constructors = new Dictionary<Type, ConstructorInfo>();

        public static Injector CreateInstance()
        {
            if (injector == null)
            {
                injector = new Injector();
                injector.singletons.Add(typeof(Injector), injector);
            }

            return injector;
        }
        
        public void Bind(Type intf, Type type)
        {
            if (intf.IsInterface)
                bindings.Add(intf, type);
            else
                throw new Exception(String.Format("{0} is not an interface", intf.ToString()));
        }

        public object GetInstance(Type intf)
        {
            Type t = GetBindedType(intf);

            object obj;
            if (singletons.TryGetValue(t, out obj))
                return obj;

            if (!constructors.ContainsKey(t))
            {
                ConstructorInfo constructor = null;
                foreach (ConstructorInfo ci in t.GetConstructors())
                {
                    if (ci.GetParameters().Length == 0)
                        constructor = ci;
                    else
                    {
                        if (ci.GetCustomAttribute(typeof(Inject)) != null)
                        {
                            constructor = ci;
                            break;
                        }
                    }
                }

                if (constructor != null)
                    constructors.Add(t, constructor);
                else
                    throw new Exception(String.Format("Unable to find constructor for type {0}", t.ToString()));
            }

            ConstructorInfo c = constructors[t];
            object[] arr = new object[c.GetParameters().Length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = GetInstance(c.GetParameters()[i].ParameterType);
            }

            object instance = c.Invoke(arr);

            Attribute s = t.GetCustomAttribute(typeof(Singleton));
            if(s!=null)
                singletons.Add(t, instance);

            return instance;
        }

        private Type GetBindedType(Type t)
        {
            if(t.IsInterface)
            {
                if (!bindings.ContainsKey(t))
                    throw new Exception(String.Format("Unable to find type {0} in bindings table", t.ToString()));

                return bindings[t];
            }
            else
                return t;
        }
    }
}
