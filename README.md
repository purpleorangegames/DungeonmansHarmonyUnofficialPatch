# DungeonmansHarmonyUnofficialPatch
Dungeonmans Unofficial Patch using Harmony


##Patch 1 for Dungeonmans 1.81e

Fix adventure map in hotbar
+Will only consume the map if requirements are met
+Will only show quantity one as maps are unique
+Will display correct tooltip
+Will be removed from hotbar after being consumed

##Patch 2 for Dungeonmans 1.81e

Fix adventure map dungeon generation on overworld
+Will no longer generate an adventure map with coordinates equal to an already put dungeon
+Will check before placing dungeon if there is another already at the same coordinates and place it nearby instead

##Patch 3 for Dungeonmans 1.81e

Fix "The Way Home" skill
+Skill is now set as not passive, so you can grab and hover it to the hotbar
+Fixed tooltip to show the correct icon
+Fixed the image to be a square (non passive skill type of icon)

##Path 4 for Dungeonmans 1.81e

Two additional fixes and added Harmony to Dungeonmans so Harmony patches can be loadede from AssemblyMods folder
+Fix adventure map in the hotbar being used with keyboards numbers **sorry I forgot to test that**
+Fix for when a map has the same location of a dungeon, now it expands the search range if necessary to find a suitable nearby location
+Reverted all changes from the executable and moved them to Harmony
+Now there is no need to overwrite the "field_work_perks.txt" file, the changes are being made in Harmony
+Changed the texture to have a different name so there is no need to overwrite ("power_icons_2.xnb"), the patch will load this one instead
+A log will be generated regarding the DLLs loaded in the AssemblyMods folder, the log is truncated so it nevers becomes big
+All patches in Harmony are either Prefix or Postfix, some extensions were also created

*****************************************************************************************
Added to "dmGame.cs" at namespace "DungeonMans"
```
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class StaticConstructorOnStartup : Attribute
    {

    }

    public class Mod
    {
        public Mod()
        {

        }
    }

    public class ModAssemblyHandler
    {
        public ModAssemblyHandler(String newPath)
        {
            if (!Directory.Exists(newPath))
                Directory.CreateDirectory(newPath);
            this.modDirPath = newPath;
            globalResolverIsSet = false;
            dmGame.logWrite("ModAssemblyHandler initialized","AssemblyMods\\",true);
        }

        public void LoadAssembly()
        {
            if (!ModAssemblyHandler.globalResolverIsSet)
            {
                ResolveEventHandler @object = (object obj, ResolveEventArgs args) => Assembly.GetExecutingAssembly();
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += @object.Invoke;
                ModAssemblyHandler.globalResolverIsSet = true;
            }
            DirectoryInfo directoryInfo = new DirectoryInfo(modDirPath);
            if (!directoryInfo.Exists)
            {
                dmGame.logWrite("Directory doesn't exists", "AssemblyMods\\");
                return;
            }
            foreach (FileInfo fileInfo in directoryInfo.GetFiles("*.*", SearchOption.AllDirectories))
            {
                if (!(fileInfo.Extension.ToLower() != ".dll"))
                {
                    dmGame.logWrite("Found file " + fileInfo.Name, "AssemblyMods\\");
                    Assembly assembly = null;
                    try
                    {
                        byte[] rawAssembly = File.ReadAllBytes(fileInfo.FullName);
                        string fileName = Path.Combine(fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(fileInfo.FullName)) + ".pdb";
                        FileInfo fileInfo2 = new FileInfo(fileName);
                        if (fileInfo2.Exists)
                        {
                            byte[] rawSymbolStore = File.ReadAllBytes(fileInfo2.FullName);
                            assembly = AppDomain.CurrentDomain.Load(rawAssembly, rawSymbolStore);
                        }
                        else
                        {
                            assembly = AppDomain.CurrentDomain.Load(rawAssembly);
                        }
                    }
                    catch (Exception ex)
                    {
                        dmGame.logWrite("Exception loading " + fileInfo.Name + ": " + ex.ToString(), "AssemblyMods\\");
                        break;
                    }
                    if (assembly != null)
                    {
                        if (this.AssemblyIsUsable(assembly))
                        {
                            this.loadedAssemblies.Add(assembly);
                            dmGame.logWrite("Loading assembly " + fileInfo.Name, "AssemblyMods\\");
                            if (fileInfo.Name!="0Harmony.dll")
                            {
                                Type[] types = assembly.GetExportedTypes();
                                foreach (Type t in types)
                                {
                                    dmGame.logWrite("Loading assembly type " + t.Name, "AssemblyMods\\");
                                    if (!t.IsAbstract && !t.IsSealed)
                                        Activator.CreateInstance(t);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool AssemblyIsUsable(Assembly asm)
        {
            try
            {
                asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(string.Concat(new object[]
                {
                    "ReflectionTypeLoadException getting types in assembly ",asm.GetName().Name,": ",ex
                }));
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("Loader exceptions:");
                if (ex.LoaderExceptions != null)
                {
                    foreach (Exception ex2 in ex.LoaderExceptions)
                    {
                        stringBuilder.AppendLine("   => " + ex2.ToString());
                    }
                }
                dmGame.logWrite(stringBuilder.ToString(), "AssemblyMods\\");
                return false;
            }
            catch (Exception ex3)
            {
                dmGame.logWrite("Exception getting types in assembly " +asm.GetName().Name+": " +ex3,"AssemblyMods\\");
                return false;
            }
            return true;
        }

        public String modDirPath;

        public List<Assembly> loadedAssemblies = new List<Assembly>();

        private static bool globalResolverIsSet;
    }
```

*****************************************************************************************
Added at the beggining of the class "dmGame"
```
	public ModAssemblyHandler modAssemblyHandler;

        public static void logWrite(string logMessage, string directory="", bool initial=false)
        {
            string m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                if (initial)
                    File.Delete(m_exePath + "\\" + directory + "log.txt");

                using (StreamWriter txtWriter = File.AppendText(m_exePath + "\\" + directory + "log.txt"))
                {
                    txtWriter.Write("\r\nLog Entry : ");
                    txtWriter.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    txtWriter.WriteLine("  :{0}", logMessage);
                    txtWriter.WriteLine("-------------------------------");
                }
            }
            catch (Exception ex)
            {
            }
        }
```

*****************************************************************************************
Added at the beginning of "public dmGame()"
```
            modAssemblyHandler = new ModAssemblyHandler("AssemblyMods");
            modAssemblyHandler.LoadAssembly();
```
