﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace xLib.UI_Propertys
{
    public interface IUI_PropertyEvents
    {
        void Select();
    }

    public interface IUI_PropertyValue<TValue>
    {
        TValue Value { get; set; }
    }

    public interface IUI_PropertyRequest<TRequest>
    {
        TRequest Request { get; set; }
    }

    public interface IUI_Code
    {
        int Code { get; set; }
    }

    public abstract class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UI_Property : NotifyPropertyChanged
    {
        public delegate Brush xBackgroundRule<TProperty>(TProperty property);
        public delegate TValue ParseRule<TValue>(TValue last, TValue request);

        protected string name = "";
        protected string note = "";
        protected bool state = false;
        protected bool is_writable = false;
        protected bool is_enable = true;
        protected object code;

        protected object _value;
        public object _request;

        protected Visibility visibility_value = Visibility.Visible;
        protected Visibility visibility_request = Visibility.Visible;

        public static Brush RED = (Brush)new BrushConverter().ConvertFrom("#FF641818");
        public static Brush GREEN = (Brush)new BrushConverter().ConvertFrom("#FF21662A");
        public static Brush YELLOW = (Brush)new BrushConverter().ConvertFrom("#FF724C21");
        public static Brush TRANSPARENT = null;

        protected Brush background_value;
        protected Brush background_request;

        public object Content;
        public xEvent<UI_Property> EventSelection;
        public xEvent<UI_Property> EventValueChanged;
        public xEvent<UI_Property> EventRequestChanged;
        public xBackgroundRule<UI_Property> BackgroundValueRule;
        public xBackgroundRule<UI_Property> BackgroundRequestRule;

        public DataTemplate RequestDataTemplate;
        public Control RequestControl;

        public static Brush GetBrush(string request)
        {
            Brush brush = null;
            try { brush = (Brush)new BrushConverter().ConvertFrom(request); }
            catch { }
            return brush;
        }

        public virtual string Name
        {
            set { name = value; OnPropertyChanged(nameof(Name)); }
            get { return name; }
        }

        public virtual string Note
        {
            set { note = value; OnPropertyChanged(nameof(Note)); }
            get { return note; }
        }

        public virtual object Code
        {
            set { code = value; OnPropertyChanged(nameof(Code)); }
            get { return code; }
        }

        public bool IsEnable
        {
            set { is_enable = value; OnPropertyChanged(nameof(IsEnable)); }
            get { return is_enable; }
        }

        public bool IsReadOnly
        {
            get { return !is_writable; }
            set { }
        }

        public bool IsWritable
        {
            set { is_writable = value; IsEnable = is_enable | is_writable; OnPropertyChanged(nameof(IsWritable)); OnPropertyChanged(nameof(IsReadOnly)); }
            get => is_writable;
        }

        public Brush BackgroundValue
        {
            set { background_value = value; OnPropertyChanged(nameof(BackgroundValue)); }
            get { return background_value; }
        }

        public Brush BackgroundRequest
        {
            set { background_request = value; OnPropertyChanged(nameof(BackgroundRequest)); }
            get { return background_request; }
        }

        public Visibility VisibilityValue
        {
            get { return visibility_value; }
            set { visibility_value = value; OnPropertyChanged(nameof(VisibilityValue)); }
        }

        public Visibility VisibilityRequest
        {
            get => _request == null ? Visibility.Collapsed : visibility_request;
            set { visibility_request = value; OnPropertyChanged(nameof(VisibilityRequest)); }
        }

        public virtual bool SetValue(object request)
        {
            try
            {
                Value = request;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public virtual bool SetRequest<TArg>(TArg request) where TArg : IConvertible
        {
            try
            {
                Request = request;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public virtual object Value
        {
            get => _value;
            set
            {
                if (_value != null)
                {
                    try
                    {
                        var result = Convert.ChangeType(value, _value?.GetType());
                        if (result != null) { value = result; }
                    }
                    catch { }

                    if (Comparer<object>.Default.Compare(_value, value) != 0)
                    {
                        _value = value;
                        OnPropertyChanged(nameof(Value));
                        EventValueChanged?.Invoke(this);
                        BackgroundValue = BackgroundValueRule?.Invoke(this);
                    }
                }
            }
        }

        public virtual object Request
        {
            get => _request;
            set
            {
                if (_request != null)
                {
                    try
                    {
                        var result = Convert.ChangeType(value, _request?.GetType());
                        if (result != null) { value = result; }
                    }
                    catch { }

                    if (Comparer<object>.Default.Compare(_request, value) != 0)
                    {
                        _request = value;
                        OnPropertyChanged(nameof(Request));
                        EventRequestChanged?.Invoke(this);
                        BackgroundRequest = BackgroundRequestRule?.Invoke(this);
                    }
                }
            }
        }

        public virtual void Update()
        {
            BackgroundRequest = BackgroundRequestRule?.Invoke(this);
            BackgroundValue = BackgroundValueRule?.Invoke(this);
        }

        protected async Task<object> wait_value_state_async(UI_Property property, object state, int time)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (Comparer<object>.Default.Compare(_value, state) != 0 && stopwatch.ElapsedMilliseconds < time)
            {
                await Task.Delay(1);
                //Thread.Sleep(1);
            }
            stopwatch.Stop();
            return property.Value;
        }

        protected TState wait_value_state<TState>(UI_Property property, TState state, int time)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (Comparer<object>.Default.Compare(_value, state) != 0 && stopwatch.ElapsedMilliseconds < time)
            {
                Thread.Sleep(1);
            }
            stopwatch.Stop();
            return (TState)property.Value;
        }

        public virtual TState WaitValue<TState>(TState state, int time)
        {
            return wait_value_state(this, state, time);
        }

        public virtual async Task<TState> WaitValueAsync<TState>(TState state, int time)
        {
            return (TState)await Task.Run(() => wait_value_state_async(this, state, time));
        }

        public virtual void Select()
        {
            EventSelection?.Invoke(this);
        }

        public virtual TValue Offset<TValue>(TValue value, int offset)
        {
            dynamic a = value;
            a += offset;
            return (TValue)a;
        }
    }

    public class UI_Property<TValue, TRequest> : UI_Property, IUI_PropertyEvents where TValue : IComparable where TRequest : IComparable
    {
        public UI_Property()
        {
            _value = default(TValue);
            _request = default(TRequest);
        }

        public new xEvent<UI_Property<TValue, TRequest>> EventValueChanged;
        public new xEvent<UI_Property<TValue, TRequest>> EventRequestChanged;
        public new xEvent<UI_Property<TValue, TRequest>> EventSelection;
        public ParseRule<TValue> ValueParseRule;
        public new xBackgroundRule<UI_Property<TValue, TRequest>> BackgroundValueRule;
        public new xBackgroundRule<UI_Property<TValue, TRequest>> BackgroundRequestRule;

        public new TValue Value
        {
            get => _value != null ? (TValue)_value : default(TValue);
            set
            {
                if (_value != null && Comparer<TValue>.Default.Compare((TValue)_value, value) != 0)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                    EventValueChanged?.Invoke(this);
                    BackgroundValue = BackgroundValueRule?.Invoke(this);
                }
            }
        }

        public new TRequest Request
        {
            get => _request != null ? (TRequest)_request : default(TRequest);
            set
            {
                if (_request != null && Comparer<TRequest>.Default.Compare((TRequest)_request, value) != 0)
                {
                    _request = value;
                    OnPropertyChanged(nameof(Request));
                    EventRequestChanged?.Invoke(this);
                    BackgroundRequest = BackgroundRequestRule?.Invoke(this);
                }
            }
        }

        public override void Update()
        {
            BackgroundRequest = BackgroundRequestRule?.Invoke(this);
            BackgroundValue = BackgroundValueRule?.Invoke(this);
        }

        public virtual TValue WaitValue(TValue state, int time)
        {
            return (TValue)wait_value_state(this, state, time);
        }

        public virtual async Task<TValue> WaitValueAsync(TValue state, int time)
        {
            return (TValue)await Task.Run(() => wait_value_state_async(this, state, time));
        }

        public override void Select() => EventSelection?.Invoke(this);
    }

    public class UI_Property<TValue> : UI_Property<TValue, TValue>, IUI_PropertyEvents where TValue : IComparable
    {
        //public UI_Property() : base();
    }
}
