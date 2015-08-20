## Murl Tools
### A Visual Studio extension

Download from the
[Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/TBD)

## Summary

The Murl Tools extension adds the following two commands to the context menu of the Solution Explorer:
- "Duplicate" to create a copy of the selected file(s).
- "Refresh" to add missing files that exist in the folder but not in the selected filter.
- The extension additionally adds a Remove/Delete dialog when deleting files (VisualC projects only).

These features are especially useful for cross platform projects where the source and data files are not stored inside of the project folder 
(e.g. [Murl Engine](http://murlengine.com)).

### Duplicate File(s)

To create a copy of an existing file in the Solution Explorer you usually select the file and press CTRL-C and CTRL-V.
While this is working for C# and VB projects, it does not work for VisualC/C++ projects.

The extension adds a Duplicate command to the context menu of the Solution Explorer.
The command creates a copy of the selected file and adds it to project within he same filter group..

![Duplicate](screenshots/duplicate.png)

You can also vote for the feature [here](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/9145699-solution-explorer-should-support-ctrl-c-ctrl-v-in).

### Refresh Folder

This command adds references for all missing files that exist in the folder but not in the selected filter.
Note, that this command only makes sense when your filter hierarchy matches to real folders in the file system.

![Refresh](screenshots/refresh.png)

### Remove/Delete Dialog

When I select one or more files in the solution explorer and press the delete key, I usually get a dialog where I can choose to remove or delete the file:

Visual Studio displays a remove/delete file dialog when you try to delete one or more files from a VisualC/C++ project.
The dialog lets you to decide if only the file reference should be removed or if also the file should be permanently deleted in the file system.
Unfortunately the dialog only shows up if the selected files are stored inside of the project folder which is not the case for e.g. cross platform projects where multiple projects share the same source code files.

The extension fixes this weird behavior and always displays a Remove/Delete dialog.

![Remove Delete Dialog](screenshots/remove_delete_dialog.png)

You can vote for the feature [here](http://visualstudio.uservoice.com/forums/121579-visual-studio/suggestions/9146353-remove-delete-dialog-should-show-up-when-deleting).

### Support or Contact

Having trouble with the extension? Check out our [forum](http://murlengine.com/forum) or contact us per [email](spraylight.at/?splpage=contact).
