using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Spreads {


    public static class Program {

        // when running as console app, init Bootstrapper
        static Program() {
            Console.WriteLine(Bootstrapper.Instance.AppFolder);
        }

        public static void Main() {
            // dll must be compressed by running this project as .exe

            //Loader.CompressResource(@"Path to dll");
            //Loader.CompressFolder(@"Path to folder");
            //Loader.ExtractFolder(@"... \lib\msvcrt\x64.zip", @"destination path");
            
            Console.ReadLine();
        }
    }

    public class Bootstrapper {

        private static readonly Bootstrapper instance = new Bootstrapper();
        public static Bootstrapper Instance {
            get {
                return instance;
            }
        }

        // Bootstrapper extracts dlls embedded as resource to the app folder.
        // Those dlls could contain other embedded dlls, which are extracted recursively as Russian dolls.
        // Bootstrapper overwrites existing files 

        

        static Bootstrapper() {


            instance.Bootstrap<Loader>(
                null, // new[] { "yeppp" },
                null, // new[] { "Newtonsoft.Json.dll" },
                null,
                null,
                () => {
#if DEBUG
                    Console.WriteLine("Pre-copy action");
#endif
                },
                () => {
                //Yeppp.Library.Init();
#if DEBUG
                Console.WriteLine("Post-copy action");
#endif
                },
                () => {
                //Yeppp.Library.Release();
            });

            AppDomain.CurrentDomain.AssemblyResolve +=
                new ResolveEventHandler((object sender, ResolveEventArgs args) => {
                    var an = new AssemblyName(args.Name);
                    return instance.managedLibraries[an.Name + ".dll"];
                });
            //new ResolveEventHandler(Loader.ResolveManagedAssembly);
        }


        private string _baseFolder;
        private string _dataFolder;

        // Botstrap self
        private Bootstrapper() {

            _baseFolder = Environment.UserInteractive
                ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                : Instance.AssemblyDirectory;

            _dataFolder = Path.Combine(_baseFolder, rootFolder, dataSubFolder);

            if (!Directory.Exists(AppFolder)) {
                Directory.CreateDirectory(AppFolder);
            }

            if (!Directory.Exists(Path.Combine(AppFolder, "x32"))) {
                Directory.CreateDirectory(Path.Combine(AppFolder, "x32"));
            }

            if (!Directory.Exists(Path.Combine(AppFolder, "x64"))) {
                Directory.CreateDirectory(Path.Combine(AppFolder, "x64"));
            }

            if (!Directory.Exists(AppFolder)) {
                Directory.CreateDirectory(AppFolder);
            }

            if (!Directory.Exists(ConfigFolder)) {
                Directory.CreateDirectory(ConfigFolder);
            }

            if (!Directory.Exists(DataFolder)) {
                Directory.CreateDirectory(DataFolder);
            }

            if (!Directory.Exists(TempFolder)) {
                Directory.CreateDirectory(TempFolder);
            }
        }


        private const string rootFolder = "Spreads";
        private const string configSubFolder = "config";
        private const string appSubFolder = "bin";
        private const string dataSubFolder = "data";
        // TODO next two only in user interactive mode
        private const string docFolder = "Docs";
        private const string gplFolder = "Libraries";



        public string AssemblyDirectory {
            get {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        internal string BaseFolder {
            get {
                return _baseFolder;
            }
            set {
                _baseFolder = value;
            }
        }

        internal string ConfigFolder {
            get {
                return Path.Combine(_baseFolder, rootFolder, configSubFolder);
            }
        }

        internal string AppFolder {
            get {
                return Path.Combine(_baseFolder, rootFolder, appSubFolder);
            }
        }

        internal string DataFolder {
            get {
                return _dataFolder;
            }
            set {
                _dataFolder = value;
            }
        }

        private string _tmpFolder = null;
        internal string TempFolder {
            get {
                if (_tmpFolder == null) {
                    _tmpFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                }
                return _tmpFolder;
            }
        }

        // keep references to avoid GC
        internal Dictionary<string, NativeLibrary> nativeLibraries = new Dictionary<string, NativeLibrary>();
        // this will block other dlls with the same name from loading
        // TODO do not store managed, return to resolve method
        internal Dictionary<string, Assembly> managedLibraries = new Dictionary<string, Assembly>();
        private List<Action> DisposeActions = new List<Action>();

        /// <summary>
        /// From assembly with type T load libraries
        /// </summary>
        public void Bootstrap<T>(string[] nativeLibNames = null,
            string[] managedLibNames = null,
            string[] resourceNames = null,
            string[] serviceNames = null, // ensure these .exes are running
            Action preCopyAction = null,
            Action postCopyAction = null,
            Action disposeAction = null) {

            if (preCopyAction != null) preCopyAction.Invoke();

            if (nativeLibNames != null) {
                foreach (var nativeName in nativeLibNames) {
                    if (nativeLibraries.ContainsKey(nativeName)) continue;
                    nativeLibraries.Add(nativeName, Loader.LoadNativeLibrary<T>(nativeName));
                }
            }

            if (managedLibNames != null) {
                foreach (var managedName in managedLibNames) {
                    if (managedLibraries.ContainsKey(managedName)) continue;
                    Trace.Assert(managedName.EndsWith(".dll"));
                    //if (!Environment.UserInteractive){
                    //    Debugger.Launch();
                    //}
                    managedLibraries.Add(managedName, Loader.LoadManagedDll<T>(managedName));
                }
            }


            if (resourceNames != null) {
                foreach (var resourceName in resourceNames) {
                    Loader.ExtractResource<T>(resourceName);
                }
            }

            if (serviceNames != null) {
                foreach (var serviceName in serviceNames) {
                    Trace.Assert(serviceName.EndsWith(".exe"));
                    // TODO run exe as singletone process by path
                    throw new NotImplementedException("TODO start exe process");
                }
            }

            if (postCopyAction != null) postCopyAction.Invoke();

            DisposeActions.Add(disposeAction);
        }



        ~Bootstrapper() {
            if (DisposeActions.Count > 0) {
                foreach (var action in DisposeActions) {
                    action.Invoke();
                }
            }
            foreach (var loadedLibrary in nativeLibraries) {
                if (loadedLibrary.Value != null) loadedLibrary.Value.Dispose();
            }
            try {
                Directory.Delete(Instance.TempFolder, true);
            } catch {
            }
        }
    }
}
