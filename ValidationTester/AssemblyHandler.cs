using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IntegrationDevelopmentUtility.ValidationTester
{
    class AssemblyHandler
    {
        private string _assemblyPath;
        private string _externalNamespace;

        public string ExternalNamespace
        {
            get { return _externalNamespace; }
            set { _externalNamespace = value; }
        }

        //Temporarily made public
        public Assembly a;

        public AssemblyHandler(string assemblyPath)
        {
            _assemblyPath = assemblyPath;

            LoadAssembly();
        }

        private void LoadAssembly()
        {
            a = Assembly.LoadFrom(_assemblyPath);
        }

        public T CreateInstance<T>(string classname, bool isOptional = false)
        {
            Type connectionType = a.GetType(_externalNamespace + "." + classname);
            if (connectionType == null)
            {
                if (isOptional)
                    return default(T); //This is null for a generic type
                else
                    throw new Exception("Unable to find class type " + classname + " in the assembly " + _assemblyPath);
            }

            return (T)Activator.CreateInstance(connectionType);
        }

        public Type GetType(string typename)
        {
            return a.GetType(_externalNamespace + "." + typename);
        }

        public void Unload()
        {
            a = null;
        }

        /// <summary>
        /// Determine the Namespace for the Integration.Abstract inherited classes used by a given assembly
        /// </summary>
        /// <remarks>
        /// This is only a temporary solution and comes with many problems and restrictions:
        ///     It assumes that there is only one class named Connection and that it's namespace is valid for all the other required classes
        ///     It assumes that the inherited classes are named exactly to match the classes they inherit from
        ///     It does not validate the presence of any other required class
        /// As such, this should only be used as a temporary method for DLLs written by Red Rook. Prior to accepting any outside-written DLLs, we will need a place to specify
        /// the namespace, and a separate way to validate our requirements prior to loading the DLL in the first place.
        /// </remarks>
        /// <param name="a"></param>
        /// <returns></returns>
        public void DetermineNamespaceByClassName(string className)
        {
            string externalNamespace = null;
            //Look for the Connection object 
            foreach (Type type in a.GetTypes())
            {
                if (type.Name == className)
                {
                    externalNamespace = type.FullName.Substring(0, type.FullName.Length - (type.Name.Length + 1));
                    _externalNamespace = externalNamespace;
                    return;
                }
            }

            throw new Exception("Unable to retrieve external name space. Please ensure that the specified DLL contains a class named Connection");
        }
    }
}
