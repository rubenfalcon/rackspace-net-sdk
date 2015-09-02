﻿using System;
using System.Diagnostics;
using System.Extensions;
using System.Net.Http.Headers;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using OpenStack;

namespace Rackspace
{
    /// <summary>
    /// A static container for global configuration settings affecting Rackspace.NET behavior.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    public static class RackspaceNet
    {
        /// <summary>
        /// Global configuration which affects OpenStack.NET's behavior.
        /// <para>Modify using <see cref="Configure"/>.</para>
        /// </summary>
        public static readonly RackspaceNetConfigurationOptions Configuration = new RackspaceNetConfigurationOptions();
        private static readonly object ConfigureLock = new object();
        private static bool _isConfigured;

        /// <summary>
        /// Provides thread-safe accesss to Rackspace.NET's global configuration options.
        /// <para>
        /// Can only be called once at application start-up, before instantiating any Rackspace.NET objects.
        /// </para>
        /// </summary>
        /// <param name="configureFlurl">Addtional configuration of Flurl's global settings <seealso cref="FlurlHttp.Configure" />.</param>
        /// <param name="configureJson">Additional configuration of Json.NET's global settings <seealso cref="JsonConvert.DefaultSettings" />.</param>
        /// <param name="configure">Additional configuration of Rackspace.NET's global settings.</param>
        public static void Configure(Action<FlurlHttpConfigurationOptions> configureFlurl = null, Action<JsonSerializerSettings> configureJson = null, Action<RackspaceNetConfigurationOptions> configure = null)
        {
            lock (ConfigureLock)
            {
                if (_isConfigured)
                    return;

                configure?.Invoke(Configuration);

                Action<OpenStackNetConfigurationOptions> configureOpenStackNet = options =>
                {
                    Configuration.Apply(options);
                };
                OpenStackNet.Configure(configureFlurl, configureJson, configureOpenStackNet);
                _isConfigured = true;
            }
        }

        /// <summary>
        /// Resets all configuration (Rackspace.NET, OpenStack.NET, Flurl and Json.NET) so that <see cref="Configure"/> can be called again.
        /// </summary>
        public static void ResetDefaults()
        {
            lock (ConfigureLock)
            {
                if (!_isConfigured)
                    return;

                Configuration.ResetDefaults();
                OpenStackNet.ResetDefaults();

                _isConfigured = false;
            }
        }

        /// <inheritdoc cref="OpenStack.OpenStackNet.Tracing" />
        public static class Tracing
        {
            /// <inheritdoc cref="OpenStack.OpenStackNet.Tracing.Http" />
            public static readonly TraceSource Http = OpenStackNet.Tracing.Http;
        }
    }

    /// <summary>
    /// A set of properties that affect the SDK's behavior.
    /// <para>Generally set via the static <see cref="Rackspace.RackspaceNet.Configure"/> method.</para>
    /// </summary>
    public class RackspaceNetConfigurationOptions : OpenStackNetConfigurationOptions
    {
        internal RackspaceNetConfigurationOptions() { }

        internal void Apply(OpenStackNetConfigurationOptions target)
        {
            target.UserAgents.Clear();
            UserAgents.ForEach(userAgent => target.UserAgents.Add(userAgent));
        }

        /// <inheritdoc/>
        public override void ResetDefaults()
        {
            base.ResetDefaults();
            UserAgents.Add(new ProductInfoHeaderValue("rackspace.net", GetType().GetAssemblyFileVersion()));
        }
    }
}
