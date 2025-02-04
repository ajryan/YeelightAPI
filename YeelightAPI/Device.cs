﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YeelightAPI.Core;
using YeelightAPI.Models;

namespace YeelightAPI
{
    /// <summary>
    /// Yeelight Device
    /// </summary>
    public partial class Device : IDisposable
    {
        #region PRIVATE ATTRIBUTES

        /// <summary>
        /// Dictionary of results
        /// </summary>
        private readonly Dictionary<int, ICommandResultHandler> _currentCommandResults = new Dictionary<int, ICommandResultHandler>();

        /// <summary>
        /// lock
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// The unique id to send when executing a command.
        /// </summary>
        private int _uniqueId = 0;

        /// <summary>
        /// TCP client used to communicate with the device
        /// </summary>
        private TcpClient? _tcpClient;

        /// <summary>
        /// Cancellation token source for the Watch task
        /// </summary>
        private CancellationTokenSource? _watchCancellationTokenSource;

        #endregion PRIVATE ATTRIBUTES

        #region EVENTS

        /// <summary>
        /// Notification Received event
        /// </summary>
        public event ErrorEventHandler? OnError;

        /// <summary>
        /// Notification Received event
        /// </summary>
        public event NotificationReceivedEventHandler? OnNotificationReceived;

        /// <summary>
        /// Notification Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ErrorEventHandler(object sender, UnhandledExceptionEventArgs e);

        /// <summary>
        /// Notification Received event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void NotificationReceivedEventHandler(object sender, NotificationReceivedEventArgs e);

        #endregion EVENTS

        #region PUBLIC PROPERTIES

        /// <summary>
        /// HostName
        /// </summary>
        public string Hostname { get; }

        /// <summary>
        /// The ID.
        /// </summary>
        public string? Id { get; }

