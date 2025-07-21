//Just a comment to test

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace IntegrationDevelopmentUtility.DocumentationGenerator
{
    /// <summary>
    /// A simple utility class to handle the creation and instantiation of a given assembly. It also includes a procedure to determine the 
    /// expected namespace of the inherited classes in a given assembly.
    /// </summary>
    public class AssemblyHandler
    {
        private string _assemblyPath;
        private string _externalNamespace;

        //this is only used in the Test environment
        private CollectibleAssemblyLoadContext calc;

        public string ExternalNamespace
        {
            get { return _externalNamespace; }
            set { _externalNamespace = value; }
        }

        private Assembly a;

        public AssemblyHandler(string assemblyPath)
        {
            _assemblyPath = assemblyPath;

            LoadAssembly();
        }

        /// <summary>
        /// Load the given assembly
        /// </summary>
        private void LoadAssembly()
        {
            //throw new Exception("We shouldn't be using this right now. We are supposed to be using Coy's loader");

            //If there is a calc value, that means we are in a test environment and need to load the assembly via the calc
            if (calc != null)
            {
                //This works but seems to have a memory leak
                a = AppDomain.CurrentDomain.Load(calc.LoadFromAssemblyPath(_assemblyPath).GetName());
            }
            else
            {
                //a = Assembly.LoadFrom(_assemblyPath);
                //The line below is supposedly more thread-friendly than the one above
                a = AppDomain.CurrentDomain.Load(Assembly.LoadFrom(_assemblyPath).GetName());
            }
        }

        /// <summary>
        /// Create an instance of the class name specified. This class must be in the assembly and inside the namespace 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="classname"></param>
        /// <param name="isOptional"></param>
        /// <returns></returns>
        public T CreateInstance<T>(string classname, bool isOptional = false)
        {
            Type connectionType = a.GetType(_externalNamespace + "." + classname);
            if (connectionType == null)
            {
                if (isOptional)
                    return default(T); //This is null for a generic type
                else
                    throw new Exception($"Unable to find class type {classname} in the assembly {_assemblyPath}");
            }

            return (T)Activator.CreateInstance(connectionType);
        }

        //Get the System.Type based on the type name
        public Type GetType(string typename)
        {
            return a.GetType(_externalNamespace + "." + typename);
        }

        public void Unload()
        {
            if (calc != null)
                calc.Unload();
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

            throw new Exception($"Unable to retrieve external name space. Please ensure that the specified DLL contains a class named {className}");
        }
    }

    //This class is required for our assembly-unloader 
    public class CollectibleAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext() : base(isCollectible: true)
        { }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
