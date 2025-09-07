using System.Windows.Input;

namespace XAUMobile.Controls
{
    public partial class CallToAction : ContentView
    {
        private TaskCompletionSource<bool>? _tcs;
        private TaskCompletionSource<bool>? _dialogTcs;

        public CallToAction()
        {
            InitializeComponent();
            ClosePopupCommand = new Command(async () => await HideAction());
            LeftButtonCommand = new Command(async () => await OnLeftButtonClicked());
            RightButtonCommand = new Command(async () => await OnRightButtonClicked());
            BindingContext = this;
            this.SizeChanged += OnSizeChanged;
            Application.Current.PageAppearing += OnPageAppearing;
        }

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            if (sender is ContentView contentView && contentView.Width > 0)
            {
                double padding = 40;
                CallToActionFrame.WidthRequest = contentView.Width - padding;
            }
        }

        private async void OnPageAppearing(object? sender, Page page)
        {
            if (this.IsVisible)
            {
                await HideAction();
            }
        }

        public string Title
        {
            get => ActionStatus.Text;
            set => ActionStatus.Text = value;
        }

        public string Message
        {
            get => MessageLabel.Text;
            set => MessageLabel.Text = value;
        }

        public string IconSource
        {
            get => ActionIcon.Source.ToString() ?? string.Empty;
            set => ActionIcon.Source = value;
        }

        public Color TitleColor
        {
            get => ActionStatus.TextColor;
            set => ActionStatus.TextColor = value;
        }

        public ICommand ClosePopupCommand { get; set; }
        public ICommand LeftButtonCommand { get; set; }
        public ICommand RightButtonCommand { get; set; }

        public async Task ShowAction()
        {
            this.IsVisible = true;
            await DimBackground.FadeTo(0.4, 250);
            CallToActionFrame.IsVisible = true;
            await CallToActionFrame.ScaleTo(1.0, 250, Easing.CubicInOut);
        }

        public async Task HideAction()
        {
            await CallToActionFrame.ScaleTo(0.0, 250, Easing.CubicInOut);
            await DimBackground.FadeTo(0, 250);
            CallToActionFrame.IsVisible = false;
            this.IsVisible = false;

            if (_tcs?.Task != null && !_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(true);
                _tcs = null;
            }

            if (_dialogTcs?.Task != null && !_dialogTcs.Task.IsCompleted)
            {
                _dialogTcs.TrySetResult(false);
                _dialogTcs = null;
            }

            await Task.Delay(500);
        }

        // Show a simple message with only the action button visible
        public async Task ShowMessage(string title, string message, string iconSource, Color titleColor)
        {
            Title = title;
            Message = message;
            IconSource = iconSource;
            TitleColor = titleColor;

            ActionButton.IsVisible = true;  // Only action button
            LeftActionButton.IsVisible = false;
            RightActionButton.IsVisible = false;
            DialogButtonStack.IsVisible = false;  // Hide dialog buttons

            _tcs = new TaskCompletionSource<bool>();

            await ShowAction();
            await _tcs.Task;
            await HideAction();
        }

        // Show a dialog with left and right buttons
        public async Task<bool> ShowDialog(string title, string message, string leftButtonText, string rightButtonText, string iconSource, Color titleColor)
        {
            Title = title;
            Message = message;
            IconSource = iconSource;
            TitleColor = titleColor;

            LeftActionButton.Text = leftButtonText;
            RightActionButton.Text = rightButtonText;

            ActionButton.IsVisible = false;  // Hide action button
            DialogButtonStack.IsVisible = true;
            LeftActionButton.IsVisible = true;
            RightActionButton.IsVisible = true;  // Show dialog buttons

            _dialogTcs = new TaskCompletionSource<bool>();

            await ShowAction();
            return await _dialogTcs.Task;
        }

        // Show a popup with no buttons
        public async Task ShowPopup(string title, string message, string iconSource, Color titleColor)
        {
            Title = title;
            Message = message;
            IconSource = iconSource;
            TitleColor = titleColor;

            ActionButton.IsVisible = false;  // Hide action button
            DialogButtonStack.IsVisible = false;  // Hide dialog buttons

            await ShowAction();
            await Task.Delay(2000);  // Auto-hide after 2 seconds
            await HideAction();
        }

        private async Task OnLeftButtonClicked()
        {
            _dialogTcs?.TrySetResult(true);
            await HideAction();
        }

        private async Task OnRightButtonClicked()
        {
            _dialogTcs?.TrySetResult(false);
            await HideAction();
        }
    }
}