        /// <summary>
        /// Gets a value indicating if the connection to Device is established
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _tcpClient != null && _tcpClient.IsConnected();
            }
        }

        /// <summary>
        /// Indicate wether the music mode is enabled
        /// </summary>
        public bool IsMusicModeEnabled { get; private set; }

        /// <summary>
        /// The model.
        /// </summary>
        public MODEL Model { get; }

        /// <summary>
        /// Port number
        /// </summary>
        public int Port { get; }

        #endregion PUBLIC PROPERTIES

        #region CONSTRUCTOR

        /// <summary>
        /// Constructor with a hostname and (optionally) a port number
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <param name="autoConnect"></param>
        public Device(string hostname, int port = Constants.DefaultPort, bool autoConnect = false)
        {
            Hostname = hostname;
            Port = port;

            // auto-connect device if specified
            if (autoConnect)
            {
                Connect().Wait();
            }
        }

        internal Device(string hostname, int port, string id, MODEL model, string firmwareVersion, Dictionary<string, object> properties, List<METHODS> supportedOperations)
        {
            Hostname = hostname;
            Port = port;
            Id = id;
            Model = model;
            FirmwareVersion = firmwareVersion;
            Properties = properties;
            SupportedOperations = supportedOperations;
        }

        #endregion CONSTRUCTOR

        #region PROPERTIES ACCESS

        /// <summary>
        /// Firmware version
        /// </summary>
        public readonly string? FirmwareVersion = null;

        /// <summary>
        /// List of device properties
        /// </summary>
        public readonly Dictionary<string, object> Properties = new Dictionary<string, object>();

        /// <summary>
        /// List of supported operations
        /// </summary>
        public readonly List<METHODS> SupportedOperations = new List<METHODS>();

        /// <summary>
        /// Name of the device
        /// </summary>
        public string Name
        {
            get
            {
                return this[PROPERTIES.name] as string ?? "<unknown>";
            }
            set
            {
                this[PROPERTIES.name] = value;
            }
        }

        /// <summary>
        /// Access property from its enum value
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public object? this[PROPERTIES property]
        {
            get
            {
                return this[property.ToString()];
            }
            set
            {
                this[property.ToString()] = value;
            }
        }

        /// <summary>
        /// Access property from its name
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object? this[string propertyName]
        {
            get
            {
                return Properties.TryGetValue(propertyName, out var val) ? val : null;
            }
            set
            {
                Properties[propertyName] = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        #endregion PROPERTIES ACCESS

        #region PUBLIC METHODS

        /// <summary>
        /// Execute a command
        /// </summary>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        public void ExecuteCommand(METHODS method, List<object>? parameters = null)
        {
            ExecuteCommand(method, GetUniqueIdForCommand(), parameters);
        }


        /// <summary>
        /// Execute a command and waits for a response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<CommandResult<T>?> ExecuteCommandWithResponse<T>(METHODS method, List<object>? parameters = null)
        {
            if (IsMusicModeEnabled)
            {
                // music mode enabled, there will be no response, we should assume everything works
                int uniqueId = GetUniqueIdForCommand();
                ExecuteCommand(method, uniqueId, parameters);
                return Task.FromResult((CommandResult<T>?)(new CommandResult<T>() { Id = uniqueId, Error = null, IsMusicResponse = true }));
            }

            // default behavior : send command and wait for response
            return ExecuteCommandWithResponse<T>(method, GetUniqueIdForCommand(), parameters);
        }


        /// <summary>
        /// Readable value for the device
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.Model} ({this.Hostname}:{this.Port})";
        }

        #endregion PUBLIC METHODS

        #region INTERNAL METHODS

        /// <summary>
        /// Execute a command
        /// </summary>
        /// <param name="method"></param>
        /// <param name="id"></param>
        /// <param name="parameters"></param>
        internal void ExecuteCommand(METHODS method, int id, List<object>? parameters = null)
        {
            if (_tcpClient == null)
            {
                throw new InvalidOperationException("Not connected.");
            }

            if (!IsMethodSupported(method))
            {
                throw new InvalidOperationException($"The operation {method.GetRealName()} is not allowed by the device");
            }

            Command command = new Command()
            {
                Id = id,
                Method = method.GetRealName() ?? throw new ApplicationException("METHODS enum missing RealName attribute"),
                Params = parameters ?? new List<object>()
            };

            string data = JsonConvert.SerializeObject(command, Constants.DeviceSerializerSettings);
            byte[] sentData = Encoding.ASCII.GetBytes(data + Constants.LineSeparator); // \r\n is the end of the message, it needs to be sent for the message to be read by the device

            lock (_syncLock)
            {
                _tcpClient.Client.Send(sentData);
            }
        }

        /// <summary>
        /// Execute a command and waits for a response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="id"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal async Task<CommandResult<T>?> ExecuteCommandWithResponse<T>(METHODS method, int id, List<object>? parameters = null)
        {
            try
            {
                return await UnsafeExecuteCommandWithResponse<T>(method, id, parameters);
            }
            catch (TaskCanceledException) { }

            return null;
        }

        internal async Task DisableMusicModeAsync()
        {
            _ = await Connect();
            IsMusicModeEnabled = false;

        }

        #endregion INTERNAL METHODS

        #region PRIVATE METHODS

        private static string GetLocalIpAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint ?? throw new ApplicationException("Cannot determine local endpoint");
                return endPoint.Address.ToString();
            }
        }

        /// <summary>
        /// Generate valid parameters for percent values
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="percent"></param>
        private static void HandlePercentValue(ref List<object> parameters, int percent)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (percent < 0)
            {
                parameters.Add(Math.Max(percent, -100));
            }
            else
            {
                parameters.Add(Math.Min(percent, 100));
            }
        }

        /// <summary>
        /// Generate valid parameters for smooth values
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="smooth"></param>
        private static void HandleSmoothValue(ref List<object> parameters, int? smooth)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (smooth.HasValue)
            {
                parameters.Add("smooth");
                parameters.Add(Math.Max(smooth.Value, Constants.MinimumSmoothDuration));
            }
            else
            {
                parameters.Add("sudden");
                parameters.Add(0); // two parameters needed
            }
        }

        /// <summary>
        /// Check if the method is supported by the device
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private bool IsMethodSupported(METHODS method)
        {
            if (SupportedOperations?.Count != 0)
            {
                return SupportedOperations!.Contains(method);
            }

            return true;
            // no supported operations, so we can't check if the operation is permitted
        }

        /// <summary>
        /// Execute a command and waits for a response (Unsafe because of Task Cancellation)
        /// </summary>
        /// <param name="method"></param>
        /// <param name="id"></param>
        /// <param name="parameters"></param>
        /// <exception cref="TaskCanceledException"></exception>
        /// <returns></returns>
        private async Task<CommandResult<T>> UnsafeExecuteCommandWithResponse<T>(METHODS method, int id = 0, List<object>? parameters = null)
        {
            CommandResultHandler<T> commandResultHandler;
            lock (_currentCommandResults)
            {
                if (_currentCommandResults.TryGetValue(id, out var oldHandler))
                {
                    oldHandler.TrySetCanceled();
                    _currentCommandResults.Remove(id);
                }

                commandResultHandler = new CommandResultHandler<T>();
                _currentCommandResults.Add(id, commandResultHandler);
            }

            try
            {
                ExecuteCommand(method, id, parameters);
                return await commandResultHandler.Task;
            }
            finally
            {
                lock (_currentCommandResults)
                {
                    // remove the command if its the current handler in the dictionary
                    if (_currentCommandResults.TryGetValue(id, out var currentHandler))
                    {
                        if (commandResultHandler == currentHandler)
                            _currentCommandResults.Remove(id);
                    }
                }
            }
        }

        /// <summary>
        /// Watch for device responses and notifications
        /// </summary>
        /// <returns></returns>
        private async Task Watch()
        {
            using (_watchCancellationTokenSource = new CancellationTokenSource())
            {
                await Task.Run(async () =>
                {
                    // while device is connected
                    while (_tcpClient != null && _watchCancellationTokenSource.IsCancellationRequested == false)
                    {
                        lock (_syncLock)
                        {
                            if (_tcpClient != null)
                            {
                                // automatic re-connection
                                if (!_tcpClient.IsConnected())
                                {
                                    _tcpClient.ConnectAsync(Hostname, Port).Wait();
                                }

                                if (_tcpClient.IsConnected())
                                {
                                    // there is data available in the pipe
                                    if (_tcpClient.Client.Available > 0)
                                    {
                                        byte[] bytes = new byte[_tcpClient.Client.Available];

                                        // read data
                                        _tcpClient.Client.Receive(bytes);

                                        try
                                        {
                                            string data = Encoding.UTF8.GetString(bytes);
                                            if (!string.IsNullOrEmpty(data))
                                            {
                                                // get every messages in the pipe
                                                foreach (string entry in data.Split(new string[] { Constants.LineSeparator },
                                                        StringSplitOptions.RemoveEmptyEntries))
                                                {
                                                    var commandResult =
                                                        JsonConvert.DeserializeObject<CommandResult>(entry, Constants.DeviceSerializerSettings);
                                                    if (commandResult?.Id != 0)
                                                    {
                                                        ICommandResultHandler? commandResultHandler;
                                                        lock (_currentCommandResults)
                                                        {
                                                            if (!_currentCommandResults.TryGetValue(commandResult!.Id, out commandResultHandler))
                                                                continue; // ignore if the result can't be found
                                                        }

                                                        if (commandResult.Error == null)
                                                        {
                                                            commandResult = (CommandResult?)JsonConvert.DeserializeObject(entry, commandResultHandler.ResultType, Constants.DeviceSerializerSettings);
                                                            if (commandResult != null)
                                                            {
                                                                commandResultHandler.SetResult(commandResult);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            commandResultHandler.SetError(commandResult.Error);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var notificationResult =
                                                            JsonConvert.DeserializeObject<NotificationResult>(entry,
                                                                Constants.DeviceSerializerSettings);

                                                        if (notificationResult != null && notificationResult.Method != null)
                                                        {
                                                            if (notificationResult.Params != null)
                                                            {
                                                                // save properties
                                                                foreach (KeyValuePair<PROPERTIES, object> property in
                                                                        notificationResult.Params)
                                                                {
                                                                    this[property.Key] = property.Value;
                                                                }
                                                            }

                                                            // notification result
                                                            OnNotificationReceived?.Invoke(this,
                                                                    new NotificationReceivedEventArgs(notificationResult));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            OnError?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
                                        }
                                    }
                                }
                            }
                        }

                        await Task.Delay(100);
                    }
                }, _watchCancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// Get a thread-safe unique Id to pass to the API
        /// </summary>
        /// <returns></returns>
        private int GetUniqueIdForCommand()
        {
            return Interlocked.Increment(ref _uniqueId);
        }

        #endregion PRIVATE METHODS

        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                lock (_syncLock)
                {
                    Disconnect();
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~Device()
        {
            Dispose(false);
        }

        #endregion IDisposable
    }
}