namespace snmpget
{
    /// <summary>
    /// Enum for UPS output
    /// </summary>
    public enum DeviceMode
    {
        /// <summary>
        /// The other
        /// </summary>
        Other = 1,

        /// <summary>
        /// The none
        /// </summary>
        None,

        /// <summary>
        /// AC power
        /// </summary>
        Normal,

        /// <summary>
        /// The bypass
        /// </summary>
        Bypass,

        /// <summary>
        /// DC power / battery mode
        /// </summary>
        Battery,

        /// <summary>
        /// The booster
        /// </summary>
        Booster,

        /// <summary>
        /// The reducer
        /// </summary>
        Reducer
    }
}
