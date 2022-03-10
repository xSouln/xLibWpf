﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xLib.Transceiver
{
    public class xRequest
    {
        protected xAction<bool, byte[]> transmitter;
        protected int try_count = 1;
        protected int try_number = 0;
        protected int response_time_out = 100;
        protected long response_time = 0;
        protected Timer timer_transmiter;
        protected volatile ETransactionState transmission_state;

        protected AutoResetEvent transmition_synchronize = new AutoResetEvent(true);

        protected volatile bool is_transmit_action;
        protected volatile bool is_accept;
        //public bool IsNotify = true;

        public byte[] Data;

        public xEvent<xRequest> EventTimeOut;

        public virtual xTransactionBase Parent { get; set; }

        public virtual xTransactionHandler Handler { get; set; }

        public virtual xEvent<string> Tracer { get; set; }

        public string Name => Parent?.Name;

        public int ResponseTimeOut => response_time_out;

        public long ResponseTime => response_time;

        public int TryCount
        {
            get => try_count;
            set
            {
                if (try_count > 0) { try_count = value; }
            }
        }

        public int TryNumber => try_number;

        public xAction<bool, byte[]> Transmitter
        {
            get => transmitter;
            set => transmitter = value;
        }

        public ETransactionState TransmissionState => transmission_state;

        public void Accept()
        {
            try
            {
                transmition_synchronize.WaitOne();

                if (transmission_state == ETransactionState.IsTransmit)
                {
                    transmission_state = ETransactionState.Complite;
                }
            }
            finally
            {
                transmition_synchronize.Set();
            }
        }

        protected static void transmit_action(xRequest request)
        {
            try
            {
                request.transmition_synchronize.WaitOne();

                if (request.transmission_state == ETransactionState.IsTransmit)
                {
                    if (request.try_number < request.try_count)
                    {
                        if (request.transmitter == null || !request.transmitter(request.Data))
                        {
                            request.transmission_state = ETransactionState.ErrorTransmite;
                            request.Tracer?.Invoke("Transmit: " + request.Parent.Name + " " + request.transmission_state);
                            return;
                        }
                        request.try_number++;
                        request.Tracer?.Invoke("Transmit: " + request.Parent.Name + " try: " + request.try_number);
                    }
                    else
                    {
                        request.transmission_state = ETransactionState.TimeOut;
                        request.Tracer?.Invoke("TimeOut: " + request.Parent.Name);
                        request.EventTimeOut?.Invoke(request);
                    }
                }
            }
            finally { request.transmition_synchronize.Set(); }
        }

        protected virtual xRequest transmition()
        {
            try
            {
                transmition_synchronize.WaitOne();

                if (transmission_state != ETransactionState.Free) { return this; }
                transmission_state = ETransactionState.Prepare;

                if (!(bool)Handler?.Add(Parent))
                {
                    transmission_state = ETransactionState.Busy;
                    return this;
                }
                else
                {
                    transmission_state = ETransactionState.IsTransmit;
                }
            }
            finally
            {
                transmition_synchronize.Set();
            }

            try_number = 0;
            response_time = 0;

            Stopwatch time_transmition = new Stopwatch();
            Stopwatch time_transmit_action = new Stopwatch();

            time_transmition.Start();
            do
            {
                transmit_action(this);
                time_transmit_action.Restart();
                while (transmission_state == ETransactionState.IsTransmit && time_transmit_action.ElapsedMilliseconds < response_time_out)
                {
                    Thread.Sleep(1);
                }
            }
            while (transmission_state == ETransactionState.IsTransmit);

            time_transmition.Stop();
            time_transmit_action.Stop();
            response_time = time_transmition.ElapsedMilliseconds;
            return this;
        }

        protected virtual async Task<xRequest> transmition_async()
        {
            try
            {
                transmition_synchronize.WaitOne();

                if (transmission_state != ETransactionState.Free) { return this; }
                transmission_state = ETransactionState.Prepare;

                if (!(bool)Handler?.Add(Parent))
                {
                    transmission_state = ETransactionState.Busy;
                    return this;
                }
                else
                {
                    transmission_state = ETransactionState.IsTransmit;
                }
            }
            finally
            {
                transmition_synchronize.Set();
            }

            try_number = 0;
            response_time = 0;

            Stopwatch time_transmition = new Stopwatch();
            Stopwatch time_transmit_action = new Stopwatch();

            time_transmition.Start();
            do
            {
                transmit_action(this);
                time_transmit_action.Restart();
                while (transmission_state == ETransactionState.IsTransmit && time_transmit_action.ElapsedMilliseconds < response_time_out)
                {
                    await Task.Delay(1);
                }
            }
            while (transmission_state == ETransactionState.IsTransmit);

            time_transmition.Stop();
            time_transmit_action.Stop();
            response_time = time_transmition.ElapsedMilliseconds;
            return this;
        }

        public virtual void Break()
        {
            transmition_synchronize.WaitOne();
            transmission_state = ETransactionState.Free;
            transmition_synchronize.Set();

            Handler?.Remove(Parent);
        }

        public virtual xRequest Transmition(xAction<bool, byte[]> transmitter, int try_count, int response_time_out)
        {
            if (transmitter == null || try_count <= 0 || response_time_out <= 0 || transmission_state != ETransactionState.Free) { return null; }
            this.transmitter = transmitter;
            this.try_count = try_count;
            this.response_time_out = response_time_out;
            this.try_number = 0;

            var result = transmition();
            return result;
        }

        public static TRequest Transmition<TRequest>(TRequest request, xAction<bool, byte[]> transmitter, int try_count, int response_time_out) where TRequest : xRequest
        {
            if (transmitter == null || try_count <= 0 || response_time_out <= 0 || request.transmission_state != ETransactionState.Free) { return null; }

            request.transmitter = transmitter;
            request.try_count = try_count;
            request.response_time_out = response_time_out;
            request.try_number = 0;

            var result = request.transmition();
            return (TRequest)result;
        }

        public virtual async Task<xRequest> TransmitionAsync(xAction<bool, byte[]> transmitter, int try_count, int response_time_out)
        {
            if (transmitter == null || try_count <= 0 || response_time_out <= 0 || transmission_state != ETransactionState.Free) { return null; }
            this.transmitter = transmitter;
            this.try_count = try_count;
            this.response_time_out = response_time_out;
            this.try_number = 0;

            var result = await Task.Run(() => transmition());
            //var result = await Task.Run(() => transmition_async());
            return result;
        }

        public static async Task<TRequest> TransmitionAsync<TRequest>(TRequest request, xAction<bool, byte[]> transmitter, int try_count, int response_time, CancellationTokenSource cancellation) where TRequest : xRequest
        {
            if (transmitter == null || try_count <= 0 || response_time <= 0 || request.transmission_state != ETransactionState.Free) { return null; }

            if (cancellation == null) { cancellation = new CancellationTokenSource(); }

            request.transmitter = transmitter;
            request.try_count = try_count;
            request.response_time_out = response_time;
            request.try_number = 0;

            var result = await Task.Run(() => request.transmition(), cancellation.Token);
            return (TRequest)result;
        }

        public static async Task<TRequest> TransmitionAsync<TRequest>(TRequest request, xAction<bool, byte[]> transmitter, int try_count, int response_time) where TRequest : xRequest
        {
            if (transmitter == null || try_count <= 0 || response_time <= 0 || request.transmission_state != ETransactionState.Free) { return null; }

            request.transmitter = transmitter;
            request.try_count = try_count;
            request.response_time_out = response_time;
            request.try_number = 0;

            var result = await Task.Run(() => request.transmition());
            return (TRequest)result;
        }
    }
}