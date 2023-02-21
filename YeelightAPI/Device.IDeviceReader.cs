using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YeelightAPI.Core;
using YeelightAPI.Models;
using YeelightAPI.Models.Cron;

namespace YeelightAPI
{
    /// <summary>
    /// Yeelight Device : IDeviceReader implementation
    /// </summary>
    public partial class Device : IDeviceReader
    {
        #region Public Methods

        /// <summary>
        /// Get a cron JOB
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<CronResult?> CronGet(CronType type = CronType.PowerOff)
        {
            List<object> parameters = new List<object>() { (int)type };

            var result = await ExecuteCommandWithResponse<CronResult[]>(
                            method: METHODS.GetCron,
                            parameters: parameters);

            return result?.Result?.FirstOrDefault();
        }

        /// <summary>
        /// Get all the properties asynchronously
        /// </summary>
        /// <returns></returns>
        public Task<Dictionary<PROPERTIES, object>> GetAllProps()
        {
            return GetProps(PROPERTIES.ALL);
        }

        /// <summary>
        /// Get a single property value asynchronously
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public async Task<object?> GetProp(PROPERTIES prop)
        {
            var result = await ExecuteCommandWithResponse<List<string>>(
                method: METHODS.GetProp,
                parameters: new List<object>() { prop.ToString() });

            return result?.Result?.Count == 1 ? result.Result[0] : null;
        }

        /// <summary>
        /// Get multiple properties asynchronously
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        public async Task<Dictionary<PROPERTIES, object>> GetProps(PROPERTIES props)
        {
            List<string> names = props.GetRealNames();
            List<string> response = new List<string>();
            if (names.Count <= 20)
            {
                var commandResult = await ExecuteCommandWithResponse<List<string>>(
                    method: METHODS.GetProp,
                    parameters: names.Select(n => (object)n).ToList());

                if (commandResult == null)
                {
                    throw new ApplicationException("Empty comment result from GetProp");
                }
                response.AddRange(commandResult.Result);
            }
            else
            {

                var commandResult1 = await ExecuteCommandWithResponse<List<string>>(
                    method: METHODS.GetProp,
                    parameters: names.Take(20).Select(n => (object)n).ToList());
                var commandResult2 = await ExecuteCommandWithResponse<List<string>>(
                    method: METHODS.GetProp,
                    parameters: names.Skip(20).Select(n => (object)n).ToList());

                if (commandResult1 == null || commandResult2 == null)
                {
                    throw new ApplicationException("Empty comment result from GetProp");
                }
                response.AddRange(commandResult1.Result);
                response.AddRange(commandResult2.Result);

            }

            if (response.Count != names.Count)
            {
                throw new ApplicationException("Got different GetProp count versus requested names.");
            }

            var result = new Dictionary<PROPERTIES, object>();

            for (int n = 0; n < names.Count; n++)
            {
                string? name = names[n].ToString();

                if (name != null && Enum.TryParse<PROPERTIES>(name, out PROPERTIES p))
                {
                    result.Add(p, response[n]);
                }
            }

            return result;
        }

        /// <summary>
        /// Set the name of the device
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<bool> SetName(string name)
        {
            List<object> parameters = new List<object>() { name };

            var result = await ExecuteCommandWithResponse<List<string>>(
                            method: METHODS.SetName,
                            parameters: parameters);

            if (result?.IsOk() == true)
            {
                Name = name;
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion Public Methods
    }
}