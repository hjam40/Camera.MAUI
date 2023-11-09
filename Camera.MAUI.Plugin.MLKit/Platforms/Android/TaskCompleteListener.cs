using Android.Gms.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Camera.MAUI.Plugin.MLKit
{
    internal class TaskCompleteListener : Java.Lang.Object, IOnCompleteListener
    {
        private readonly TaskCompletionSource<Java.Lang.Object> _taskCompletionSource;

        public TaskCompleteListener(TaskCompletionSource<Java.Lang.Object> tcs)
        {
            _taskCompletionSource = tcs;
        }

        public void OnComplete(global::Android.Gms.Tasks.Task task)
        {
            if (task.IsCanceled)
            {
                _taskCompletionSource.SetCanceled();
            }
            else if (task.IsSuccessful)
            {
                _taskCompletionSource.SetResult(task.Result);
            }
            else
            {
                _taskCompletionSource.SetException(task.Exception);
            }
        }
    }
}