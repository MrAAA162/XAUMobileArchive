namespace XAUMobile.Controls
{
    public static class CallToActionHelper
    {
        private static Queue<Func<Task>> _actionQueue = new Queue<Func<Task>>();
        private static bool _isShowingAction = false;

        public static async Task ShowMessage(CallToAction callToActionControl, string title, string message, string iconSource, string color)
        {
            if (Application.Current != null)
            {
                _actionQueue.Enqueue(async () =>
                {
                    var colorResource = (Color)Application.Current.Resources[color];
                    await callToActionControl.ShowMessage(title, message, iconSource, colorResource);
                    await callToActionControl.HideAction();
                });

                await ProcessQueue();
            }
        }

        public static async Task ShowPopup(CallToAction callToActionControl, string title, string message, string iconSource, string color)
        {
            if (Application.Current != null)
            {
                _actionQueue.Enqueue(async () =>
                {
                    var colorResource = (Color)Application.Current.Resources[color];
                    await callToActionControl.ShowPopup(title, message, iconSource, colorResource);
                    await callToActionControl.HideAction();
                });

                await ProcessQueue();
            }
        }

        public static async Task<bool> ShowDialog(CallToAction callToActionControl, string title, string message, string leftButtonText, string rightButtonText, string iconSource, string color)
        {
            if (Application.Current != null)
            {
                var dialogTcs = new TaskCompletionSource<bool>();

                _actionQueue.Enqueue(async () =>
                {
                    var colorResource = (Color)Application.Current.Resources[color];
                    bool result = await callToActionControl.ShowDialog(title, message, leftButtonText, rightButtonText, iconSource, colorResource);
                    dialogTcs.SetResult(result);
                    await callToActionControl.HideAction();
                });

                await ProcessQueue();
                return await dialogTcs.Task;
            }

            return false;
        }

        private static async Task ProcessQueue()
        {
            if (_isShowingAction || _actionQueue.Count == 0)
                return;

            _isShowingAction = true;

            var actionToShow = _actionQueue.Dequeue();
            await actionToShow();

            _isShowingAction = false;

            if (_actionQueue.Count > 0)
            {
                await ProcessQueue();
            }
        }
    }
}