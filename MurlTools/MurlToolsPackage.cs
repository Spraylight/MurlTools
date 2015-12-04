using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using EnvDTE90;
using EnvDTE100;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.VCProjectEngine;

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

            // Add our command handlers for menu (commands must exist in the .vsct file)
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidShowHelpCmdSet, (int)PkgCmdIDList.cmdidShowHelp);
                MenuCommand menuItem = new MenuCommand(MenuItemCallbackShowHelp, menuCommandID);
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

        private bool IsVCProject()
        {
            EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            if (_applicationObject.ActiveWindow.Object != _applicationObject.ToolWindows.SolutionExplorer)
                return false;

            Array projs = _applicationObject.ActiveSolutionProjects as Array;
            if (projs.Length == 0)
            {
                return false;
            }

            string type = "Unknown";
            try
            {
                EnvDTE.Project proj = projs.GetValue(0) as EnvDTE.Project;
                if (HasProperty(proj.Properties, "Kind"))
                {
                    type = proj.Properties.Item("Kind").Value.ToString();
                }
                if (type.Equals("VCProject"))
                {
                    //VCProject vcProject = (VCProject)proj.Object;
                    return true;
                }
            }
            catch (Exception) { }

            return false;
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
            if (!IsVCProject())
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

        /*
         * See also http://www.mztools.com/articles/2006/MZ2006009.aspx for info about how to determine the code element at the cursor position.
         */
        private void MenuItemCallbackShowHelp(object sender, EventArgs e)
        {
            EnvDTE80.DTE2 dte = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            string searchText = "";
            try
            {
                Document activeDoc = dte.ActiveDocument;
                if (activeDoc != null && activeDoc.Selection != null)
                {
                    TextSelection sel = activeDoc.Selection as TextSelection;

                    if (sel.Text.Length == 0)
                    {
                        sel.WordLeft(true);
                        searchText = sel.Text;
                        sel.WordRight(true);
                        searchText = searchText + sel.Text;
                    }
                    else
                    {
                        searchText = sel.Text;
                    }
                }

                if (searchText.Length == 0)
                {
                    System.Diagnostics.Process.Start("http://murlengine.com/api");
                }
                else
                {
                    if (searchText.Length > 255)
                    {
                        searchText = searchText.Substring(0, 255);
                    }
                    System.Diagnostics.Process.Start("http://murlengine.com/api/en/search.php?q=" + System.Web.HttpUtility.UrlEncode(searchText));
                }
            }
            catch (Exception) 
            { }

            Debug.Print("Show Help: " + searchText);
        }

        private void MenuItemCallbackRefresh(object sender, EventArgs e)
        {
            if (!IsVCProject())
            {
                return;
            }

            RefreshSelectedFolder();
        }

        /// <summary>
        /// Refresh selected filter/folder callback
        /// </summary>
        void RefreshSelectedFolder()
        {
            
            EnvDTE80.DTE2 _applicationObject = GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;

            object[] selectedItems = (object[])_applicationObject.ToolWindows.SolutionExplorer.SelectedItems;
            foreach (EnvDTE.UIHierarchyItem selectedItem in selectedItems)
            {
                if (!(selectedItem.Object is EnvDTE.ProjectItem) || selectedItem.UIHierarchyItems == null)
                {
                    continue;
                }

                string path =  determinePath(selectedItem);
                if (path.Length > 0)
                {
                    RefreshSelectedFolder(selectedItem, path);
                }
            }
        }

        private string determinePath(EnvDTE.UIHierarchyItem selectedItem)
        {
            List<UIHierarchyItem> filterList = new List<UIHierarchyItem>();
            // try to determine path from files
            foreach (UIHierarchyItem item in selectedItem.UIHierarchyItems)
            {
                if (item.Object is EnvDTE.ProjectItem)
                {
                    if (item.UIHierarchyItems != null && item.UIHierarchyItems.Count > 0)
                    {
                        //filter element
                        filterList.Add(item);
                        continue;
                    }

                    ProjectItem prjItem = item.Object as ProjectItem;
                    Property prop = GetProperty(prjItem.Properties, "FullPath");
                    if (prop != null)
                    {
                        string res = Path.GetDirectoryName(prop.Value.ToString());
                        if (Directory.Exists(res))
                        {
                            return Path.GetDirectoryName(prop.Value.ToString());
                        }
                    }
                }
            }
            // try to determine path from sub folders/filters
            foreach (UIHierarchyItem item in filterList)
            {
                string path = determinePath(item);
                if (path.Length > 0)
                {
                    try
                    {
                        string res = path.Substring(0, path.Length - item.Name.Length-1);
                        if (res.EndsWith(selectedItem.Name) && Directory.Exists(res))
                        {
                            return res;
                        }
                    }
                    catch (Exception) { }
                }
            }
            // not able to determine path
            return "";
        }

        private void RefreshSelectedFolder(EnvDTE.UIHierarchyItem selectedItem, string dir)
        {
            List<string> pathList = new List<string>();
            List<UIHierarchyItem> filterList = new List<UIHierarchyItem>();
            string path = "";
            // Remove references which aren't exist
            foreach (UIHierarchyItem item in selectedItem.UIHierarchyItems)
            {
                if (!(item.Object is EnvDTE.ProjectItem))
                {
                    continue;
                }

                if (item.UIHierarchyItems != null && item.UIHierarchyItems.Count > 0)
                {
                    filterList.Add(item);
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
                else
                { 
                    //empty filter
                    filterList.Add(item);
                }
            }

            // Add existing files which are not in pathList
            if (dir.Length > 0)
            {
                string[] fileEntries = Directory.GetFiles(dir);
                foreach (string fileName in fileEntries)
                {
                    if (!pathList.Contains(fileName))
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

            // Add existing directories which are not listed as filters
            if (dir.Length > 0)
            {
                string[] dirEntries = Directory.GetDirectories(dir);
                List<string> fl = new List<string>();
                foreach (UIHierarchyItem item in filterList)
                {
                    fl.Add(dir+"\\"+item.Name);
                }
                foreach (string dirName in dirEntries)
                {
                    if (!fl.Contains(dirName))
                    {
                        ProjectItem filter = selectedItem.Object as EnvDTE.ProjectItem;
                        VCFilter vcFilter = (VCFilter)filter.Object;
                        if (vcFilter != null)
                        {
                            addNewFilterRecursive(vcFilter, dirName, dir);
                        }
                        // add
                        Debug.WriteLine(dirName);
                    }
                }
            }

            // recursively update sub dirs/filters
            foreach (UIHierarchyItem item in filterList)
            {
                string newPath = dir + "\\" + item.Name;
                if (Directory.Exists(newPath))
                {
                    RefreshSelectedFolder(item, newPath);
                }
            }
        }

        private void addNewFilterRecursive(VCFilter vcFilter, string dirName, string dir)
        {
            string filterName = dirName.Substring(dir.Length + 1);
            VCFilter newFilter = vcFilter.AddFilter(filterName);
            
            // add files
            string[] fileEntries = Directory.GetFiles(dirName);
            foreach (string file in fileEntries)
            {
                newFilter.AddFile(file);
            }

            // add directories as filter
            string[] dirEntries = Directory.GetDirectories(dirName);
            foreach (string d in dirEntries)
            {
                addNewFilterRecursive(newFilter, d, dirName);
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
