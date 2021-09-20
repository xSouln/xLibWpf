﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using xLib.UI_Propertys;

namespace xLib.Sources
{
    public class xCommunicationControl : NotifyPropertyChanged
    {
        private Timer timer;
        private bool is_update;
        private bool is_available;
        private Brush background_true = (Brush)new BrushConverter().ConvertFrom("#FF21662A");
        private Brush background_false = (Brush)new BrushConverter().ConvertFrom("#FF641818");
        private Brush background;

        public event xEvent<bool> EventStateChanged;

        public Brush Background
        {
            get { return background; }
            set { if (background != value) { background = value; OnPropertyChanged(nameof(Background)); } }
        }

        public bool IsAvailable
        {
            get { return is_available; }
            set
            {
                if (value && background != background_true) { background = background_true; }
                else if(!value && background != background_false) { background = background_false; }

                if (is_available != value)
                {
                    is_available = value;
                    EventStateChanged?.Invoke(is_available);
                    OnPropertyChanged(nameof(IsAvailable));
                }
            }
        }

        public void StartControl(int period)
        {
            if (period < 100) { period = 100; }
            timer = new Timer(update_state, null, 0, period);
        }

        public void Dispose() { timer?.Dispose(); }

        private void update_state(object arg)
        {
            if (!is_update && IsAvailable) { IsAvailable = false; }
            is_update = false;
        }

        public void Update() { is_update = true; IsAvailable = true; }
    }
}