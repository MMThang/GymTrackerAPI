namespace GymTracker.Exceptions
{
    public class EmailConfigurationException : Exception
    {
        public string ConfigurationKey { get; }

        public EmailConfigurationException(string configurationKey)
            : base($"Missing or null EmailService configuration: '{configurationKey}'")
        {
            ConfigurationKey = configurationKey;
        }
    }
}