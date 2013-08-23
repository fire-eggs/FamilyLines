/*
 * Family.Show derived code provided under MS-PL license.
 */
using System.Windows;
using System.Windows.Shell;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// KBR 12/31/2012 - Originally a helper class for the WindowsAPICodePack Taskbar manager, I've done a 
    /// Q&D rework to use the .NET 4.0 support instead.
    /// TODO: make this disappear completely in a future release.
    /// </summary>
    internal class TaskBar
    {
        #region Properties

        public static TaskBar Current { get; private set; }

        private TaskbarItemInfo TBI { get { return _window.TaskbarItemInfo; } }

        #endregion

        #region Initialization

        private TaskBar()
        {
        }

        private JumpList _jumpList;
        private Window _window;

        private TaskBar(Window mainWindow, string appId)
        {
            _window = mainWindow;

            if (_window.TaskbarItemInfo == null)
            {
                var tbi = new TaskbarItemInfo {Description = "FamilyLines Genealogy Program"};
                _window.TaskbarItemInfo = tbi;
            }

            _jumpList = new JumpList();
        }

        /// <summary>
        /// Creates a single task bar instance.
        /// </summary>
        /// <returns></returns>
        public static TaskBar Create(Window mainWindow, string appId, params JumpTask[] tasks)
        {
            Current = new TaskBar(mainWindow, appId);

            // NOTE: order is important!!! The tasks must be added BEFORE SetJumpList is called!!
            Current._jumpList.JumpItems.AddRange(tasks);

            JumpList.SetJumpList(Application.Current, Current._jumpList);
            return Current;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Set the progress of a Windows 7 taskbar.
        /// </summary>
        /// <param name="progress"></param>
        public virtual void Progress(int progress)
        {
            Current.TBI.ProgressValue = progress;
            Current.TBI.ProgressState = TaskbarItemProgressState.Normal;
        }

        /// <summary>
        /// Set the progress of a Windows 7 taskbar to indeterminate.
        /// </summary>
        public virtual void Loading()
        {
            Current.TBI.ProgressState = TaskbarItemProgressState.Indeterminate;
        }

        /// <summary>
        /// Clear the progress of a Windows 7 taskbar.
        /// </summary>
        public virtual void Restore()
        {
            Current.TBI.ProgressState = TaskbarItemProgressState.None;
        }

        #endregion
    }
}
