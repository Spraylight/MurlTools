using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio;

namespace Spraylight.MurlTools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Autoload as soon as a solution exists.
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidMurlToolsPkgString)]
    public sealed class MurlToolsPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public MurlToolsPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidDuplicateFileCmdSet, (int)PkgCmdIDList.cmdidDuplicateCmd);
                MenuCommand menuItem = new MenuCommand(MenuItemCallbackDuplicate, menuCommandID);
                mcs.AddCommand(menuItem);
            }

            // Add our command handlers for menu (commands must exist in the .vsct file)
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidRefreshCmdSet, (int)PkgCmdIDList.cmdidRefreshCmd);
                MenuCommand menuItem = new MenuCommand(MenuItemCallbackRefresh, menuCommandID);
                mcs.AddCommand(menuItem);
            }

            // Override Edit.Delete command
            _applicationObject = (DTE)GetService(typeof(DTE));
            var command = _applicationObject.Commands.Item("Edit.Delete");
            _removeEvent = _applicationObject.Events.CommandEvents[command.Guid, command.ID];
            _removeEvent.BeforeExecute += OnBeforeDeleteCommand;
        }
        private EnvDTE.DTE _applicationObject;
        private CommandEvents _removeEvent;
        #endregion

        /// <summary>
        /// Helper method: Store all selected project items in the selectedProjectItems array.
        /// </summary>
        private void getSelectedItems()
        {
            EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            UIHierarchy solutionExplorer = _applicationObject.ToolWindows.SolutionExplorer;
            Array selectedItems = solutionExplorer.SelectedItems as Array;

            selectedProjectItems.Clear();

            if (selectedItems != null)
            {
                foreach (UIHierarchyItem selItem in selectedItems)
                {
                    if (selItem.Object is EnvDTE.ProjectItem)
                    {
                        ProjectItem prjItem = selItem.Object as ProjectItem;
                        if (prjItem != null && prjItem.ProjectItems.Count == 0)
                        {
                            if (HasProperty(prjItem.Properties, "FullPath"))
                            {
                                selectedProjectItems.Add(prjItem);
                            }
                        }
                    }
                    else if (selItem.Object is EnvDTE.Project)
                    { }
                    else if (selItem.Object is EnvDTE.Solution)
                    { }
                }
            }
        }

        /// <summary>
        /// Called on Edit.Delete command events
        /// </summary>
        /// <param name="Guid"></param>
        /// <param name="ID"></param>
        /// <param name="CustomIn"></param>
        /// <param name="CustomOut"></param>
        /// <param name="CancelDefault"></param>
        private void OnBeforeDeleteCommand(string Guid, int ID, Object CustomIn, Object CustomOut, ref bool CancelDefault)
        {
            EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            if (_applicationObject.ActiveWindow.Object != _applicationObject.ToolWindows.SolutionExplorer)
                return;

            Array projs = _applicationObject.ActiveSolutionProjects as Array;
            if (projs.Length == 0)
            {
                return;
            }

            string type = "Unknown";
            try
            {
                EnvDTE.Project proj = projs.GetValue(0) as EnvDTE.Project;
                if (HasProperty(proj.Properties,"Kind"))
                    type = proj.Properties.Item("Kind").Value.ToString();
            }
            catch (Exception) { }

            if (!type.Equals("VCProject"))
            {
                return;
            }

            getSelectedItems();

            if (selectedProjectItems.Count == 0)
                return;
           
            DlgRemoveDelete dlg = new DlgRemoveDelete();
            
            var m = dlg.ShowModal();
            int result = dlg.getResult();
            
            if (result == DlgRemoveDelete.DELETE)
            {
                for (int i = selectedProjectItems.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        selectedProjectItems[i].Delete();
                    }
                    catch (Exception) { }
                }
            }
            else if (result == DlgRemoveDelete.REMOVE)
            {
                for (int i = selectedProjectItems.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        selectedProjectItems[i].Remove();
                    }
                    catch (Exception) { }
                }
            }

            CancelDefault = true;
        }

        /// <summary>
        /// Duplicate File Callback
        /// </summary>
        private void MenuItemCallbackDuplicate(object sender, EventArgs e)
        {
            getSelectedItems();
            duplicateFiles();
            SelectLastAdded();
            EnterRenameState();
        }

        private void duplicateFiles()
        {
            lastAdded = null;

            if (selectedProjectItems.Count == 0)
                return;

            EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            for (int i = 1; i <= _applicationObject.Solution.Projects.Count; i++)
            {
                Project project = _applicationObject.Solution.Projects.Item(i);
                ProjectItems projItems = project.ProjectItems;
                duplicateFiles(projItems);
            }
        }

        private void duplicateFiles(ProjectItems projItems)
        {
            ProjectItem projItem;
            for (int i = 1; i <= projItems.Count; i++)
            {
                projItem = projItems.Item(i);
                Debug.Write(projItem.Name + "  " + projItem.Kind);
                try
                {
                    if (projItem.ProjectItems.Count > 0)
                    {
                        duplicateFiles(projItem.ProjectItems);
                    }
                    else if (selectedProjectItems.Contains(projItem))
                    {
                        selectedProjectItems.Remove(projItem);
                        String newPath = copyFile(projItem);
                        if (newPath != null)
                        {
                            try
                            {
                                lastAdded = projItems.AddFromFile(newPath);
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        // determine the new name and create a copy of the file
        private String copyFile(ProjectItem projItem)
        {
            string path = projItem.get_FileNames(1);

            if (!File.Exists(path))
                return null;

            int index = path.LastIndexOf('.');
            if (index < 0)
                return null;

            string newBase = path.Substring(0, index) + " - Copy";
            string newExt = path.Substring(index);
            string newPath = newBase + newExt;
            index = 1;
            while (File.Exists(newPath))
            {
                index++;
                newPath = newBase + " (" + index + ")" + newExt;
            }
            File.Copy(path, newPath);
            return newPath;
        }

        private void SelectLastAdded()
        {
            if (lastAdded != null)
            {
                List<UIHierarchyItems> itemStack = new List<UIHierarchyItems>();

                EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
                UIHierarchy solutionExplorer = _applicationObject.ToolWindows.SolutionExplorer;
                UIHierarchyItems items = solutionExplorer.UIHierarchyItems;

                itemStack.Add(solutionExplorer.UIHierarchyItems);
                while (itemStack.Count > 0)
                {
                    int lastIndex = itemStack.Count - 1;
                    items = itemStack[lastIndex];
                    itemStack.RemoveAt(lastIndex);

                    foreach (UIHierarchyItem item in items)
                    {
                        if (item.Object == lastAdded)
                        {
                            item.Select(vsUISelectionType.vsUISelectionTypeSelect);
                            return;
                        }
                        if (item.UIHierarchyItems != null)
                        {
                            itemStack.Add(item.UIHierarchyItems);
                        }
                    }
                }
            }
        }

        private void EnterRenameState()
        {
            EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            _applicationObject.ExecuteCommand("File.Rename");
        }

        private void MenuItemCallbackRefresh(object sender, EventArgs e)
        {
            RefreshSelectedFolder();
        }

        /// <summary>
        /// Refresh selected filter/folder callback
        /// </summary>
        void RefreshSelectedFolder()
        {
            List<string> pathList = new List<string>();
            EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;

            object[] selectedItems = (object[])_applicationObject.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (EnvDTE.UIHierarchyItem selectedItem in selectedItems)
            {
                if (!(selectedItem.Object is EnvDTE.ProjectItem) || selectedItem.UIHierarchyItems == null)
                {
                    continue;
                }

                pathList.Clear();
                string path = "";
                // Remove references which are dont exist
                foreach (UIHierarchyItem item in selectedItem.UIHierarchyItems)
                {
                    if (!(item.Object is EnvDTE.ProjectItem))
                    {
                        continue;
                    }

                    if (item.UIHierarchyItems != null && item.UIHierarchyItems.Count > 0)
                    {
                        //filter element
                        continue;
                    }

                    ProjectItem prjItem = item.Object as ProjectItem;
                    Property prop = GetProperty(prjItem.Properties, "FullPath");
                    if (prop != null)
                    {
                        path = prop.Value.ToString();
                        if (!File.Exists(path))
                        {
                            // remove prjItem if path does not exist
                            try
                            {
                                prjItem.Remove();
                            }
                            catch (Exception) { }
                        }
                        else
                        {
                            // else store in pathList
                            pathList.Add(path);
                        }
                    }
                }

                // use path of last removed reference if no element is left in filter
                if (pathList.Count > 0)
                {
                    path = pathList[0];
                }

                // Add existing files which are not in pathList
                if (path.Length > 0)
                {
                    string dir = Path.GetDirectoryName(path);
                    string[] fileEntries = Directory.GetFiles(dir);
                    foreach (string fileName in fileEntries)
                    {
                        if (!pathList.Contains(fileName) )
                        {
                            ProjectItem filter = selectedItem.Object as EnvDTE.ProjectItem;
                            if (filter != null && filter.ProjectItems != null)
                            {
                                try
                                {
                                    filter.ProjectItems.AddFromFile(fileName);
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                }
            }
        }

        private Property GetProperty(Properties properties, string propertyName)
        {
            if (properties != null)
            {
                foreach (Property item in properties)
                {
                    if (item != null && item.Name == propertyName)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        private bool HasProperty(Properties properties, string propertyName)
        {
            if (GetProperty(properties, propertyName) != null)
                return true;
            return false;
        }

        /// <summary>
        /// Debug only
        /// </summary>
        /// <param name="s"></param>
        void writeDebugIntoOutputPane(string s)
        {
            EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            // Retrieve the Output window.
            OutputWindow outputWin = _applicationObject.ToolWindows.OutputWindow;

            // Find the "Test Pane" Output window pane; if it doesn't exist, 
            // create it.
            OutputWindowPane pane = null;
            try
            {
                pane = outputWin.OutputWindowPanes.Item("Test Pane");
            }
            catch
            {
                pane = outputWin.OutputWindowPanes.Add("Test Pane");
            }

            pane.OutputString(s + "\n");
        }

        /// <summary>
        /// Variable Declarations
        /// </summary>
        private List<ProjectItem> selectedProjectItems = new List<ProjectItem>();
        private ProjectItem lastAdded = null;


    }
}
