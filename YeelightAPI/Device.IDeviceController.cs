﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using YeelightAPI.Core;
using YeelightAPI.Models;
using YeelightAPI.Models.Adjust;
using YeelightAPI.Models.ColorFlow;
using YeelightAPI.Models.Cron;
using YeelightAPI.Models.Music;
using YeelightAPI.Models.Scene;

namespace YeelightAPI
{
    /// <summary>
    /// Yeelight Device : IDeviceController implementation
    /// </summary>
    public partial class Device : IDeviceController
    {
        /// <summary>
        /// Adjusts the brightness
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public async Task<bool> AdjustBright(int percent, int? duration = null)
        {
            List<object> parameters = new List<object>() { };
            HandlePercentValue(ref parameters, percent);
            parameters.Add(Math.Max(duration ?? 0, Constants.MinimumSmoothDuration));

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.AdjustBright,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Adjusts the color
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public async Task<bool> AdjustColor(int percent, int? duration = null)
        {
            List<object> parameters = new List<object>() { };
            HandlePercentValue(ref parameters, percent);
            parameters.Add(Math.Max(duration ?? 0, Constants.MinimumSmoothDuration));

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.AdjustColor,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Adjusts the color temperature
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public async Task<bool> AdjustColorTemperature(int percent, int? duration = null)
        {
            List<object> parameters = new List<object>() { };
            HandlePercentValue(ref parameters, percent);
            parameters.Add(Math.Max(duration ?? 0, Constants.MinimumSmoothDuration));

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.AdjustColorTemperature,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        #region Public Methods

        /// <summary>
        /// Connects to a device asynchronously
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Connect()
        {
            Disconnect();

            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(Hostname, Port);

            if (!_tcpClient.IsConnected())
            {
                return false;
            }

            // continuous receiving
#pragma warning disable 4014
            Watch();
#pragma warning restore 4014

            // initializing all properties
            Dictionary<PROPERTIES, object> properties = await GetAllProps();

            if (properties != null)
            {
                foreach (KeyValuePair<PROPERTIES, object> property in properties)
                {
                    this[property.Key] = property.Value;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Add a cron job
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<bool> CronAdd(int value, CronType type = CronType.PowerOff)
        {
            List<object> parameters = new List<object>() { (int)type, value };

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.AddCron,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Delete a cron job
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<bool> CronDelete(CronType type = CronType.PowerOff)
        {
            List<object> parameters = new List<object>() { (int)type };

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.DeleteCron,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Disconnect the current device
        /// </summary>
        /// <returns></returns>
        public void Disconnect()
        {
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
                try
                {
                    _watchCancellationTokenSource?.Cancel();
                }
                catch (ObjectDisposedException ex)
                {
                    OnError?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
                }
            }
        }

        /// <summary>
        /// Initiate a new Color Flow
        /// </summary>
        /// <returns></returns>
        public FluentFlow Flow()
        {
            return new FluentFlow(this, StartColorFlow, StopColorFlow);
        }

        /// <summary>
        /// Adjusts the state of the device
        /// </summary>
        /// <param name="action"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public async Task<bool> SetAdjust(AdjustAction action, AdjustProperty property)
        {
            List<object> parameters = new List<object>() { action.ToString(), property.ToString() };

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetAdjust,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Change the device brightness asynchronously
        /// </summary>
        /// <param name="value"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public async Task<bool> SetBrightness(int value, int? smooth = null)
        {
            List<object> parameters = new List<object>() { value };

            HandleSmoothValue(ref parameters, smooth);

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetBrightness,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Change Color Temperature asynchronously
        /// </summary>
        /// <param name="temperature"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public async Task<bool> SetColorTemperature(int temperature, int? smooth = null)
        {
            List<object> parameters = new List<object>() { temperature };

            HandleSmoothValue(ref parameters, smooth);

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetColorTemperature,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Set the current state as the default one
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SetDefault()
        {
            var result = await ExecuteCommandWithResponse<List<string>>(METHODS.SetDefault);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Change HSV color asynchronously
        /// </summary>
        /// <param name="hue"></param>
        /// <param name="sat"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public async Task<bool> SetHSVColor(int hue, int sat, int? smooth = null)
        {
            List<object> parameters = new List<object>() { hue, sat };

            HandleSmoothValue(ref parameters, smooth);

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetHSVColor,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Set the device power state asynchronously
        /// </summary>
        /// <param name="state"></param>
        /// <param name="smooth"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public async Task<bool> SetPower(bool state = true, int? smooth = null, PowerOnMode mode = PowerOnMode.Normal)
        {
            List<object> parameters = new List<object>() { state ? Constants.On : Constants.Off };
            HandleSmoothValue(ref parameters, smooth);
            parameters.Add((int)mode);

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetPower,
                parameters: parameters
            );

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Change the device RGB color asynchronously
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public async Task<bool> SetRGBColor(int r, int g, int b, int? smooth = null)
        {
            // Convert RGB into integer 0x00RRGGBB
            int value = ColorHelper.ComputeRGBColor(r, g, b);
            List<object> parameters = new List<object>() { value };

            HandleSmoothValue(ref parameters, smooth);

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetRGBColor,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Set a Scene
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public async Task<bool> SetScene(Scene scene)
        {
            List<object> parameters = scene;

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetScene,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Starts a color flow asynchronously
        /// </summary>
        /// <param name="flow"></param>
        /// <returns></returns>
        public async Task<bool> StartColorFlow(ColorFlow flow)
        {
            List<object> parameters = new List<object>() { flow.RepetitionCount * flow.Count, (int)flow.EndAction, flow.GetColorFlowExpression() };

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.StartColorFlow,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Starts the music mode
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<bool> StartMusicMode(string? hostname = null, int port = 12345)
        {
            // init new TCP socket
            if (string.IsNullOrWhiteSpace(hostname))
            {
                hostname = GetLocalIpAddress();
            }

            var listener = new TcpListener(System.Net.IPAddress.Parse(hostname), port);
            listener.Start();

            List<object> parameters = new List<object>() { (int)MusicAction.On, hostname, port };

            var result = await ExecuteCommandWithResponse<List<string>>(
                            method: METHODS.SetMusicMode,
                            parameters: parameters);

            if (result?.IsOk() == true)
            {
                this.IsMusicModeEnabled = true;
                this.Disconnect();
                var musicTcpClient = await listener.AcceptTcpClientAsync();
                _tcpClient = musicTcpClient;
            }

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Stops the color flow
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StopColorFlow()
        {
            var result = await ExecuteCommandWithResponse<List<string>>(
                            method: METHODS.StopColorFlow);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Stops the music mode
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StopMusicMode()
        {
            List<object> parameters = new List<object>() { (int)MusicAction.Off };

            var result = await ExecuteCommandWithResponse<List<string>>(
                            method: METHODS.SetMusicMode,
                            parameters: parameters);

            if (result?.IsOk() == true)
            {
                // disables the music mode
                this.IsMusicModeEnabled = false;
                await DisableMusicModeAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Toggle the device power asynchronously
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Toggle()
        {
            var result = await ExecuteCommandWithResponse<List<string>>(METHODS.Toggle);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Turn-Off the device
        /// </summary>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public async Task<bool> TurnOff(int? smooth = null)
        {
            List<object> parameters = new List<object>() { Constants.Off };
            HandleSmoothValue(ref parameters, smooth);

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetPower,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        /// <summary>
        /// Turn-On the device
        /// </summary>
        /// <param name="smooth"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public async Task<bool> TurnOn(int? smooth = null, PowerOnMode mode = PowerOnMode.Normal)
        {
            List<object> parameters = new List<object>() { Constants.On };
            HandleSmoothValue(ref parameters, smooth);
            parameters.Add((int)mode);

            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.SetPower,
                parameters: parameters);

            return result?.IsOk() == true;
        }

        #endregion Public Methods
    }
}
