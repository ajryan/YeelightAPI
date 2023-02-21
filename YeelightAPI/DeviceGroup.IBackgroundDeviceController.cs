using System.Threading.Tasks;
using YeelightAPI.Models;
using YeelightAPI.Models.Adjust;
using YeelightAPI.Models.ColorFlow;
using YeelightAPI.Models.Scene;

namespace YeelightAPI
{
    /// <summary>
    /// Group of Yeelight Devices : IBackgroundDeviceController implementation
    /// </summary>
    public partial class DeviceGroup : IBackgroundDeviceController
    {
        #region Public Methods

        /// <summary>
        /// Adjusts the brightness
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Task<bool> BackgroundAdjustBright(int percent, int? smooth = null)
        {
            return Process((Device device) =>
            {
                return device.BackgroundAdjustBright(percent, smooth);
            });
        }

        /// <summary>
        /// Adjusts the color
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Task<bool> BackgroundAdjustColor(int percent, int? smooth = null)
        {
            return Process((Device device) =>
            {
                return device.BackgroundAdjustColor(percent, smooth);
            });
        }

        /// <summary>
        /// Adjusts the color temperature
        /// </summary>
        /// <param name="percent"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Task<bool> BackgroundAdjustColorTemperature(int percent, int? smooth = null)
        {
            return Process((Device device) =>
            {
                return device.BackgroundAdjustColorTemperature(percent, smooth);
            });
        }

        /// <summary>
        /// Initiate a new background Color Flow
        /// </summary>
        /// <returns></returns>
        public FluentFlow BackgroundFlow()
        {
            return new FluentFlow(this, BackgroundStartColorFlow, BackgroundStopColorFlow);
        }

        /// <summary>
        /// Adjusts background light
        /// </summary>
        /// <param name="action"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public Task<bool> BackgroundSetAdjust(AdjustAction action, AdjustProperty property)
        {
            return Process((Device device) =>
            {
                return device.BackgroundSetAdjust(action, property);
            });
        }

        /// <summary>
        /// Set background light color
        /// </summary>
        /// <param name="value"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Task<bool> BackgroundSetBrightness(int value, int? smooth = null)
        {
            return Process((Device device) =>
            {
                return device.BackgroundSetBrightness(value, smooth);
            });
        }

        /// <summary>
        /// Set background light temperature
        /// </summary>
        /// <param name="temperature"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Task<bool> BackgroundSetColorTemperature(int temperature, int? smooth)
        {
            return Process((Device device) =>
            {
                return device.BackgroundSetColorTemperature(temperature, smooth);
            });
        }

        /// <summary>
        /// Set the background current state as the default one
        /// </summary>
        /// <returns></returns>
        public Task<bool> BackgroundSetDefault()
        {
            return Process((Device device) =>
            {
                return device.BackgroundSetDefault();
            });
        }

        /// <summary>
        /// Set background light HSV color
        /// </summary>
        /// <param name="hue"></param>
        /// <param name="sat"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Task<bool> BackgroundSetHSVColor(int hue, int sat, int? smooth = null)
        {
            return Process((Device device) =>
            {
                return device.BackgroundSetHSVColor(hue, sat, smooth);
            });
        }

        /// <summary>
        /// Set background light power
        /// </summary>
        /// <param name="state"></param>
        /// <param name="smooth"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public Task<bool> BackgroundSetPower(bool state = true, int? smooth = null, PowerOnMode mode = PowerOnMode.Normal)
        {
            return Process((Device device) =>
            {
                return device.BackgroundSetPower(state, smooth, mode);
            });
        }

        /// <summary>
        /// Set background light RGB color
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Task<bool> BackgroundSetRGBColor(int r, int g, int b, int? smooth)
        {
            return Process((Device device) =>
            {
                return device.BackgroundSetRGBColor(r, g, b, smooth);
            });
        }

        /// <summary>
        /// Set background scene
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public Task<bool> BackgroundSetScene(Scene scene)
        {
            return Process((Device device) =>
            {
                return device.BackgroundSetScene(scene);
            });
        }

        /// <summary>
        /// Starts a color flow
        /// </summary>
        /// <param name="flow"></param>
        /// <returns></returns>
        public Task<bool> BackgroundStartColorFlow(ColorFlow flow)
        {
            return Process((Device device) =>
            {
                return device.BackgroundStartColorFlow(flow);
            });
        }

        /// <summary>
        /// Stops the color flow
        /// </summary>
        /// <returns></returns>
        public Task<bool> BackgroundStopColorFlow()
        {
            return Process((Device device) =>
            {
                return device.BackgroundStopColorFlow();
            });
        }

        /// <summary>
        /// Toggle background light
        /// </summary>
        /// <returns></returns>
        public Task<bool> BackgroundToggle()
        {
            return Process((Device device) =>
            {
                return device.BackgroundToggle();
            });
        }

        /// <summary>
        /// Turn-Off the devices background light
        /// </summary>
        /// <param name="smooth"></param>
        /// <returns></returns>
        public Task<bool> BackgroundTurnOff(int? smooth = null)
        {
            return Process((Device device) =>
            {
                return device.BackgroundTurnOff(smooth);
            });
        }

        /// <summary>
        /// Turn-On the devices background light
        /// </summary>
        /// <param name="smooth"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public Task<bool> BackgroundTurnOn(int? smooth = null, PowerOnMode mode = PowerOnMode.Normal)
        {
            return Process((Device device) =>
            {
                return device.BackgroundTurnOn(smooth, mode);
            });
        }

        /// <summary>
        /// Toggle Both Background and normal light
        /// </summary>
        /// <returns></returns>
        public Task<bool> DevToggle()
        {
            return Process((Device device) =>
            {
                return device.DevToggle();
            });
        }

        #endregion Public Methods
    }
}