using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else

using DecodeDataType = System.Object;

#endif

namespace Camera.MAUI.Plugin
{
    public abstract class PluginDecoder<TOptions, TResult> : BindableObject, IPluginDecoder<TOptions, TResult>
        where TOptions : IPluginDecoderOptions
        where TResult : IPluginResult
    {
        #region Public Fields

        public static readonly BindableProperty OnDecodedCommandProperty = BindableProperty.Create(nameof(OnDecodedCommand), typeof(ICommand), typeof(PluginDecoder<TOptions, TResult>), null, defaultBindingMode: BindingMode.TwoWay);
        public static readonly BindableProperty OptionsProperty = BindableProperty.Create(nameof(Options), typeof(TOptions), typeof(PluginDecoder<TOptions, TResult>), default, propertyChanged: OptionsChanged);
        public static readonly BindableProperty ResultsProperty = BindableProperty.Create(nameof(Results), typeof(TResult[]), typeof(PluginDecoder<TOptions, TResult>), null, BindingMode.OneWayToSource);
        public static readonly BindableProperty VibrateOnDetectedProperty = BindableProperty.Create(nameof(VibrateOnDetected), typeof(bool), typeof(PluginDecoder<TOptions, TResult>), true, defaultBindingMode: BindingMode.TwoWay);

        #endregion Public Fields

        #region Protected Constructors

        protected PluginDecoder()
        {
            Options = (TOptions)Activator.CreateInstance(typeof(TOptions));
        }

        #endregion Protected Constructors

        #region Public Events

        public event PluginDecoderResultHandler Decoded;

        #endregion Public Events

        #region Public Properties

        public ICommand OnDecodedCommand
        {
            get { return (ICommand)GetValue(OnDecodedCommandProperty); }
            set { SetValue(OnDecodedCommandProperty, value); }
        }

        public TOptions Options
        {
            get { return (TOptions)GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        public TResult[] Results
        {
            get { return (TResult[])GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }

        public bool VibrateOnDetected
        {
            get => (bool)GetValue(VibrateOnDetectedProperty);
            set => SetValue(VibrateOnDetectedProperty, value);
        }

        #endregion Public Properties

        #region Public Methods

        public abstract void ClearResults();

        public abstract void Decode(DecodeDataType data);

        #endregion Public Methods

        #region Protected Methods

        protected void OnDecoded(PluginDecodedEventArgs args)
        {
            if (VibrateOnDetected)
            {
                try
                {
                    Vibration.Vibrate(200);
                }
                catch
                { }
            }
            Decoded?.Invoke(this, args);
            OnDecodedCommand?.Execute(args);
        }

        protected abstract void OnOptionsChanged(object oldValue, object newValue);

        #endregion Protected Methods

        #region Private Methods

        private static void OptionsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null && oldValue != newValue && bindable is PluginDecoder<TOptions, TResult> decoder)
            {
                decoder.OnOptionsChanged(oldValue, newValue);
            }
        }

        #endregion Private Methods
    }
}